using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Application.Services;

public class SentimentAnalysisServiceTests
{
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock;
    private readonly Mock<ISentimentAnalysisCacheRepository> _cacheRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IMeetingNoteRepository> _meetingNoteRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IClaudeApiService> _claudeApiServiceMock;
    private readonly Mock<ILogger<SentimentAnalysisService>> _loggerMock;
    private readonly SentimentAnalysisService _service;

    private readonly DirectReport _testDirectReport;
    private AppSettings _enabledSettings;

    public SentimentAnalysisServiceTests()
    {
        _settingsRepositoryMock = new Mock<IAppSettingsRepository>();
        _cacheRepositoryMock = new Mock<ISentimentAnalysisCacheRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _meetingNoteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _claudeApiServiceMock = new Mock<IClaudeApiService>();
        _loggerMock = new Mock<ILogger<SentimentAnalysisService>>();

        _service = new SentimentAnalysisService(
            _settingsRepositoryMock.Object,
            _cacheRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _meetingNoteRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _claudeApiServiceMock.Object,
            _loggerMock.Object);

        _testDirectReport = new DirectReport("Alice", "Johnson", "alice@test.com", "Engineer", "Engineering", new DateTime(2021, 3, 15));

        _enabledSettings = new AppSettings("[]");
        _enabledSettings.UpdateClaudeApiKey("sk-test-key-12345");
        _enabledSettings.UpdateSentimentSettings(90, true);
    }

    // ============== Constructor Tests ==============

