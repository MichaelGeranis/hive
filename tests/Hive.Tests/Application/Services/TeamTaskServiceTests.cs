using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

public class TeamTaskServiceTests
{
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IAppSettingsRepository> _appSettingsRepositoryMock;
    private readonly TeamTaskService _service;
    private readonly DirectReport _testDirectReport;
    private readonly Project _testProject;

    public TeamTaskServiceTests()
    {
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _appSettingsRepositoryMock = new Mock<IAppSettingsRepository>();
        _service = new TeamTaskService(
            _taskRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _appSettingsRepositoryMock.Object);

        _testDirectReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Eng", DateTime.UtcNow);
        _testProject = new Project("Test Project");
    }

    [Fact]
    public void Constructor_WithNullTaskRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(null!, _directReportRepositoryMock.Object, _projectRepositoryMock.Object, _appSettingsRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("taskRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, null!, _projectRepositoryMock.Object, _appSettingsRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    [Fact]
    public void Constructor_WithNullProjectRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, _directReportRepositoryMock.Object, null!, _appSettingsRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectRepository");
    }

    [Fact]
    public void Constructor_WithNullAppSettingsRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, _directReportRepositoryMock.Object, _projectRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("appSettingsRepository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var task = new TeamTask("Test Task", assigneeId: _testDirectReport.Id, projectId: _testProject.Id);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);

        // Act
        var result = await _service.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be(task.Title);
        result.AssigneeName.Should().Be(_testDirectReport.FullName);
        result.ProjectName.Should().Be(_testProject.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        // Arrange
        var tasks = new List<TeamTask> { new TeamTask("Task 1"), new TeamTask("Task 2") };
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByAssigneeIdAsync_ReturnsMatchingTasks()
    {
        // Arrange
        var tasks = new List<TeamTask> { new TeamTask("Task", assigneeId: _testDirectReport.Id) };
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByAssigneeIdAsync(_testDirectReport.Id);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsMatchingTasks()
    {
        // Arrange
        var tasks = new List<TeamTask> { new TeamTask("Task", projectId: _testProject.Id) };
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);

        // Act
        var result = await _service.GetByProjectIdAsync(_testProject.Id);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var task1 = new TeamTask("Backlog Task");
        var task2 = new TeamTask("Todo Task");
        task2.MoveToTodo();
        var task3 = new TeamTask("In Progress Task");
        task3.Start();
        var task4 = new TeamTask("Done Task");
        task4.Complete();
        var task5 = new TeamTask("Cancelled Task");
        task5.Cancel();

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, task3, task4, task5 });

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        result.TotalTasks.Should().Be(5);
        result.BacklogTasks.Should().Be(1);
        result.TodoTasks.Should().Be(1);
        result.InProgressTasks.Should().Be(1);
        result.DoneTasks.Should().Be(1);
        result.CancelledTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_WithProjectId_FiltersByProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<TeamTask> { new TeamTask("Task", projectId: projectId) };
        _taskRepositoryMock.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetSummaryAsync(projectId);

        // Assert
        result.TotalTasks.Should().Be(1);
        _taskRepositoryMock.Verify(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesTask()
    {
        // Arrange
        var dto = new CreateTeamTaskDto
        {
            Title = "New Task",
            Description = "Description",
            Type = TaskType.Story,
            Priority = TaskPriority.High,
            AssigneeId = _testDirectReport.Id,
            ProjectId = _testProject.Id
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(dto.Title);
        result.Type.Should().Be(TaskType.Story);
        result.Priority.Should().Be(TaskPriority.High);
        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentAssignee_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateTeamTaskDto { Title = "Task", AssigneeId = Guid.NewGuid() };
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.AssigneeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentProject_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateTeamTaskDto { Title = "Task", ProjectId = Guid.NewGuid() };
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(dto.ProjectId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesTask()
    {
        // Arrange
        var task = new TeamTask("Original");
        var dto = new UpdateTeamTaskDto
        {
            Title = "Updated",
            Description = "New desc",
            Type = TaskType.Bug,
            Priority = TaskPriority.Critical
        };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.UpdateAsync(task.Id, dto);

        // Assert
        result.Title.Should().Be(dto.Title);
        result.Type.Should().Be(TaskType.Bug);
        _taskRepositoryMock.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);

        // Act
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateTeamTaskDto { Title = "Test" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignAsync_WithValidAssignee_AssignsTask()
    {
        // Arrange
        var task = new TeamTask("Task");
        var dto = new AssignTaskDto { AssigneeId = _testDirectReport.Id };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.AssignAsync(task.Id, dto);

        // Assert
        result.AssigneeId.Should().Be(_testDirectReport.Id);
    }

    [Fact]
    public async Task AssignAsync_WithNull_UnassignsTask()
    {
        // Arrange
        var task = new TeamTask("Task", assigneeId: _testDirectReport.Id);
        var dto = new AssignTaskDto { AssigneeId = null };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.AssignAsync(task.Id, dto);

        // Assert
        result.AssigneeId.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_UpdatesStatusToInProgress()
    {
        // Arrange
        var task = new TeamTask("Task");
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.StartAsync(task.Id);

        // Assert
        result.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task MoveToReviewAsync_UpdatesStatusToInReview()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.MoveToReviewAsync(task.Id);

        // Assert
        result.Status.Should().Be(TaskStatus.InReview);
    }

    [Fact]
    public async Task CompleteAsync_UpdatesStatusToDone()
    {
        // Arrange
        var task = new TeamTask("Task");
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.CompleteAsync(task.Id);

        // Assert
        result.Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task CancelAsync_UpdatesStatusToCancelled()
    {
        // Arrange
        var task = new TeamTask("Task");
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.CancelAsync(task.Id);

        // Assert
        result.Status.Should().Be(TaskStatus.Cancelled);
    }

    [Fact]
    public async Task ReopenAsync_UpdatesStatusToTodo()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ReopenAsync(task.Id);

        // Assert
        result.Status.Should().Be(TaskStatus.Todo);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.ExistsAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(taskId);

        // Assert
        _taskRepositoryMock.Verify(r => r.DeleteAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.ExistsAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(taskId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
