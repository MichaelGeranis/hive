using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class PerformanceReviewTests
{
    private readonly Guid _validDirectReportId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesReview()
    {
        // Arrange
        var reviewPeriod = "2024 Annual Review";
        var reviewDate = new DateTime(2024, 12, 15);

        // Act
        var review = new PerformanceReview(_validDirectReportId, reviewPeriod, reviewDate);

        // Assert
        review.Id.Should().NotBeEmpty();
        review.DirectReportId.Should().Be(_validDirectReportId);
        review.ReviewPeriod.Should().Be(reviewPeriod);
        review.ReviewDate.Should().Be(reviewDate);
        review.Rating.Should().Be(PerformanceRating.NotRated);
        review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new PerformanceReview(Guid.Empty, "2024 Review", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyReviewPeriod_ThrowsArgumentException(string? reviewPeriod)
    {
        // Act
        var act = () => new PerformanceReview(_validDirectReportId, reviewPeriod!, DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("reviewPeriod");
    }

    [Fact]
    public void Constructor_WithReviewPeriodTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longPeriod = new string('a', 51);

        // Act
        var act = () => new PerformanceReview(_validDirectReportId, longPeriod, DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("reviewPeriod");
    }

    [Fact]
    public void UpdateContent_UpdatesFieldsCorrectly()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        review.UpdateContent(
            "Strong leadership",
            "Communication skills",
            "Great year",
            PerformanceRating.ExceedsExpectations);

        // Assert
        review.Strengths.Should().Be("Strong leadership");
        review.AreasForImprovement.Should().Be("Communication skills");
        review.ManagerNotes.Should().Be("Great year");
        review.Rating.Should().Be(PerformanceRating.ExceedsExpectations);
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_TrimsStrings()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        review.UpdateContent(
            "  Strengths  ",
            "  Areas  ",
            "  Notes  ",
            PerformanceRating.MeetsExpectations);

        // Assert
        review.Strengths.Should().Be("Strengths");
        review.AreasForImprovement.Should().Be("Areas");
        review.ManagerNotes.Should().Be("Notes");
    }

    [Fact]
    public void UpdateContent_HandlesNullValues()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        review.UpdateContent(null!, null!, null!, PerformanceRating.NotRated);

        // Assert
        review.Strengths.Should().BeEmpty();
        review.AreasForImprovement.Should().BeEmpty();
        review.ManagerNotes.Should().BeEmpty();
    }
}
