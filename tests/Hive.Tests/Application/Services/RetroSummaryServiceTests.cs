using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Application.Services;

public class RetroSummaryServiceTests
{
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock = new();
    private readonly Mock<INoteFolderRepository> _folderRepositoryMock = new();
    private readonly Mock<IManagerNoteRepository> _noteRepositoryMock = new();
    private readonly Mock<IRetroSummaryRepository> _summaryRepositoryMock = new();
    private readonly Mock<IClaudeApiService> _claudeApiServiceMock = new();
    private readonly Mock<IOllamaApiService> _ollamaApiServiceMock = new();
    private readonly RetroSummaryService _service;

    private readonly AppSettings _settings = new();
    private readonly NoteFolder _retroFolder = new("Retro");

    private static readonly RetroSummaryResult Result = new(
        "The team kept raising flaky tests.",
        ["Flaky tests raised in three retros"],
        [new RetroActionItem("Quarantine flaky tests", new DateOnly(2026, 9, 28), "Alice")],
        ["Give the flaky test work an owner"]);

    public RetroSummaryServiceTests()
    {
        _service = new RetroSummaryService(
            _settingsRepositoryMock.Object,
            _folderRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _summaryRepositoryMock.Object,
            _claudeApiServiceMock.Object,
            _ollamaApiServiceMock.Object,
            Mock.Of<ILogger<RetroSummaryService>>());

        _settings.UpdateRetroSummarySettings(true, 6);
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_settings);
        _folderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new NoteFolder("Journal"), _retroFolder]);
        _summaryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RetroSummary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetroSummary s, CancellationToken _) => s);
        _ollamaApiServiceMock
            .Setup(s => s.SummarizeRetrosAsync(It.IsAny<OllamaConnection>(), It.IsAny<IReadOnlyList<RetroForAnalysis>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result);
    }

    private List<ManagerNote> GivenRetros(params string[] contents)
    {
        var notes = contents.Select(c =>
        {
            var note = ManagerNote.CreateBlank(_retroFolder.Id);
            note.UpdateContent(c);
            return note;
        }).ToList();
        _noteRepositoryMock.Setup(r => r.GetByFolderAsync(_retroFolder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notes);
        return notes;
    }

    private void GivenCached(RetroSummary? summary) =>
        _summaryRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(summary);

    [Fact]
    public async Task GetAsync_Disabled_AsksNoModel()
    {
        _settings.UpdateRetroSummarySettings(false, 6);

        var result = await _service.GetAsync();

        result.IsEnabled.Should().BeFalse();
        result.Summary.Should().BeNull();
        _folderRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ClaudeWithoutKey_IsNotConfigured()
    {
        _settings.UpdateSentimentProvider("Claude");

        var result = await _service.GetAsync();

        result.IsEnabled.Should().BeTrue();
        result.IsConfigured.Should().BeFalse();
        _claudeApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_NoRetroFolder_SaysSo()
    {
        _folderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new NoteFolder("Journal")]);

        var result = await _service.GetAsync();

        result.HasRetroFolder.Should().BeFalse();
        result.Retros.Should().BeEmpty();
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_EmptyRetroFolder_AsksNoModel()
    {
        GivenRetros();
        GivenCached(null);

        var result = await _service.GetAsync();

        result.HasRetroFolder.Should().BeTrue();
        result.Summary.Should().BeNull();
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_NothingCached_SummarisesTheLatestRetrosAndCachesIt()
    {
        GivenRetros("2026-07-01\n- old", "2026-09-01\n- newest", "2026-08-01\n- middle");
        _settings.UpdateRetroSummarySettings(true, 2);
        GivenCached(null);
        IReadOnlyList<RetroForAnalysis>? sent = null;
        _ollamaApiServiceMock
            .Setup(s => s.SummarizeRetrosAsync(It.IsAny<OllamaConnection>(), It.IsAny<IReadOnlyList<RetroForAnalysis>>(), It.IsAny<CancellationToken>()))
            .Callback((OllamaConnection _, IReadOnlyList<RetroForAnalysis> retros, CancellationToken _) => sent = retros)
            .ReturnsAsync(Result);

        var result = await _service.GetAsync();

        sent!.Select(r => r.RetroDate).Should().BeEquivalentTo([new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1)]);
        result.Summary.Should().Be(Result.Summary);
        result.Warnings.Should().Equal(Result.Warnings);
        result.ActionItems.Should().ContainSingle().Which.Owner.Should().Be("Alice");
        result.Suggestions.Should().Equal(Result.Suggestions);
        result.Model.Should().Be(AppSettings.DefaultOllamaModel);
        result.RetrosAnalyzed.Should().Be(2);
        result.Retros.Select(r => r.RetroDate).Should().Equal(new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));
        result.IsUpToDate.Should().BeTrue();
        result.Error.Should().BeNull();
        _summaryRepositoryMock.Verify(r => r.AddAsync(It.Is<RetroSummary>(s => s.RetrosAnalyzed == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_CacheMatchesTheRetros_AsksNoModel()
    {
        var notes = GivenRetros("2026-09-01\n- went well");
        var fingerprint = Retrospective.Fingerprint(Retrospective.Latest(notes, 6));
        GivenCached(new RetroSummary("Cached.", "[]", "[]", "[]", "llama3.1:8b", 1, new DateOnly(2026, 9, 1), fingerprint));

        var result = await _service.GetAsync();

        result.Summary.Should().Be("Cached.");
        result.IsUpToDate.Should().BeTrue();
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_RetrosChanged_UpdatesTheCache()
    {
        GivenRetros("2026-09-01\n- went well");
        var cached = new RetroSummary("Old.", "[]", "[]", "[]", "llama3.1:8b", 1, new DateOnly(2026, 8, 1), "OLD");
        GivenCached(cached);

        var result = await _service.GetAsync();

        result.Summary.Should().Be(Result.Summary);
        _summaryRepositoryMock.Verify(r => r.UpdateAsync(cached, It.IsAny<CancellationToken>()), Times.Once);
        _summaryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RetroSummary>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ForceRefresh_AsksTheModelEvenWhenUpToDate()
    {
        var notes = GivenRetros("2026-09-01\n- went well");
        var fingerprint = Retrospective.Fingerprint(Retrospective.Latest(notes, 6));
        GivenCached(new RetroSummary("Cached.", "[]", "[]", "[]", "llama3.1:8b", 1, new DateOnly(2026, 9, 1), fingerprint));

        var result = await _service.GetAsync(forceRefresh: true);

        result.Summary.Should().Be(Result.Summary);
    }

    [Fact]
    public async Task GetAsync_Claude_UsesTheKey()
    {
        _settings.UpdateSentimentProvider("Claude");
        _settings.UpdateClaudeApiKey("sk-test");
        GivenRetros("2026-09-01\n- went well");
        GivenCached(null);
        _claudeApiServiceMock
            .Setup(s => s.SummarizeRetrosAsync("sk-test", It.IsAny<IReadOnlyList<RetroForAnalysis>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result);

        var result = await _service.GetAsync();

        result.Model.Should().Be("Claude");
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ModelFails_KeepsTheLastSummaryAndReportsTheError()
    {
        GivenRetros("2026-09-01\n- went well");
        var cached = new RetroSummary("Old.", "[\"w\"]", "[]", "[]", "llama3.1:8b", 1, new DateOnly(2026, 8, 1), "OLD");
        GivenCached(cached);
        _ollamaApiServiceMock
            .Setup(s => s.SummarizeRetrosAsync(It.IsAny<OllamaConnection>(), It.IsAny<IReadOnlyList<RetroForAnalysis>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaApiException("Could not reach Ollama."));

        var result = await _service.GetAsync();

        result.Summary.Should().Be("Old.");
        result.Warnings.Should().Equal("w");
        result.IsUpToDate.Should().BeFalse();
        result.Error.Should().Be("Could not reach Ollama.");
        _summaryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RetroSummary>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ModelFailsWithNothingCached_ReportsTheError()
    {
        GivenRetros("2026-09-01\n- went well");
        GivenCached(null);
        _ollamaApiServiceMock
            .Setup(s => s.SummarizeRetrosAsync(It.IsAny<OllamaConnection>(), It.IsAny<IReadOnlyList<RetroForAnalysis>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaApiException("Could not reach Ollama."));

        var result = await _service.GetAsync();

        result.Summary.Should().BeNull();
        result.Retros.Should().ContainSingle();
        result.Error.Should().Be("Could not reach Ollama.");
    }
}
