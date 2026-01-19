using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class PerformanceReviewRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly PerformanceReviewRepository _repository;
    private readonly Guid _directReportId;

    public PerformanceReviewRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new PerformanceReviewRepository(_context);
        _directReportId = Guid.NewGuid();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PerformanceReviewRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var review = CreateAndAddReview();

        // Act
        var result = await _repository.GetByIdAsync(review.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(review.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllReviews()
    {
        // Arrange
        CreateAndAddReview(reviewDate: new DateTime(2024, 1, 1));
        CreateAndAddReview(reviewDate: new DateTime(2024, 6, 1));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByReviewDateDescendingThenCreatedAt()
    {
        // Arrange
        var review1 = CreateAndAddReview(reviewDate: new DateTime(2024, 1, 1));
        await Task.Delay(10);
        var review2 = CreateAndAddReview(reviewDate: new DateTime(2024, 6, 1));
        await Task.Delay(10);
        var review3 = CreateAndAddReview(reviewDate: new DateTime(2024, 6, 1));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(review2.Id); // June, created first
        result[1].Id.Should().Be(review3.Id); // June, created second
        result[2].Id.Should().Be(review1.Id); // January
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingReviews()
    {
        // Arrange
        var reportId1 = Guid.NewGuid();
        var reportId2 = Guid.NewGuid();

        CreateAndAddReview(directReportId: reportId1);
        CreateAndAddReview(directReportId: reportId1);
        CreateAndAddReview(directReportId: reportId2);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(reportId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.DirectReportId.Should().Be(reportId1));
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_OrdersByReviewDateDescending()
    {
        // Arrange
        var review1 = CreateAndAddReview(reviewDate: new DateTime(2024, 1, 1));
        var review2 = CreateAndAddReview(reviewDate: new DateTime(2024, 6, 1));

        // Act
        var result = await _repository.GetByDirectReportIdAsync(_directReportId);

        // Assert
        result[0].Id.Should().Be(review2.Id);
        result[1].Id.Should().Be(review1.Id);
    }

    [Fact]
    public async Task AddAsync_AddsReviewToContext()
    {
        // Arrange
        var review = new PerformanceReview(_directReportId, "2024-Q1", DateTime.UtcNow);

        // Act
        var result = await _repository.AddAsync(review);

        // Assert
        result.Should().Be(review);
        _context.PerformanceReviews.Should().ContainKey(review.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = CreateAndAddReview();

        // Act
        var act = () => _repository.AddAsync(review);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesReviewInContext()
    {
        // Arrange
        var review = CreateAndAddReview();
        review.UpdateContent("Updated strengths", "Updated areas", "Updated notes", PerformanceRating.ExceedsExpectations);

        // Act
        await _repository.UpdateAsync(review);

        // Assert
        var stored = _context.PerformanceReviews[review.Id];
        stored.Strengths.Should().Be("Updated strengths");
        stored.Rating.Should().Be(PerformanceRating.ExceedsExpectations);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var review = new PerformanceReview(_directReportId, "2024-Q1", DateTime.UtcNow);

        // Act
        var act = () => _repository.UpdateAsync(review);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesReviewFromContext()
    {
        // Arrange
        var review = CreateAndAddReview();

        // Act
        await _repository.DeleteAsync(review.Id);

        // Assert
        _context.PerformanceReviews.Should().NotContainKey(review.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var review = CreateAndAddReview();

        // Act
        var result = await _repository.ExistsAsync(review.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        CreateAndAddReview(reviewPeriod: "2024-Q1");

        // Act
        var result = await _repository.HasReviewForPeriodAsync(_directReportId, "2024-Q1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddReview(reviewPeriod: "2024-Q1");

        // Act
        var result = await _repository.HasReviewForPeriodAsync(_directReportId, "2024-q1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        CreateAndAddReview(reviewPeriod: "2024-Q1");

        // Act
        var result = await _repository.HasReviewForPeriodAsync(_directReportId, "2024-Q2");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var review = CreateAndAddReview(reviewPeriod: "2024-Q1");

        // Act
        var result = await _repository.HasReviewForPeriodAsync(_directReportId, "2024-Q1", excludeId: review.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_FindsDuplicateWhenExcludingDifferentId()
    {
        // Arrange
        CreateAndAddReview(reviewPeriod: "2024-Q1");
        var otherId = Guid.NewGuid();

        // Act
        var result = await _repository.HasReviewForPeriodAsync(_directReportId, "2024-Q1", excludeId: otherId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasReviewForPeriodAsync_FiltersByDirectReportId()
    {
        // Arrange
        var otherReportId = Guid.NewGuid();
        CreateAndAddReview(reviewPeriod: "2024-Q1");

        // Act
        var result = await _repository.HasReviewForPeriodAsync(otherReportId, "2024-Q1");

        // Assert
        result.Should().BeFalse();
    }

    private PerformanceReview CreateAndAddReview(
        Guid? directReportId = null,
        string reviewPeriod = "2024-Q1",
        DateTime? reviewDate = null)
    {
        var review = new PerformanceReview(
            directReportId ?? _directReportId,
            reviewPeriod,
            reviewDate ?? DateTime.UtcNow);
        _context.PerformanceReviews.TryAdd(review.Id, review);
        return review;
    }
}
