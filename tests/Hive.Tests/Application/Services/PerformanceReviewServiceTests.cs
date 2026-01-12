using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class PerformanceReviewServiceTests
{
    private readonly Mock<IPerformanceReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly PerformanceReviewService _service;
    private readonly DirectReport _testDirectReport;

    public PerformanceReviewServiceTests()
    {
        _reviewRepositoryMock = new Mock<IPerformanceReviewRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new PerformanceReviewService(_reviewRepositoryMock.Object, _directReportRepositoryMock.Object, _activityServiceMock.Object);
        _testDirectReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Eng", DateTime.UtcNow.AddYears(-1));
    }

    [Fact]
    public void Constructor_WithNullReviewRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PerformanceReviewService(null!, _directReportRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reviewRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PerformanceReviewService(_reviewRepositoryMock.Object, null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Review", DateTime.UtcNow);
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByIdAsync(review.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(review.Id);
        result.DirectReportName.Should().Be(_testDirectReport.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReview?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var reviews = new List<PerformanceReview>
        {
            new PerformanceReview(_testDirectReport.Id, "2024 Q1", DateTime.UtcNow),
            new PerformanceReview(_testDirectReport.Id, "2024 Q2", DateTime.UtcNow)
        };
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingReviews()
    {
        // Arrange
        var reviews = new List<PerformanceReview>
        {
            new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow)
        };
        _reviewRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });

        // Act
        var result = await _service.GetByDirectReportIdAsync(_testDirectReport.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportId.Should().Be(_testDirectReport.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMatchingReviews()
    {
        // Arrange
        var reviews = new List<PerformanceReview>
        {
            new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow)
        };
        _reviewRepositoryMock.Setup(r => r.GetByStatusAsync(ReviewStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });

        // Act
        var result = await _service.GetByStatusAsync(ReviewStatus.Draft);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesReview()
    {
        // Arrange
        var dto = new CreatePerformanceReviewDto
        {
            DirectReportId = _testDirectReport.Id,
            ReviewPeriod = "2024 Annual",
            ReviewDate = DateTime.UtcNow
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _reviewRepositoryMock.Setup(r => r.HasReviewForPeriodAsync(_testDirectReport.Id, dto.ReviewPeriod, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReview entity, CancellationToken _) => entity);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ReviewPeriod.Should().Be(dto.ReviewPeriod);
        result.Status.Should().Be(ReviewStatus.Draft);
        _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDirectReport_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreatePerformanceReviewDto
        {
            DirectReportId = Guid.NewGuid(),
            ReviewPeriod = "2024 Annual",
            ReviewDate = DateTime.UtcNow
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatePeriod_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreatePerformanceReviewDto
        {
            DirectReportId = _testDirectReport.Id,
            ReviewPeriod = "2024 Annual",
            ReviewDate = DateTime.UtcNow
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _reviewRepositoryMock.Setup(r => r.HasReviewForPeriodAsync(_testDirectReport.Id, dto.ReviewPeriod, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task SubmitAsync_UpdatesStatusToSubmitted()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow);
        review.UpdateContent("Strong", "Areas", "Goals", "Notes", PerformanceRating.MeetsExpectations);

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.SubmitAsync(review.Id);

        // Assert
        result.Status.Should().Be(ReviewStatus.Submitted);
        _reviewRepositoryMock.Verify(r => r.UpdateAsync(review, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcknowledgeAsync_UpdatesStatusToAcknowledged()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow);
        review.UpdateContent("Strong", "Areas", "Goals", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.AcknowledgeAsync(review.Id);

        // Assert
        result.Status.Should().Be(ReviewStatus.Acknowledged);
    }

    [Fact]
    public async Task CompleteAsync_UpdatesStatusToCompleted()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow);
        review.UpdateContent("Strong", "Areas", "Goals", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();
        review.Acknowledge();

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.CompleteAsync(review.Id);

        // Assert
        result.Status.Should().Be(ReviewStatus.Completed);
    }

    [Fact]
    public async Task ReopenAsync_UpdatesStatusToDraft()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow);
        review.UpdateContent("Strong", "Areas", "Goals", "Notes", PerformanceRating.MeetsExpectations);
        review.Submit();

        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.ReopenAsync(review.Id);

        // Assert
        result.Status.Should().Be(ReviewStatus.Draft);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesReview()
    {
        // Arrange
        var review = new PerformanceReview(_testDirectReport.Id, "2024 Annual", DateTime.UtcNow);
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        // Act
        await _service.DeleteAsync(review.Id);

        // Assert
        _reviewRepositoryMock.Verify(r => r.DeleteAsync(review.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReview?)null);

        // Act
        var act = () => _service.DeleteAsync(reviewId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
