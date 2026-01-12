using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class SprintServiceTests
{
    private readonly Mock<ISprintRepository> _repositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly SprintService _service;

    public SprintServiceTests()
    {
        _repositoryMock = new Mock<ISprintRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new SprintService(_repositoryMock.Object, _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintService(null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sprintRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintService(_repositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new Sprint("LP_4Q25_S6");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("LP_4Q25_S6");
        result.TeamName.Should().Be("LP");
        result.Quarter.Should().Be(4);
        result.Year.Should().Be(2025);
        result.SprintNumber.Should().Be(6);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new Sprint("LP_4Q25_S6");
        _repositoryMock.Setup(r => r.GetByNameAsync("LP_4Q25_S6", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByNameAsync("LP_4Q25_S6");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("LP_4Q25_S6");
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _service.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var entities = new List<Sprint>
        {
            new Sprint("LP_4Q25_S6"),
            new Sprint("LP_4Q25_S7")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("LP_4Q25_S6");
        result[1].Name.Should().Be("LP_4Q25_S7");
    }

    [Fact]
    public async Task GetByTeamAsync_ReturnsTeamSprints()
    {
        // Arrange
        var entities = new List<Sprint>
        {
            new Sprint("LP_4Q25_S6"),
            new Sprint("LP_4Q25_S7")
        };
        _repositoryMock.Setup(r => r.GetByTeamAsync("LP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByTeamAsync("LP");

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.TeamName == "LP").Should().BeTrue();
    }

    [Fact]
    public async Task GetByYearQuarterAsync_ReturnsQuarterSprints()
    {
        // Arrange
        var entities = new List<Sprint>
        {
            new Sprint("LP_4Q25_S6"),
            new Sprint("LP_4Q25_S7")
        };
        _repositoryMock.Setup(r => r.GetByYearQuarterAsync(2025, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByYearQuarterAsync(2025, 4);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.Year == 2025 && s.Quarter == 4).Should().BeTrue();
    }

    [Fact]
    public async Task GetByYearAsync_ReturnsYearSprints()
    {
        // Arrange
        var entities = new List<Sprint>
        {
            new Sprint("LP_1Q25_S1"),
            new Sprint("LP_4Q25_S6")
        };
        _repositoryMock.Setup(r => r.GetByYearAsync(2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByYearAsync(2025);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.Year == 2025).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSprint()
    {
        // Arrange
        var dto = new CreateSprintDto { Name = "LP_4Q25_S8" };
        _repositoryMock.Setup(r => r.ExistsAsync(dto.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint sprint, CancellationToken _) => sprint);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("LP_4Q25_S8");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameExists_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateSprintDto { Name = "LP_4Q25_S6" };
        _repositoryMock.Setup(r => r.ExistsAsync(dto.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenExists_ReturnsExisting()
    {
        // Arrange
        var entity = new Sprint("LP_4Q25_S6");
        _repositoryMock.Setup(r => r.GetByNameAsync("LP_4Q25_S6", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetOrCreateAsync("LP_4Q25_S6");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("LP_4Q25_S6");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNotExists_CreatesNew()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByNameAsync("LP_4Q25_S9", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint sprint, CancellationToken _) => sprint);

        // Act
        var result = await _service.GetOrCreateAsync("LP_4Q25_S9");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("LP_4Q25_S9");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.GetOrCreateAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Sprint name cannot be empty.*");
    }

    [Fact]
    public async Task DeleteAsync_WhenSprintExists_DeletesSprint()
    {
        // Arrange
        var entity = new Sprint("LP_4Q25_S6");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSprintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
