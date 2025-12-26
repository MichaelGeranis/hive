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
        review.Status.Should().Be(ReviewStatus.Draft);
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
    public void UpdateContent_InDraftStatus_UpdatesFields()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        review.UpdateContent(
            "Strong leadership",
            "Communication skills",
            "Lead new project",
            "Great year",
            PerformanceRating.ExceedsExpectations);

        // Assert
        review.Strengths.Should().Be("Strong leadership");
        review.AreasForImprovement.Should().Be("Communication skills");
        review.GoalsForNextPeriod.Should().Be("Lead new project");
        review.ManagerNotes.Should().Be("Great year");
        review.Rating.Should().Be(PerformanceRating.ExceedsExpectations);
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_WhenNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        // Act
        var act = () => review.UpdateContent("New", "New", "New", "New", PerformanceRating.Outstanding);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void UpdateSelfAssessment_WhenNotCompleted_UpdatesField()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        review.UpdateSelfAssessment("I did well this year");

        // Assert
        review.EmployeeSelfAssessment.Should().Be("I did well this year");
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSelfAssessment_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = CreateCompletedReview();

        // Act
        var act = () => review.UpdateSelfAssessment("New assessment");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Submit_FromDraftWithRating_ChangesStatusToSubmitted()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);

        // Act
        review.Submit();

        // Assert
        review.Status.Should().Be(ReviewStatus.Submitted);
        review.SubmittedAt.Should().NotBeNull();
        review.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Submit_WithoutRating_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        var act = () => review.Submit();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Rating*");
    }

    [Fact]
    public void Submit_WhenNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        // Act
        var act = () => review.Submit();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public void Acknowledge_FromSubmitted_ChangesStatusToAcknowledged()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        // Act
        review.Acknowledge();

        // Assert
        review.Status.Should().Be(ReviewStatus.Acknowledged);
        review.AcknowledgedAt.Should().NotBeNull();
        review.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Acknowledge_WhenNotSubmitted_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);

        // Act
        var act = () => review.Acknowledge();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*submitted*");
    }

    [Fact]
    public void Complete_FromAcknowledged_ChangesStatusToCompleted()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();
        review.Acknowledge();

        // Act
        review.Complete();

        // Assert
        review.Status.Should().Be(ReviewStatus.Completed);
    }

    [Fact]
    public void Complete_WhenNotAcknowledged_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        // Act
        var act = () => review.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*acknowledged*");
    }

    [Fact]
    public void Reopen_FromSubmitted_ChangesStatusToDraft()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        // Act
        review.Reopen();

        // Assert
        review.Status.Should().Be(ReviewStatus.Draft);
        review.SubmittedAt.Should().BeNull();
        review.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void Reopen_FromAcknowledged_ChangesStatusToDraft()
    {
        // Arrange
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();
        review.Acknowledge();

        // Act
        review.Reopen();

        // Assert
        review.Status.Should().Be(ReviewStatus.Draft);
    }

    [Fact]
    public void Reopen_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = CreateCompletedReview();

        // Act
        var act = () => review.Reopen();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Completed*");
    }

    private PerformanceReview CreateCompletedReview()
    {
        var review = new PerformanceReview(_validDirectReportId, "2024 Review", DateTime.Now);
        review.UpdateContent("Strength", "Area", "Goal", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();
        review.Acknowledge();
        review.Complete();
        return review;
    }
}
