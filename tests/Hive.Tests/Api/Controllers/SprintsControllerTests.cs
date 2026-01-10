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
/// Tests for SprintsController.
/// </summary>
public class SprintsControllerTests
{
    private readonly Mock<ISprintService> _serviceMock;
    private readonly Mock<ILogger<SprintsController>> _loggerMock;
    private readonly SprintsController _controller;

    public SprintsControllerTests()
    {
        _serviceMock = new Mock<ISprintService>();
        _loggerMock = new Mock<ILogger<SprintsController>>();
        _controller = new SprintsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllSprints()
    {
        // Arrange
        var sprints = new List<SprintDto>
        {
            CreateDto("Sprint 1"),
            CreateDto("Sprint 2")
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<SprintDto>>();
        var resultSprints = okResult.Value as IEnumerable<SprintDto>;
        resultSprints.Should().HaveCount(2);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithSprint()
    {
        // Arrange
        var dto = CreateDto("Sprint 1");

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SprintDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByName Tests

    [Fact]
    public async Task GetByName_WhenExists_ReturnsOkWithSprint()
    {
        // Arrange
        var dto = CreateDto("Sprint 1");

        _serviceMock.Setup(s => s.GetByNameAsync(dto.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetByName(dto.Name, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SprintDto>();
    }

    [Fact]
    public async Task GetByName_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintDto?)null);

        // Act
        var result = await _controller.GetByName("NonExistent", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByTeam Tests

    [Fact]
    public async Task GetByTeam_ReturnsOkWithSprints()
    {
        // Arrange
        var team = "Engineering";
        var sprints = new List<SprintDto> { CreateDto("Sprint 1", team) };

        _serviceMock.Setup(s => s.GetByTeamAsync(team, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);

        // Act
        var result = await _controller.GetByTeam(team, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<SprintDto>>();
    }

    #endregion

    #region GetByYear Tests

    [Fact]
    public async Task GetByYear_ReturnsOkWithSprints()
    {
        // Arrange
        var year = 2024;
        var sprints = new List<SprintDto> { CreateDto("Sprint 1") };

        _serviceMock.Setup(s => s.GetByYearAsync(year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);

        // Act
        var result = await _controller.GetByYear(year, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<SprintDto>>();
    }

    #endregion

    #region GetByYearQuarter Tests

    [Fact]
    public async Task GetByYearQuarter_ReturnsOkWithSprints()
    {
        // Arrange
        var year = 2024;
        var quarter = 1;
        var sprints = new List<SprintDto> { CreateDto("Sprint 1") };

        _serviceMock.Setup(s => s.GetByYearQuarterAsync(year, quarter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);

        // Act
        var result = await _controller.GetByYearQuarter(year, quarter, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<SprintDto>>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateSprintDto
        {
            Name = "Sprint 1"
        };
        var resultDto = CreateDto("Sprint 1");

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
        var createDto = new CreateSprintDto
        {
            Name = ""
        };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Name cannot be empty"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

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
            .ThrowsAsync(new NotFoundException("Sprint", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static SprintDto CreateDto(string name, string teamName = "Engineering")
    {
        return new SprintDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            TeamName = teamName,
            Year = 2024,
            Quarter = 1,
            SprintNumber = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
