using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SentimentAnalysisCacheTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesCache()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var positiveScore = 65.5;
        var neutralScore = 25.0;
        var negativeScore = 9.5;
        var overallSentiment = "Positive";
        var keyThemesJson = "[\"growth\", \"challenges\"]";
        var trendDataJson = "[{\"date\": \"2024-01\", \"score\": 70}]";
        var notesAnalyzed = 15;
        var daysAnalyzed = 90;
        var latestNoteDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var cache = new SentimentAnalysisCache(
            directReportId,
            positiveScore,
            neutralScore,
            negativeScore,
            overallSentiment,
            keyThemesJson,
            trendDataJson,
            notesAnalyzed,
            daysAnalyzed,
            latestNoteDate);

        // Assert
        cache.Id.Should().NotBeEmpty();
        cache.DirectReportId.Should().Be(directReportId);
        cache.PositiveScore.Should().Be(positiveScore);
        cache.NeutralScore.Should().Be(neutralScore);
        cache.NegativeScore.Should().Be(negativeScore);
        cache.OverallSentiment.Should().Be(overallSentiment);
        cache.KeyThemesJson.Should().Be(keyThemesJson);
        cache.TrendDataJson.Should().Be(trendDataJson);
        cache.NotesAnalyzed.Should().Be(notesAnalyzed);
        cache.DaysAnalyzed.Should().Be(daysAnalyzed);
        cache.LatestNoteDate.Should().Be(latestNoteDate);
        cache.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        cache.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new SentimentAnalysisCache(
            Guid.Empty, 50, 30, 20, "Neutral", "[]", "[]", 10, 30, DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(110, 100)]
    [InlineData(150, 100)]
    public void Constructor_ClampsPositiveScoreToValidRange(double input, double expected)
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), input, 0, 0, "Test", "[]", "[]", 0, 0, DateTime.UtcNow);

        // Assert
        cache.PositiveScore.Should().Be(expected);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(110, 100)]
    public void Constructor_ClampsNeutralScoreToValidRange(double input, double expected)
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 0, input, 0, "Test", "[]", "[]", 0, 0, DateTime.UtcNow);

        // Assert
        cache.NeutralScore.Should().Be(expected);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(110, 100)]
    public void Constructor_ClampsNegativeScoreToValidRange(double input, double expected)
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 0, 0, input, "Test", "[]", "[]", 0, 0, DateTime.UtcNow);

        // Assert
        cache.NegativeScore.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithNullOverallSentiment_DefaultsToUnknown()
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, null!, "[]", "[]", 10, 30, DateTime.UtcNow);

        // Assert
        cache.OverallSentiment.Should().Be("Unknown");
    }

    [Fact]
    public void Constructor_WithNullJsonValues_DefaultsToEmptyArray()
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", null!, null!, 10, 30, DateTime.UtcNow);

        // Assert
        cache.KeyThemesJson.Should().Be("[]");
        cache.TrendDataJson.Should().Be("[]");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", "[]", "[]", 5, 30, DateTime.UtcNow.AddDays(-30));

        var newPositive = 70.0;
        var newNeutral = 20.0;
        var newNegative = 10.0;
        var newSentiment = "Positive";
        var newThemes = "[\"success\"]";
        var newTrend = "[{\"score\": 75}]";
        var newNotesCount = 20;
        var newDays = 60;
        var newLatestDate = DateTime.UtcNow;

        // Act
        cache.Update(
            newPositive, newNeutral, newNegative, newSentiment,
            newThemes, newTrend, newNotesCount, newDays, newLatestDate);

        // Assert
        cache.PositiveScore.Should().Be(newPositive);
        cache.NeutralScore.Should().Be(newNeutral);
        cache.NegativeScore.Should().Be(newNegative);
        cache.OverallSentiment.Should().Be(newSentiment);
        cache.KeyThemesJson.Should().Be(newThemes);
        cache.TrendDataJson.Should().Be(newTrend);
        cache.NotesAnalyzed.Should().Be(newNotesCount);
        cache.DaysAnalyzed.Should().Be(newDays);
        cache.LatestNoteDate.Should().Be(newLatestDate);
        cache.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_ClampsScoresToValidRange()
    {
        // Arrange
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", "[]", "[]", 5, 30, DateTime.UtcNow);

        // Act
        cache.Update(150, -20, 200, "Test", "[]", "[]", 1, 1, DateTime.UtcNow);

        // Assert
        cache.PositiveScore.Should().Be(100);
        cache.NeutralScore.Should().Be(0);
        cache.NegativeScore.Should().Be(100);
    }

    [Fact]
    public void IsStale_WhenLatestNoteDateIsNewer_ReturnsTrue()
    {
        // Arrange
        var originalDate = DateTime.UtcNow.AddDays(-5);
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", "[]", "[]", 5, 30, originalDate);

        var newerDate = DateTime.UtcNow;

        // Act & Assert
        cache.IsStale(newerDate).Should().BeTrue();
    }

    [Fact]
    public void IsStale_WhenLatestNoteDateIsSame_ReturnsFalse()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", "[]", "[]", 5, 30, date);

        // Act & Assert
        cache.IsStale(date).Should().BeFalse();
    }

    [Fact]
    public void IsStale_WhenLatestNoteDateIsOlder_ReturnsFalse()
    {
        // Arrange
        var cacheDate = DateTime.UtcNow;
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), 50, 30, 20, "Neutral", "[]", "[]", 5, 30, cacheDate);

        var olderDate = DateTime.UtcNow.AddDays(-5);

        // Act & Assert
        cache.IsStale(olderDate).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Constructor_WithBoundaryScores_CreatesCache(double score)
    {
        // Act
        var cache = new SentimentAnalysisCache(
            Guid.NewGuid(), score, score, score, "Test", "[]", "[]", 0, 0, DateTime.UtcNow);

        // Assert
        cache.PositiveScore.Should().Be(score);
        cache.NeutralScore.Should().Be(score);
        cache.NegativeScore.Should().Be(score);
    }
}
