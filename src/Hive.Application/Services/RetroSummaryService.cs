using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Application.Services;

/// <summary>
/// Summarises the notes in the Retro folder with the model chosen for sentiment, and caches
/// the result until the retros change.
/// </summary>
public class RetroSummaryService : IRetroSummaryService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly INoteFolderRepository _folderRepository;
    private readonly IManagerNoteRepository _noteRepository;
    private readonly IRetroSummaryRepository _summaryRepository;
    private readonly IClaudeApiService _claudeApiService;
    private readonly IOllamaApiService _ollamaApiService;
    private readonly ILogger<RetroSummaryService> _logger;

    public RetroSummaryService(
        IAppSettingsRepository settingsRepository,
        INoteFolderRepository folderRepository,
        IManagerNoteRepository noteRepository,
        IRetroSummaryRepository summaryRepository,
        IClaudeApiService claudeApiService,
        IOllamaApiService ollamaApiService,
        ILogger<RetroSummaryService> logger)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _folderRepository = folderRepository ?? throw new ArgumentNullException(nameof(folderRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _summaryRepository = summaryRepository ?? throw new ArgumentNullException(nameof(summaryRepository));
        _claudeApiService = claudeApiService ?? throw new ArgumentNullException(nameof(claudeApiService));
        _ollamaApiService = ollamaApiService ?? throw new ArgumentNullException(nameof(ollamaApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RetroSummaryDto> GetAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken) ?? new AppSettings();
        if (!settings.RetroSummaryEnabled || !settings.IsSentimentProviderConfigured)
        {
            return new RetroSummaryDto
            {
                IsEnabled = settings.RetroSummaryEnabled,
                IsConfigured = settings.IsSentimentProviderConfigured
            };
        }

        var folders = await _folderRepository.GetAllAsync(cancellationToken);
        var retroFolders = folders.Where(f => f.IsRetro).ToList();

        var retroNotes = new List<ManagerNote>();
        foreach (var folder in retroFolders)
        {
            retroNotes.AddRange(await _noteRepository.GetByFolderAsync(folder.Id, cancellationToken));
        }

        var retros = Retrospective.Latest(retroNotes, settings.RetroSummaryCount);
        var cached = await _summaryRepository.GetAsync(cancellationToken);

        // Nothing to read: no model is asked, and an earlier summary is not shown as current
        if (retros.Count == 0)
        {
            return new RetroSummaryDto
            {
                IsEnabled = true,
                IsConfigured = true,
                HasRetroFolder = retroFolders.Count > 0
            };
        }

        var fingerprint = Retrospective.Fingerprint(retros);
        if (!forceRefresh && cached is not null && !cached.IsStaleFor(fingerprint))
        {
            return Map(cached, retros, isUpToDate: true);
        }

        try
        {
            var result = await SummarizeAsync(settings, retros, cancellationToken);
            var summary = await SaveAsync(cached, result, ModelName(settings), retros, fingerprint, cancellationToken);
            return Map(summary, retros, isUpToDate: true);
        }
        catch (AiProviderException ex)
        {
            // A failed analysis never overwrites the last one; the manager sees why
            _logger.LogWarning(ex, "Could not summarise the retros");
            return Map(cached, retros, isUpToDate: false, error: ex.Message);
        }
    }

    private Task<RetroSummaryResult> SummarizeAsync(
        AppSettings settings,
        IReadOnlyList<Retrospective> retros,
        CancellationToken cancellationToken)
    {
        var forAnalysis = retros
            .Select(r => new RetroForAnalysis(r.Note.Content, r.Date))
            .ToList();

        return settings.SentimentProvider == SentimentProvider.Claude
            ? _claudeApiService.SummarizeRetrosAsync(settings.ClaudeApiKey!, forAnalysis, cancellationToken)
            : _ollamaApiService.SummarizeRetrosAsync(
                new OllamaConnection(settings.OllamaBaseUrl, settings.OllamaModel), forAnalysis, cancellationToken);
    }

    private async Task<RetroSummary> SaveAsync(
        RetroSummary? cached,
        RetroSummaryResult result,
        string model,
        IReadOnlyList<Retrospective> retros,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var warningsJson = JsonSerializer.Serialize(result.Warnings);
        var actionItemsJson = JsonSerializer.Serialize(result.ActionItems);
        var suggestionsJson = JsonSerializer.Serialize(result.Suggestions);
        var latestRetroDate = retros.Max(r => r.Date);

        if (cached is null)
        {
            var summary = new RetroSummary(
                result.Summary, warningsJson, actionItemsJson, suggestionsJson,
                model, retros.Count, latestRetroDate, fingerprint);
            return await _summaryRepository.AddAsync(summary, cancellationToken);
        }

        cached.Update(
            result.Summary, warningsJson, actionItemsJson, suggestionsJson,
            model, retros.Count, latestRetroDate, fingerprint);
        await _summaryRepository.UpdateAsync(cached, cancellationToken);
        return cached;
    }

    private static string ModelName(AppSettings settings) =>
        settings.SentimentProvider == SentimentProvider.Claude ? "Claude" : settings.OllamaModel;

    private static RetroSummaryDto Map(
        RetroSummary? summary,
        IReadOnlyList<Retrospective> retros,
        bool isUpToDate,
        string? error = null)
    {
        return new RetroSummaryDto
        {
            IsEnabled = true,
            IsConfigured = true,
            HasRetroFolder = true,
            Retros = retros
                .Select(r => new RetroNoteDto { NoteId = r.Note.Id, Title = r.Note.Title, RetroDate = r.Date })
                .ToList(),
            Summary = summary?.Summary,
            Warnings = Deserialize<string[]>(summary?.WarningsJson) ?? Array.Empty<string>(),
            ActionItems = (Deserialize<RetroActionItem[]>(summary?.ActionItemsJson) ?? Array.Empty<RetroActionItem>())
                .Select(a => new RetroActionItemDto { Text = a.Text, RetroDate = a.RetroDate, Owner = a.Owner ?? string.Empty })
                .ToList(),
            Suggestions = Deserialize<string[]>(summary?.SuggestionsJson) ?? Array.Empty<string>(),
            Model = summary?.Model,
            RetrosAnalyzed = summary?.RetrosAnalyzed ?? 0,
            AnalyzedAt = summary?.AnalyzedAt,
            IsUpToDate = isUpToDate && summary is not null,
            Error = error
        };
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
