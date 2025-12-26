using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly Mock<ILogger<ProjectsController>> _loggerMock;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        _loggerMock = new Mock<ILogger<ProjectsController>>();
        _controller = new ProjectsController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAllProjects()
    {
        // Arrange
        var projects = new List<ProjectDto> { CreateDto(), CreateDto("Project 2") };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithProject()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ProjectDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_ReturnsOkWithMatchingProjects()
    {
        // Arrange
        var projects = new List<ProjectDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByStatusAsync(ProjectStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _controller.GetByStatus(ProjectStatus.Active, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>();
    }

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveProjects()
    {
        // Arrange
        var projects = new List<ProjectDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _controller.GetActive(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateProjectDto { Name = "New Project" };
        var resultDto = CreateDto("New Project");
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateProjectDto { Name = "Existing" };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Name already exists"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateProjectDto { Name = "Updated" };
        var resultDto = CreateDto("Updated");
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateProjectDto { Name = "Test" };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Project", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Activate_WhenExists_ReturnsOkWithActivatedProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto();
        resultDto.Status = ProjectStatus.Active;
        _serviceMock.Setup(s => s.ActivateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Activate(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
        dto.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task Activate_WhenInvalidState_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.ActivateAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot activate"));

        // Act
        var result = await _controller.Activate(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutOnHold_ReturnsOkWithUpdatedProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto();
        resultDto.Status = ProjectStatus.OnHold;
        _serviceMock.Setup(s => s.PutOnHoldAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.PutOnHold(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Complete_ReturnsOkWithCompletedProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto();
        resultDto.Status = ProjectStatus.Completed;
        _serviceMock.Setup(s => s.CompleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Complete(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_ReturnsOkWithCancelledProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto();
        resultDto.Status = ProjectStatus.Cancelled;
        _serviceMock.Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Cancel(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

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
            .ThrowsAsync(new NotFoundException("Project", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static ProjectDto CreateDto(string name = "Test Project")
    {
        return new ProjectDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Description",
            Status = ProjectStatus.Planning,
            StatusName = "Planning",
            CreatedAt = DateTime.UtcNow
        };
    }
}
