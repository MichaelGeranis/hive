using System.Globalization;
using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Application.Services;

/// <summary>
/// Service for sentiment analysis operations.
/// </summary>
public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ISentimentAnalysisCacheRepository _cacheRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IMeetingNoteRepository _meetingNoteRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IClaudeApiService _claudeApiService;
    private readonly ILogger<SentimentAnalysisService> _logger;

    public SentimentAnalysisService(
        IAppSettingsRepository settingsRepository,
        ISentimentAnalysisCacheRepository cacheRepository,
        IDirectReportRepository directReportRepository,
        IMeetingNoteRepository meetingNoteRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IClaudeApiService claudeApiService,
        ILogger<SentimentAnalysisService> logger)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _cacheRepository = cacheRepository ?? throw new ArgumentNullException(nameof(cacheRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _meetingNoteRepository = meetingNoteRepository ?? throw new ArgumentNullException(nameof(meetingNoteRepository));
        _meetingRepository = meetingRepository ?? throw new ArgumentNullException(nameof(meetingRepository));
        _claudeApiService = claudeApiService ?? throw new ArgumentNullException(nameof(claudeApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SentimentStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        return new SentimentStatusDto
        {
            IsEnabled = settings?.SentimentAnalysisEnabled ?? false,
            IsConfigured = settings?.HasClaudeApiKey ?? false,
            AnalysisDays = settings?.SentimentAnalysisDays ?? 90
        };
    }

    public async Task<SentimentAnalysisDto?> GetForDirectReportAsync(
        Guid directReportId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings is null || !settings.SentimentAnalysisEnabled || !settings.HasClaudeApiKey)
        {
            return null;
        }

        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);
        if (directReport is null)
        {
            return null;
        }

        // Get meeting notes for the analysis period
        var cutoffDate = DateTime.UtcNow.AddDays(-settings.SentimentAnalysisDays);
        var notesWithContext = await GetNotesForDirectReportAsync(directReportId, cutoffDate, cancellationToken);

        if (notesWithContext.Count == 0)
        {
            return new SentimentAnalysisDto
            {
                DirectReportId = directReportId,
                DirectReportName = directReport.FullName,
                Score = new SentimentScoreDto
                {
                    Positive = 0,
                    Neutral = 100,
                    Negative = 0,
                    OverallSentiment = "Neutral"
                },
                KeyThemes = Array.Empty<string>(),
                Trend = Array.Empty<SentimentTrendDto>(),
                AnalyzedAt = DateTime.UtcNow,
                NotesAnalyzed = 0,
                DaysAnalyzed = settings.SentimentAnalysisDays
            };
        }

        var latestNoteDate = notesWithContext.Max(n => n.MeetingDate);
        var latestNoteDateAsDateTime = latestNoteDate.ToDateTime(TimeOnly.MinValue);

        // Check cache
        if (!forceRefresh)
        {
            var cached = await _cacheRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
            if (cached is not null && !cached.IsStale(latestNoteDateAsDateTime))
            {
                return MapCacheToDto(cached, directReport.FullName);
            }
        }

        // Perform analysis
        try
        {
            var result = await _claudeApiService.AnalyzeSentimentAsync(notesWithContext, cancellationToken);

            // Cache the result
            var cache = await _cacheRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
            var keyThemesJson = JsonSerializer.Serialize(result.KeyThemes);
            var trendDataJson = JsonSerializer.Serialize(result.MonthlyBreakdown);

            if (cache is null)
            {
                cache = new SentimentAnalysisCache(
                    directReportId,
                    result.Positive,
                    result.Neutral,
                    result.Negative,
                    result.OverallSentiment,
                    keyThemesJson,
                    trendDataJson,
                    notesWithContext.Count,
                    settings.SentimentAnalysisDays,
                    latestNoteDateAsDateTime
                );
                await _cacheRepository.AddAsync(cache, cancellationToken);
            }
            else
            {
                cache.Update(
                    result.Positive,
                    result.Neutral,
                    result.Negative,
                    result.OverallSentiment,
                    keyThemesJson,
                    trendDataJson,
                    notesWithContext.Count,
                    settings.SentimentAnalysisDays,
                    latestNoteDateAsDateTime
                );
                await _cacheRepository.UpdateAsync(cache, cancellationToken);
            }

            return MapCacheToDto(cache, directReport.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze sentiment for direct report {DirectReportId}", directReportId);
            throw;
        }
    }

    public async Task<TeamSentimentOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings is null || !settings.SentimentAnalysisEnabled || !settings.HasClaudeApiKey)
        {
            return new TeamSentimentOverviewDto
            {
                AveragePositive = 0,
                AverageNeutral = 100,
                AverageNegative = 0,
                OverallTeamSentiment = "Not Configured",
                ByDirectReport = Array.Empty<DirectReportSentimentSummaryDto>(),
                CommonThemes = Array.Empty<string>(),
                TotalNotesAnalyzed = 0,
                DirectReportsAnalyzed = 0
            };
        }

        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var directOnly = directReports.Where(dr => dr.IsDirect).ToList();

        var summaries = new List<DirectReportSentimentSummaryDto>();
        var allThemes = new List<string>();
        double totalPositive = 0, totalNeutral = 0, totalNegative = 0;
        int totalNotes = 0;
        int analyzedCount = 0;

        foreach (var dr in directOnly)
        {
            try
            {
                var analysis = await GetForDirectReportAsync(dr.Id, false, cancellationToken);
                if (analysis is not null && analysis.NotesAnalyzed > 0)
                {
                    summaries.Add(new DirectReportSentimentSummaryDto
                    {
                        DirectReportId = dr.Id,
                        DirectReportName = dr.FullName,
                        OverallSentiment = analysis.Score.OverallSentiment,
                        PositiveScore = analysis.Score.Positive,
                        NotesCount = analysis.NotesAnalyzed,
                        TrendDirection = CalculateTrendDirection(analysis.Trend)
                    });

                    totalPositive += analysis.Score.Positive;
                    totalNeutral += analysis.Score.Neutral;
                    totalNegative += analysis.Score.Negative;
                    totalNotes += analysis.NotesAnalyzed;
                    analyzedCount++;

                    allThemes.AddRange(analysis.KeyThemes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get sentiment for direct report {DirectReportId}", dr.Id);
            }
        }

        var commonThemes = allThemes
            .GroupBy(t => t.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.First())
            .ToList();

        var avgPositive = analyzedCount > 0 ? totalPositive / analyzedCount : 0;
        var avgNeutral = analyzedCount > 0 ? totalNeutral / analyzedCount : 100;
        var avgNegative = analyzedCount > 0 ? totalNegative / analyzedCount : 0;

        return new TeamSentimentOverviewDto
        {
            AveragePositive = Math.Round(avgPositive, 1),
            AverageNeutral = Math.Round(avgNeutral, 1),
            AverageNegative = Math.Round(avgNegative, 1),
            OverallTeamSentiment = DetermineOverallSentiment(avgPositive, avgNeutral, avgNegative),
            ByDirectReport = summaries.OrderByDescending(s => s.PositiveScore).ToList(),
            CommonThemes = commonThemes,
            TotalNotesAnalyzed = totalNotes,
            DirectReportsAnalyzed = analyzedCount
        };
    }

    public async Task<ApiKeyValidationResultDto> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var (isValid, error) = await _claudeApiService.ValidateApiKeyAsync(apiKey, cancellationToken);
        return new ApiKeyValidationResultDto
        {
            Valid = isValid,
            Error = error
        };
    }

    private async Task<IReadOnlyList<MeetingNoteForAnalysis>> GetNotesForDirectReportAsync(
        Guid directReportId,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var meetings = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        var cutoffDateOnly = DateOnly.FromDateTime(cutoffDate);
        var recentMeetings = meetings.Where(m => m.MeetingDate >= cutoffDateOnly).ToList();

        var notes = new List<MeetingNoteForAnalysis>();

        foreach (var meeting in recentMeetings)
        {
            var meetingNotes = await _meetingNoteRepository.GetByMeetingIdAsync(meeting.Id, includePrivate: false, cancellationToken);

            foreach (var note in meetingNotes)
            {
                notes.Add(new MeetingNoteForAnalysis(
                    note.Content,
                    note.Category.ToString(),
                    meeting.MeetingDate
                ));
            }
        }

        return notes;
    }

    private SentimentAnalysisDto MapCacheToDto(SentimentAnalysisCache cache, string directReportName)
    {
        var keyThemes = DeserializeJson<string[]>(cache.KeyThemesJson) ?? Array.Empty<string>();
        var trendData = DeserializeJson<MonthlySentiment[]>(cache.TrendDataJson) ?? Array.Empty<MonthlySentiment>();

        var trend = trendData.Select(t => new SentimentTrendDto
        {
            Year = t.Year,
            Month = t.Month,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(t.Month),
            PositiveScore = t.Positive,
            NeutralScore = t.Neutral,
            NegativeScore = t.Negative,
            NotesCount = t.NotesCount
        }).ToList();

        return new SentimentAnalysisDto
        {
            DirectReportId = cache.DirectReportId,
            DirectReportName = directReportName,
            Score = new SentimentScoreDto
            {
                Positive = cache.PositiveScore,
                Neutral = cache.NeutralScore,
                Negative = cache.NegativeScore,
                OverallSentiment = cache.OverallSentiment
            },
            KeyThemes = keyThemes,
            Trend = trend,
            AnalyzedAt = cache.AnalyzedAt,
            NotesAnalyzed = cache.NotesAnalyzed,
            DaysAnalyzed = cache.DaysAnalyzed
        };
    }

    private static T? DeserializeJson<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    private static string CalculateTrendDirection(IReadOnlyList<SentimentTrendDto> trend)
    {
        if (trend.Count < 2) return "Stable";

        var recent = trend.TakeLast(2).ToList();
        if (recent.Count < 2) return "Stable";

        var diff = recent[1].PositiveScore - recent[0].PositiveScore;

        return diff switch
        {
            > 5 => "Improving",
            < -5 => "Declining",
            _ => "Stable"
        };
    }

    private static string DetermineOverallSentiment(double positive, double neutral, double negative)
    {
        if (positive >= 60) return "Positive";
        if (negative >= 40) return "Negative";
        if (positive >= 40 && negative >= 20) return "Mixed";
        return "Neutral";
    }
}