    [Fact]
    public void Constructor_WithNullSettingsRepository_ThrowsArgumentNullException()
    {
        var act = () => new SentimentAnalysisService(null!, _cacheRepositoryMock.Object,
            _directReportRepositoryMock.Object, _meetingNoteRepositoryMock.Object,
            _meetingRepositoryMock.Object, _claudeApiServiceMock.Object, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settingsRepository");
    }

    [Fact]
    public void Constructor_WithNullClaudeApiService_ThrowsArgumentNullException()
    {
        var act = () => new SentimentAnalysisService(_settingsRepositoryMock.Object, _cacheRepositoryMock.Object,
            _directReportRepositoryMock.Object, _meetingNoteRepositoryMock.Object,
            _meetingRepositoryMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("claudeApiService");
    }

    // ============== GetStatusAsync Tests ==============

    [Fact]
    public async Task GetStatusAsync_WhenSettingsNull_ReturnsDisabledStatus()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeFalse();
        result.IsConfigured.Should().BeFalse();
        result.AnalysisDays.Should().Be(90);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithApiKey_ReturnsEnabledStatus()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.IsConfigured.Should().BeTrue();
        result.AnalysisDays.Should().Be(90);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledButNoApiKey_ReturnsNotConfiguredStatus()
    {
        // Arrange
        var settings = new AppSettings("[]");
        settings.UpdateSentimentSettings(90, true);
        // No API key set
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.IsConfigured.Should().BeFalse();
    }

    // ============== GetForDirectReportAsync Tests ==============

    [Fact]
    public async Task GetForDirectReportAsync_WhenSettingsNull_ReturnsNull()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenSentimentDisabled_ReturnsNull()
    {
        // Arrange
        var disabledSettings = new AppSettings("[]");
        disabledSettings.UpdateClaudeApiKey("sk-test-key");
        disabledSettings.UpdateSentimentSettings(90, false);
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(disabledSettings);

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenNoApiKey_ReturnsNull()
    {
        // Arrange
        var noKeySettings = new AppSettings("[]");
        noKeySettings.UpdateSentimentSettings(90, true);
        // No API key
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(noKeySettings);

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenDirectReportNotFound_ReturnsNull()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenNoNotes_ReturnsNeutralResult()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result.Should().NotBeNull();
        result!.NotesAnalyzed.Should().Be(0);
        result.Score.OverallSentiment.Should().Be("Neutral");
        result.Score.Positive.Should().Be(0);
        result.Score.Neutral.Should().Be(100);
        result.Score.Negative.Should().Be(0);
        _claudeApiServiceMock.Verify(c => c.AnalyzeSentimentAsync(It.IsAny<IReadOnlyList<MeetingNoteForAnalysis>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenCacheIsValid_ReturnsCachedResult()
    {
        // Arrange
        var directReportId = _testDirectReport.Id;
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Set up a meeting with a note
        var meeting = new OneOnOneMeeting(directReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 60, "1:1");
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting });

        var note = new MeetingNote(meeting.Id, "Things are going well", MeetingNoteCategory.Feedback);
        _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { note });

        // Cache is fresh (latest note is older than cache analyzed time)
        var latestNoteDate = DateTime.UtcNow.AddDays(-5);
        var cache = new SentimentAnalysisCache(directReportId, 70, 20, 10, "Positive",
            "[\"engagement\"]", "[]", 1, 90, latestNoteDate);

        _cacheRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cache);

        // Act
        var result = await _service.GetForDirectReportAsync(directReportId, forceRefresh: false);

        // Assert
        result.Should().NotBeNull();
        result!.Score.OverallSentiment.Should().Be("Positive");
        // Should NOT have called Claude API since cache is valid
        _claudeApiServiceMock.Verify(c => c.AnalyzeSentimentAsync(It.IsAny<IReadOnlyList<MeetingNoteForAnalysis>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenForceRefresh_CallsClaudeApiIgnoringCache()
    {
        // Arrange
        var directReportId = _testDirectReport.Id;
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        var meeting = new OneOnOneMeeting(directReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 60, "1:1");
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting });

        var note = new MeetingNote(meeting.Id, "Good progress this week", MeetingNoteCategory.Feedback);
        _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { note });

        var apiResult = new SentimentAnalysisResult(75, 20, 5, "Positive",
            new[] { "engagement" }, Array.Empty<MonthlySentiment>());
        _claudeApiServiceMock.Setup(c => c.AnalyzeSentimentAsync(It.IsAny<IReadOnlyList<MeetingNoteForAnalysis>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(apiResult);

        _cacheRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SentimentAnalysisCache?)null);
        _cacheRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SentimentAnalysisCache>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SentimentAnalysisCache c, CancellationToken _) => c);

        // Act
        var result = await _service.GetForDirectReportAsync(directReportId, forceRefresh: true);

        // Assert
        result.Should().NotBeNull();
        _claudeApiServiceMock.Verify(c => c.AnalyzeSentimentAsync(It.IsAny<IReadOnlyList<MeetingNoteForAnalysis>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetForDirectReportAsync_WhenApiThrows_RethrowsException()
    {
        // Arrange
        var directReportId = _testDirectReport.Id;
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        var meeting = new OneOnOneMeeting(directReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 60, "1:1");
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting });

        var note = new MeetingNote(meeting.Id, "Some note content", MeetingNoteCategory.Feedback);
        _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { note });

        _cacheRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SentimentAnalysisCache?)null);
        _claudeApiServiceMock.Setup(c => c.AnalyzeSentimentAsync(It.IsAny<IReadOnlyList<MeetingNoteForAnalysis>>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act
        var act = async () => await _service.GetForDirectReportAsync(directReportId, forceRefresh: true);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ============== GetTeamOverviewAsync Tests ==============

    [Fact]
    public async Task GetTeamOverviewAsync_WhenSentimentDisabled_ReturnsNotConfiguredOverview()
    {
        // Arrange
        var settings = new AppSettings("[]");
        settings.UpdateSentimentSettings(90, false);
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetTeamOverviewAsync();

        // Assert
        result.Should().NotBeNull();
        result.OverallTeamSentiment.Should().Be("Not Configured");
        result.ByDirectReport.Should().BeEmpty();
        result.DirectReportsAnalyzed.Should().Be(0);
        result.TotalNotesAnalyzed.Should().Be(0);
    }

    [Fact]
    public async Task GetTeamOverviewAsync_WhenSettingsNull_ReturnsNotConfiguredOverview()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        // Act
        var result = await _service.GetTeamOverviewAsync();

        // Assert
        result.OverallTeamSentiment.Should().Be("Not Configured");
    }

    [Fact]
    public async Task GetTeamOverviewAsync_WithNoDirectReports_ReturnsEmptyOverview()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.GetTeamOverviewAsync();

        // Assert
        result.ByDirectReport.Should().BeEmpty();
        result.DirectReportsAnalyzed.Should().Be(0);
        result.AverageNeutral.Should().Be(100); // defaults when no one analyzed
    }

    // ============== ValidateApiKeyAsync Tests ==============

    [Fact]
    public async Task ValidateApiKeyAsync_WhenValid_ReturnsValidResult()
    {
        // Arrange
        _claudeApiServiceMock.Setup(c => c.ValidateApiKeyAsync("valid-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _service.ValidateApiKeyAsync("valid-key");

        // Assert
        result.Valid.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenInvalid_ReturnsInvalidResultWithError()
    {
        // Arrange
        _claudeApiServiceMock.Setup(c => c.ValidateApiKeyAsync("bad-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid API key"));

        // Act
        var result = await _service.ValidateApiKeyAsync("bad-key");

        // Assert
        result.Valid.Should().BeFalse();
        result.Error.Should().Be("Invalid API key");
    }

    // ============== CalculateTrendDirection Tests (via team overview) ==============

    [Fact]
    public async Task GetForDirectReportAsync_WhenNoNotes_ReturnEmptyTrend()
    {
        // Arrange
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_enabledSettings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());

        // Act
        var result = await _service.GetForDirectReportAsync(_testDirectReport.Id);

        // Assert
        result!.Trend.Should().BeEmpty();
    }
}
