using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class SprintCapacityServiceTests
{
    private readonly Mock<ISprintCapacityRepository> _capacityRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly SprintCapacityService _service;

    public SprintCapacityServiceTests()
    {
        _capacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _service = new SprintCapacityService(_capacityRepositoryMock.Object, _sprintRepositoryMock.Object);
    }

    [Fact]
    public void Constructor_WithNullCapacityRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityService(null!, _sprintRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("capacityRepository");
    }

    [Fact]
    public void Constructor_WithNullSprintRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityService(_capacityRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sprintRepository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var entity = new SprintCapacity(sprintId, 100, 5);

        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.SprintId.Should().Be(sprintId);
        result.TotalCapacityPoints.Should().Be(100);
        result.AvailableMembers.Should().Be(5);
        result.SprintName.Should().Be("LP_4Q25_S6");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var entity = new SprintCapacity(sprintId, 100, 5);

        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);

        // Act
        var result = await _service.GetBySprintIdAsync(sprintId);

        // Assert
        result.Should().NotBeNull();
        result!.SprintId.Should().Be(sprintId);
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var result = await _service.GetBySprintIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var sprint1Id = Guid.NewGuid();
        var sprint2Id = Guid.NewGuid();
        var sprint1 = new Sprint("LP_4Q25_S6");
        var sprint2 = new Sprint("LP_4Q25_S7");
        var entities = new List<SprintCapacity>
        {
            new SprintCapacity(sprint1Id, 100, 5),
            new SprintCapacity(sprint2Id, 120, 6)
        };

        _capacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint1);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint2);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].TotalCapacityPoints.Should().Be(100);
        result[1].TotalCapacityPoints.Should().Be(120);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenSprintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateSprintCapacityDto
        {
            SprintId = Guid.NewGuid(),
            TotalCapacityPoints = 100,
            AvailableMembers = 5
        };
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(dto.SprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var act = async () => await _service.CreateOrUpdateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenCapacityNotExists_CreatesNew()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var dto = new CreateSprintCapacityDto
        {
            SprintId = sprintId,
            TotalCapacityPoints = 100,
            AvailableMembers = 5
        };

        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);
        _capacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity capacity, CancellationToken _) => capacity);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TotalCapacityPoints.Should().Be(100);
        result.AvailableMembers.Should().Be(5);
        _capacityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Once);
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenCapacityExists_UpdatesExisting()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var existing = new SprintCapacity(sprintId, 100, 5);
        var dto = new CreateSprintCapacityDto
        {
            SprintId = sprintId,
            TotalCapacityPoints = 120,
            AvailableMembers = 6
        };

        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TotalCapacityPoints.Should().Be(120);
        result.AvailableMembers.Should().Be(6);
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Once);
        _capacityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCapacityExists_DeletesCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var entity = new SprintCapacity(sprintId, 100, 5);
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _capacityRepositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCapacityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
