using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for PerformanceReviewsController.
/// </summary>
public class PerformanceReviewsControllerTests
{
    private readonly Mock<IPerformanceReviewService> _serviceMock;
    private readonly Mock<ILogger<PerformanceReviewsController>> _loggerMock;
    private readonly PerformanceReviewsController _controller;

    public PerformanceReviewsControllerTests()
    {
        _serviceMock = new Mock<IPerformanceReviewService>();
        _loggerMock = new Mock<ILogger<PerformanceReviewsController>>();
        _controller = new PerformanceReviewsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllReviews()
    {
        // Arrange
        var reviews = new List<PerformanceReviewDto>
        {
            CreateDto(),
            CreateDto()
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<PerformanceReviewDto>>();
        var resultReviews = okResult.Value as IEnumerable<PerformanceReviewDto>;
        resultReviews.Should().HaveCount(2);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithReview()
    {
        // Arrange
        var dto = CreateDto();

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<PerformanceReviewDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReviewDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByDirectReport Tests

    [Fact]
    public async Task GetByDirectReport_ReturnsOkWithReviews()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var reviews = new List<PerformanceReviewDto> { CreateDto() };

        _serviceMock.Setup(s => s.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetByDirectReport(directReportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<PerformanceReviewDto>>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreatePerformanceReviewDto
        {
            DirectReportId = Guid.NewGuid(),
            ReviewPeriod = "2024 Annual Review",
            ReviewDate = new DateTime(2024, 12, 31)
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreatePerformanceReviewDto
        {
            DirectReportId = Guid.NewGuid(),
            ReviewPeriod = "",
            ReviewDate = DateTime.UtcNow
        };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Review period cannot be empty"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenDirectReportNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreatePerformanceReviewDto
        {
            DirectReportId = Guid.NewGuid(),
            ReviewPeriod = "2024 Annual",
            ReviewDate = DateTime.UtcNow
        };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("DirectReport", createDto.DirectReportId));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateContent Tests

    [Fact]
    public async Task UpdateContent_WhenExists_ReturnsOkWithUpdatedReview()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdatePerformanceReviewContentDto
        {
            ManagerNotes = "Updated notes",
            Strengths = "Great communication",
            AreasForImprovement = "Time management",
            Rating = PerformanceRating.MeetsExpectations
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.UpdateContentAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.UpdateContent(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateContent_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdatePerformanceReviewContentDto();

        _serviceMock.Setup(s => s.UpdateContentAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("PerformanceReview", id));

        // Act
        var result = await _controller.UpdateContent(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("PerformanceReview", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static PerformanceReviewDto CreateDto()
    {
        return new PerformanceReviewDto
        {
            Id = Guid.NewGuid(),
            DirectReportId = Guid.NewGuid(),
            DirectReportName = "John Doe",
            ReviewPeriod = "2024 Annual Review",
            ReviewDate = new DateTime(2024, 12, 31),
            Rating = PerformanceRating.MeetsExpectations,
            RatingDescription = "Meets Expectations",
            Strengths = "Good performance",
            AreasForImprovement = "Time management",
            ManagerNotes = "Manager notes",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
