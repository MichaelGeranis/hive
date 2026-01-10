using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for SprintCapacityController.
/// </summary>
public class SprintCapacityControllerTests
{
    private readonly Mock<ISprintCapacityService> _serviceMock;
    private readonly Mock<ILogger<SprintCapacityController>> _loggerMock;
    private readonly SprintCapacityController _controller;

    public SprintCapacityControllerTests()
    {
        _serviceMock = new Mock<ISprintCapacityService>();
        _loggerMock = new Mock<ILogger<SprintCapacityController>>();
        _controller = new SprintCapacityController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllCapacities()
    {
        // Arrange
        var capacities = new List<SprintCapacityDto>
        {
            CreateDto(),
            CreateDto()
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(capacities);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<SprintCapacityDto>>();
        var resultCapacities = okResult.Value as IEnumerable<SprintCapacityDto>;
        resultCapacities.Should().HaveCount(2);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithCapacity()
    {
        // Arrange
        var dto = CreateDto();

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SprintCapacityDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacityDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetBySprintId Tests

    [Fact]
    public async Task GetBySprintId_WhenExists_ReturnsOkWithCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var dto = CreateDto(sprintId);

        _serviceMock.Setup(s => s.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetBySprintId(sprintId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SprintCapacityDto>();
    }

    [Fact]
    public async Task GetBySprintId_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        _serviceMock.Setup(s => s.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacityDto?)null);

        // Act
        var result = await _controller.GetBySprintId(sprintId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateOrUpdate Tests

    [Fact]
    public async Task CreateOrUpdate_WithValidDto_ReturnsOkWithCapacity()
    {
        // Arrange
        var createDto = new CreateSprintCapacityDto
        {
            SprintId = Guid.NewGuid(),
            TotalCapacityPoints = 80,
            AvailableMembers = 5
        };
        var resultDto = CreateDto(createDto.SprintId);

        _serviceMock.Setup(s => s.CreateOrUpdateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.CreateOrUpdate(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SprintCapacityDto>();
    }

    [Fact]
    public async Task CreateOrUpdate_WhenSprintNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateSprintCapacityDto
        {
            SprintId = Guid.NewGuid(),
            TotalCapacityPoints = 80,
            AvailableMembers = 5
        };

        _serviceMock.Setup(s => s.CreateOrUpdateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Sprint", createDto.SprintId));

        // Act
        var result = await _controller.CreateOrUpdate(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateOrUpdate_WithInvalidCapacity_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSprintCapacityDto
        {
            SprintId = Guid.NewGuid(),
            TotalCapacityPoints = -10,
            AvailableMembers = 5
        };

        _serviceMock.Setup(s => s.CreateOrUpdateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Capacity must be positive"));

        // Act
        var result = await _controller.CreateOrUpdate(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
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
            .ThrowsAsync(new NotFoundException("SprintCapacity", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static SprintCapacityDto CreateDto(Guid? sprintId = null)
    {
        return new SprintCapacityDto
        {
            Id = Guid.NewGuid(),
            SprintId = sprintId ?? Guid.NewGuid(),
            SprintName = "Sprint 1",
            TotalCapacityPoints = 80,
            AvailableMembers = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
