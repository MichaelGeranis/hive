using Hive.Application.DTOs;
using Hive.Application.Interfaces;
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
    private readonly Mock<IParentRepository> _parentRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly TeamTaskService _service;
    private readonly DirectReport _testDirectReport;
    private readonly Project _testProject;
    private readonly Mock<IAppSettingsRepository> _appSettingsRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock = new();
    private readonly Mock<ITaskReadinessReviewRepository> _reviewRepositoryMock = new();

    public TeamTaskServiceTests()
    {
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _parentRepositoryMock = new Mock<IParentRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _appSettingsRepositoryMock = new Mock<IAppSettingsRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _service = new TeamTaskService(
            _taskRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _parentRepositoryMock.Object,
            _activityServiceMock.Object,
            _appSettingsRepositoryMock.Object,
            _productRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _reviewRepositoryMock.Object);

        _testDirectReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Eng", DateTime.UtcNow);
        _testProject = new Project("Test Project");
    }

    [Fact]
    public void Constructor_WithNullTaskRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(null!, _directReportRepositoryMock.Object, _projectRepositoryMock.Object, _parentRepositoryMock.Object, _activityServiceMock.Object, new Mock<IAppSettingsRepository>().Object, _productRepositoryMock.Object, _sprintRepositoryMock.Object, _reviewRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("taskRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, null!, _projectRepositoryMock.Object, _parentRepositoryMock.Object, _activityServiceMock.Object, new Mock<IAppSettingsRepository>().Object, _productRepositoryMock.Object, _sprintRepositoryMock.Object, _reviewRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    [Fact]
    public void Constructor_WithNullProjectRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, _directReportRepositoryMock.Object, null!, _parentRepositoryMock.Object, _activityServiceMock.Object, new Mock<IAppSettingsRepository>().Object, _productRepositoryMock.Object, _sprintRepositoryMock.Object, _reviewRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectRepository");
    }

    [Fact]
    public void Constructor_WithNullParentRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskService(_taskRepositoryMock.Object, _directReportRepositoryMock.Object, _projectRepositoryMock.Object, null!, _activityServiceMock.Object, new Mock<IAppSettingsRepository>().Object, _productRepositoryMock.Object, _sprintRepositoryMock.Object, _reviewRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("parentRepository");
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
    public async Task GetSummaryAsync_ListsEachSprintOnceNewestFirst()
    {
        // Arrange — carried-over work lists every sprint it was in
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>
            {
                new("A", sprint: "MAR_3Q26_S5,MAR_3Q26_S6"),
                new("B", sprint: "MAR_2Q26_S4,MAR_3Q26_S10"),
                new("C", sprint: "MAR_3Q26_S6"),
                new("D")
            });

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert — sprint order, not alphabetical (S10 comes after S6)
        result.AllSprints.Should().Equal("MAR_3Q26_S10", "MAR_3Q26_S6", "MAR_3Q26_S5", "MAR_2Q26_S4");
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
        var task = new TeamTask("Test Task", "Description", TaskType.Task, TaskPriority.High);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        await _service.DeleteAsync(task.Id);

        // Assert
        _taskRepositoryMock.Verify(r => r.DeleteAsync(task.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);

        // Act
        var act = () => _service.DeleteAsync(taskId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #region Override Tests

    [Fact]
    public async Task OverrideFieldsAsync_WithAssignee_ValidatesAndSetsOverride()
    {
        // Arrange
        var task = new TeamTask("Task");
        var dto = new OverrideTeamTaskFieldsDto
        {
            AssigneeId = _testDirectReport.Id,
            HasAssigneeOverride = true
        };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.OverrideFieldsAsync(task.Id, dto);

        // Assert
        result.AssigneeId.Should().Be(_testDirectReport.Id);
        result.OverriddenFields.Should().Contain("AssigneeId");
        _taskRepositoryMock.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OverrideFieldsAsync_WithInvalidAssignee_ThrowsNotFoundException()
    {
        // Arrange
        var task = new TeamTask("Task");
        var invalidId = Guid.NewGuid();
        var dto = new OverrideTeamTaskFieldsDto
        {
            AssigneeId = invalidId,
            HasAssigneeOverride = true
        };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.OverrideFieldsAsync(task.Id, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task OverrideFieldsAsync_WithStoryPoints_SetsOverride()
    {
        // Arrange
        var task = new TeamTask("Task");
        var dto = new OverrideTeamTaskFieldsDto
        {
            StoryPoints = 5,
            HasStoryPointsOverride = true
        };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.OverrideFieldsAsync(task.Id, dto);

        // Assert
        result.StoryPoints.Should().Be(5);
        result.OverriddenFields.Should().Contain("StoryPoints");
    }

    [Fact]
    public async Task OverrideFieldsAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);

        // Act
        var act = () => _service.OverrideFieldsAsync(Guid.NewGuid(), new OverrideTeamTaskFieldsDto { HasAssigneeOverride = true });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ClearOverridesAsync_ClearsSpecifiedFields()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.OverrideAssignee(Guid.NewGuid());
        task.OverrideStoryPoints(5);
        var dto = new ClearTeamTaskOverridesDto { Fields = new List<string> { "AssigneeId" } };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ClearOverridesAsync(task.Id, dto);

        // Assert
        task.IsFieldOverridden("AssigneeId").Should().BeFalse();
        task.IsFieldOverridden("StoryPoints").Should().BeTrue();
        _taskRepositoryMock.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearOverridesAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);

        // Act
        var act = () => _service.ClearOverridesAsync(Guid.NewGuid(), new ClearTeamTaskOverridesDto { Fields = new List<string> { "AssigneeId" } });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    [Fact]
    public async Task GetSummaryAsync_CountsStaleTasksAgainstTheConfiguredThreshold()
    {
        // Arrange
        var settings = new AppSettings();
        settings.UpdateStaleTaskDays(3);
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var stuck = new TeamTask("Stuck");
        stuck.Start();
        SetStatusChangedAt(stuck, DateTime.UtcNow.AddDays(-4));
        var fresh = new TeamTask("Fresh");
        fresh.Start();
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([stuck, fresh]);

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        result.StaleTasks.Should().Be(1);
        result.StaleTaskDays.Should().Be(3);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Stale_AsksForTasksUnchangedSinceTheThreshold()
    {
        // Arrange — no settings row: the default threshold applies
        TeamTaskFilter? passed = null;
        _taskRepositoryMock.Setup(r => r.GetFilteredPagedAsync(0, 20, It.IsAny<TeamTaskFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, TeamTaskFilter, CancellationToken>((_, _, f, _) => passed = f)
            .ReturnsAsync((new List<TeamTask>(), 0, 0));
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        await _service.GetFilteredPagedAsync(new TaskPaginationParams
        {
            Filter = TaskFilter.Stale,
            Type = TaskType.Bug,
            Component = "Matching",
            Unassigned = true
        });

        // Assert
        passed!.StaleBefore.Should().BeCloseTo(DateTime.UtcNow.AddDays(-TeamTask.DefaultStaleAfterDays), TimeSpan.FromMinutes(1));
        passed.Overdue.Should().BeFalse();
        passed.Type.Should().Be(TaskType.Bug);
        passed.Component.Should().Be("Matching");
        passed.Unassigned.Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Unattributed_AsksForTasksNamingNoneOfTheProducts()
    {
        // Arrange
        var products = new List<Product> { new("Matching"), new("Rights Manager") };
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);
        TeamTaskFilter? passed = null;
        _taskRepositoryMock.Setup(r => r.GetFilteredPagedAsync(0, 20, It.IsAny<TeamTaskFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, TeamTaskFilter, CancellationToken>((_, _, f, _) => passed = f)
            .ReturnsAsync((new List<TeamTask>(), 0, 0));

        // Act
        await _service.GetFilteredPagedAsync(new TaskPaginationParams { Unattributed = true });

        // Assert
        passed!.UnattributedAmong.Should().BeEquivalentTo(products);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Unestimated_AsksForTheTasksMissingStoryPoints()
    {
        // Arrange — a story sized by its sub-task, a story that is not, and a support ticket
        var sized = new TeamTask("Sized", assigneeId: _testDirectReport.Id, type: TaskType.Story, tags: "jira:MAR-1");
        var sizedParent = Parent.FromJira("MAR-1", "Story", "Sized");
        var unsized = new TeamTask("Unsized", assigneeId: _testDirectReport.Id, type: TaskType.Story);
        var support = new TeamTask("Support", assigneeId: _testDirectReport.Id, type: TaskType.Support);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sized, new TeamTask("Sub-task", assigneeId: _testDirectReport.Id, storyPoints: 2, parentId: sizedParent.Id), unsized, support]);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([sizedParent]);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_testDirectReport]);
        TeamTaskFilter? passed = null;
        _taskRepositoryMock.Setup(r => r.GetFilteredPagedAsync(0, 20, It.IsAny<TeamTaskFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, TeamTaskFilter, CancellationToken>((_, _, f, _) => passed = f)
            .ReturnsAsync((new List<TeamTask>(), 0, 0));

        // Act
        await _service.GetFilteredPagedAsync(new TaskPaginationParams { Unestimated = true });

        // Assert
        passed!.Ids.Should().BeEquivalentTo([unsized.Id]);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_NotUnattributed_DoesNotFilterByProduct()
    {
        // Arrange
        TeamTaskFilter? passed = null;
        _taskRepositoryMock.Setup(r => r.GetFilteredPagedAsync(0, 20, It.IsAny<TeamTaskFilter>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, TeamTaskFilter, CancellationToken>((_, _, f, _) => passed = f)
            .ReturnsAsync((new List<TeamTask>(), 0, 0));

        // Act
        await _service.GetFilteredPagedAsync(new TaskPaginationParams());

        // Assert
        passed!.UnattributedAmong.Should().BeNull();
        _productRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetStatusChangedAt(TeamTask task, DateTime value) =>
        typeof(TeamTask).GetProperty(nameof(TeamTask.StatusChangedAt))!.SetValue(task, value);

    [Fact]
    public async Task GetFilteredPagedAsync_NoAcceptanceCriteria_AsksForTheUpcomingTasksWithout()
    {
        // Arrange
        var current = new Sprint("MAR_3Q26_S6");
        current.UpdateDates(DateTime.UtcNow.Date.AddDays(-3), DateTime.UtcNow.Date.AddDays(10));
        var vague = new TeamTask("Vague story", "As a user", TaskType.Story, sprint: current.Name);
        var clear = new TeamTask("Clear story", "## Acceptance criteria", TaskType.Story, sprint: current.Name);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([vague, clear]);
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([current]);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        TeamTaskFilter? used = null;
        _taskRepositoryMock
            .Setup(r => r.GetFilteredPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TeamTaskFilter>(), It.IsAny<CancellationToken>()))
            .Callback((int _, int _, TeamTaskFilter filter, CancellationToken _) => used = filter)
            .ReturnsAsync((new List<TeamTask> { vague }, 1, 0));

        // Act
        await _service.GetFilteredPagedAsync(new TaskPaginationParams { NoAcceptanceCriteria = true });

        // Assert
        used!.Ids.Should().BeEquivalentTo([vague.Id]);
    }

    [Fact]
    public async Task GetByIdAsync_ReviewedTask_CarriesItsAiReview()
    {
        // Arrange
        var task = new TeamTask("Save drafts", "As a user", TaskType.Story);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _reviewRepositoryMock.Setup(r => r.GetByTaskIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskReadinessReview(task.Id, TaskReadinessVerdict.NeedsWork, "Keeps drafts.", "[\"Acceptance criteria\"]", "llama3.1:8b", TaskReadiness.Fingerprint(task)));

        // Act
        var dto = await _service.GetByIdAsync(task.Id);

        // Assert
        dto!.AiReview.Should().NotBeNull();
        dto.AiReview!.Verdict.Should().Be("NeedsWork");
        dto.AiReview.Missing.Should().Equal("Acceptance criteria");
        dto.AiReview.IsUpToDate.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NeverReviewed_HasNoAiReview()
    {
        // Arrange
        var task = new TeamTask("Save drafts");
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var dto = await _service.GetByIdAsync(task.Id);

        // Assert
        dto!.AiReview.Should().BeNull();
    }
}
