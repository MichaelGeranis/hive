using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

public class ActivityFeedControllerTests
{
    private readonly Mock<IActivityService> _serviceMock;
    private readonly Mock<ILogger<ActivityFeedController>> _loggerMock;
    private readonly ActivityFeedController _controller;

    public ActivityFeedControllerTests()
    {
        _serviceMock = new Mock<IActivityService>();
        _loggerMock = new Mock<ILogger<ActivityFeedController>>();
        _controller = new ActivityFeedController(_serviceMock.Object, _loggerMock.Object);
    }

    private static ActivityDto CreateDto(string entityName = "Entity", string description = "Description")
    {
        return new ActivityDto
        {
            Id = Guid.NewGuid(),
            ActivityType = "Created",
            ActivityTypeName = "Created",
            EntityType = "Task",
            EntityTypeName = "Task",
            EntityId = Guid.NewGuid(),
            EntityName = entityName,
            Description = description,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============== GetById Tests ==============

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ============== GetAll Tests ==============

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        // Arrange
        var items = new List<ActivityDto> { CreateDto(), CreateDto() };
        var pagination = new ActivityPaginationParams { PageSize = 50 };
        var pagedResult = PagedResult<ActivityDto>.Create(items, 2, pagination);

        _serviceMock.Setup(s => s.SearchAsync(It.IsAny<ActivityPaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(1, 50, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<PagedResult<ActivityDto>>().Subject;
        returned.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithSearchTerm_PassesTermToService()
    {
        // Arrange
        var pagedResult = PagedResult<ActivityDto>.Create(new List<ActivityDto>(), 0,
            new ActivityPaginationParams());
        _serviceMock.Setup(s => s.SearchAsync(It.IsAny<ActivityPaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(1, 50, "sprint", CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.SearchAsync(
            It.Is<ActivityPaginationParams>(p => p.SearchTerm == "sprint"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== GetRecent Tests ==============

    [Fact]
    public async Task GetRecent_ReturnsOkWithRecentActivities()
    {
        // Arrange
        var activities = new List<ActivityDto> { CreateDto("Task A") };
        _serviceMock.Setup(s => s.GetRecentAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await _controller.GetRecent(7, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ActivityDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecent_WithDefaultDays_UsesSevenDays()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActivityDto>());

        // Act
        await _controller.GetRecent(7, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.GetRecentAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== GetByEntityType Tests ==============

    [Fact]
    public async Task GetByEntityType_ReturnsOkWithFilteredActivities()
    {
        // Arrange
        var activities = new List<ActivityDto>
        {
            CreateDto("Sprint 1"),
            CreateDto("Sprint 2")
        };
        _serviceMock.Setup(s => s.GetByEntityTypeAsync(EntityType.Sprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await _controller.GetByEntityType(EntityType.Sprint, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ActivityDto>>().Subject;
        returned.Should().HaveCount(2);
    }
}
