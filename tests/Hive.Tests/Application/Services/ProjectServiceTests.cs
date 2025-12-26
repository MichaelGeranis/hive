using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _service = new ProjectService(_projectRepositoryMock.Object, _taskRepositoryMock.Object);
    }

    [Fact]
    public void Constructor_WithNullProjectRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(null!, _taskRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectRepository");
    }

    [Fact]
    public void Constructor_WithNullTaskRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(_projectRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("taskRepository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var project = new Project("Test Project", "Description");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetByIdAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
        result.Name.Should().Be(project.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTaskCounts()
    {
        // Arrange
        var project = new Project("Test Project");
        var task1 = new TeamTask("Task 1", projectId: project.Id);
        task1.Complete();
        var task2 = new TeamTask("Task 2", projectId: project.Id);
        task2.Start();
        var task3 = new TeamTask("Task 3", projectId: project.Id);
        task3.Cancel();

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, task3 });

        // Act
        var result = await _service.GetByIdAsync(project.Id);

        // Assert
        result!.TotalTasks.Should().Be(3);
        result.CompletedTasks.Should().Be(1); // Only task1 is Done
        result.OpenTasks.Should().Be(1); // Only task2 is open (InProgress)
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project("Project 1"),
            new Project("Project 2")
        };
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMatchingProjects()
    {
        // Arrange
        var project = new Project("Active Project");
        project.Activate();
        _projectRepositoryMock.Setup(r => r.GetByStatusAsync(ProjectStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetByStatusAsync(ProjectStatus.Active);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsActiveAndPlanningProjects()
    {
        // Arrange
        var projects = new List<Project> { new Project("Project") };
        _projectRepositoryMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesProject()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            Name = "New Project",
            Description = "Description",
            StartDate = DateTime.UtcNow,
            TargetEndDate = DateTime.UtcNow.AddDays(90)
        };

        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Status.Should().Be(ProjectStatus.Planning);
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateProjectDto { Name = "Existing Project" };
        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesProject()
    {
        // Arrange
        var project = new Project("Original");
        var dto = new UpdateProjectDto { Name = "Updated", Description = "New desc" };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.UpdateAsync(project.Id, dto);

        // Assert
        result.Name.Should().Be(dto.Name);
        _projectRepositoryMock.Verify(r => r.UpdateAsync(project, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateProjectDto { Name = "Test" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_UpdatesStatusToActive()
    {
        // Arrange
        var project = new Project("Project");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.ActivateAsync(project.Id);

        // Assert
        result.Status.Should().Be(ProjectStatus.Active);
        _projectRepositoryMock.Verify(r => r.UpdateAsync(project, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PutOnHoldAsync_UpdatesStatusToOnHold()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.PutOnHoldAsync(project.Id);

        // Assert
        result.Status.Should().Be(ProjectStatus.OnHold);
    }

    [Fact]
    public async Task CompleteAsync_UpdatesStatusToCompleted()
    {
        // Arrange
        var project = new Project("Project");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.CompleteAsync(project.Id);

        // Assert
        result.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public async Task CancelAsync_UpdatesStatusToCancelled()
    {
        // Arrange
        var project = new Project("Project");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.CancelAsync(project.Id);

        // Assert
        result.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepositoryMock.Setup(r => r.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(projectId);

        // Assert
        _projectRepositoryMock.Verify(r => r.DeleteAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepositoryMock.Setup(r => r.ExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(projectId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
