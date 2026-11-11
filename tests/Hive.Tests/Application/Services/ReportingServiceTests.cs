using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Formatting;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for ReportingService.
/// </summary>
public class ReportingServiceTests
{
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ISprintCapacityRepository> _sprintCapacityRepositoryMock;
    private readonly Mock<IAppSettingsRepository> _appSettingsRepositoryMock;
    private readonly Mock<IParentRepository> _parentRepositoryMock;
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IManagerNoteRepository> _noteRepositoryMock;
    private readonly Mock<INoteFolderRepository> _noteFolderRepositoryMock;
    private readonly Mock<ICareerPlanRepository> _careerPlanRepositoryMock;
    private readonly Mock<ICareerPlanSkillRepository> _careerPlanSkillRepositoryMock;
    private readonly ReportingService _service;

    private readonly DirectReport _testDirectReport;
    private readonly Guid _testDirectReportId = Guid.NewGuid();

    public ReportingServiceTests()
    {
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _sprintCapacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _appSettingsRepositoryMock = new Mock<IAppSettingsRepository>();
        _parentRepositoryMock = new Mock<IParentRepository>();
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _noteRepositoryMock = new Mock<IManagerNoteRepository>();
        _noteFolderRepositoryMock = new Mock<INoteFolderRepository>();
        _careerPlanRepositoryMock = new Mock<ICareerPlanRepository>();
        _careerPlanRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _careerPlanSkillRepositoryMock = new Mock<ICareerPlanSkillRepository>();
        _careerPlanSkillRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _service = new ReportingService(
            _directReportRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _sprintCapacityRepositoryMock.Object,
            _appSettingsRepositoryMock.Object,
            _parentRepositoryMock.Object,
            _leaveRepositoryMock.Object,
            _productRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _noteFolderRepositoryMock.Object,
            _careerPlanRepositoryMock.Object,
            _careerPlanSkillRepositoryMock.Object);

        _testDirectReport = new DirectReport(
            "John",
            "Doe",
            "john.doe@test.com",
            "Software Engineer",
            "Engineering",
            new DateTime(2020, 1, 1));

        var idProperty = typeof(DirectReport).GetProperty("Id");
        idProperty!.SetValue(_testDirectReport, _testDirectReportId);
    }

    #region GetDashboardOverviewAsync Tests

    [Fact]
    public async Task GetDashboardOverviewAsync_ReturnsCompleteOverview()
    {
        // Arrange
        SetupBasicMocks();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Should().NotBeNull();
        result.Team.Should().NotBeNull();
        result.Tasks.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_WithSprintCount_PassesToTasksAnalytics()
    {
        // Arrange
        SetupBasicMocks();

        // Act
        await _service.GetDashboardOverviewAsync(sprintCount: 3);

        // Assert
        // The fact that this completes without error indicates the sprint count was handled
        _sprintRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region GetTasksAnalyticsAsync Tests

    [Fact]
    public async Task GetTasksAnalyticsAsync_WithNoTasks_ReturnsEmptyStats()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Tasks.TotalTasks.Should().Be(0);
        result.Tasks.CompletionRate.Should().Be(0);
        result.TasksByAssignee.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_CalculatesTaskStatusCounts()
    {
        // Arrange
        var tasks = new List<TeamTask>
        {
            CreateTask("T1", TaskStatus.Backlog),
            CreateTask("T2", TaskStatus.InProgress),
            CreateTask("T3", TaskStatus.Done),
            CreateTask("T4", TaskStatus.Done)
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Tasks.TotalTasks.Should().Be(4);
        result.Tasks.BacklogTasks.Should().Be(1);
        result.Tasks.InProgressTasks.Should().Be(1);
        result.Tasks.DoneTasks.Should().Be(2);
        result.Tasks.CompletionRate.Should().Be(50.0); // 2/4 = 50%
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_ExcludesParentTasks()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var tasks = new List<TeamTask>
        {
            CreateTask("Epic 1", TaskStatus.Done), // This is also a parent - should be excluded
            CreateTask("Task 1", TaskStatus.Done),
            CreateTask("Task 2", TaskStatus.InProgress)
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Tasks.TotalTasks.Should().Be(2); // Excludes "Epic 1" task
        result.Tasks.DoneTasks.Should().Be(1); // Only "Task 1"
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_GroupsByAssignee()
    {
        // Arrange
        var assignee1Id = Guid.NewGuid();
        var assignee2Id = Guid.NewGuid();

        var dr1 = new DirectReport("Alice", "Brown", "alice@test.com", "Dev", "Eng", DateTime.UtcNow.AddYears(-2));
        var dr2 = new DirectReport("Bob", "White", "bob@test.com", "Dev", "Eng", DateTime.UtcNow.AddYears(-1));

        var dr1IdProp = typeof(DirectReport).GetProperty("Id");
        dr1IdProp!.SetValue(dr1, assignee1Id);
        var dr2IdProp = typeof(DirectReport).GetProperty("Id");
        dr2IdProp!.SetValue(dr2, assignee2Id);

        var tasks = new List<TeamTask>
        {
            CreateTaskWithAssignee("T1", TaskStatus.Done, assignee1Id),
            CreateTaskWithAssignee("T2", TaskStatus.InProgress, assignee1Id),
            CreateTaskWithAssignee("T3", TaskStatus.Done, assignee2Id)
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { dr1, dr2 });
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.TasksByAssignee.Should().HaveCount(2);

        var alice = result.TasksByAssignee.First(a => a.AssigneeId == assignee1Id);
        alice.TotalTasks.Should().Be(2);
        alice.CompletedTasks.Should().Be(1);
        alice.CompletionRate.Should().Be(50);

        var bob = result.TasksByAssignee.First(a => a.AssigneeId == assignee2Id);
        bob.TotalTasks.Should().Be(1);
        bob.CompletedTasks.Should().Be(1);
        bob.CompletionRate.Should().Be(100);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_GroupsByAssignee_SumsStoryPointsNetOfCarryOver()
    {
        // Arrange
        var carried = CreateLabelledTask("Carried", TaskStatus.Done, storyPoints: 5, assigneeId: _testDirectReportId);
        carried.OverridePreviousSprintsStoryPoints(2);
        SetupTasksAnalytics(
            carried,
            CreateLabelledTask("Open", TaskStatus.InProgress, storyPoints: 3, assigneeId: _testDirectReportId));

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        var john = result.TasksByAssignee.Single();
        john.TotalStoryPoints.Should().Be(6, "the 2 points delivered in an earlier sprint are not counted again");
        john.CompletedStoryPoints.Should().Be(3);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_SupportAndMaintenance_AreMeasuredInStoryPoints()
    {
        // Arrange — support by its task type (a "support" label alone does not count), maintenance by label
        var settings = new AppSettings();
        settings.UpdateMaintenanceLabels("[\"maintenance\"]");
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        SetupTasksAnalytics(
            CreateLabelledTask("Support done", TaskStatus.Done, storyPoints: 3, assigneeId: _testDirectReportId, type: TaskType.Support),
            CreateLabelledTask("Support open", TaskStatus.InProgress, storyPoints: 2, assigneeId: _testDirectReportId, type: TaskType.Support),
            CreateLabelledTask("Labelled only", TaskStatus.Done, storyPoints: 4, labels: "support", assigneeId: _testDirectReportId),
            CreateLabelledTask("Upgrade", TaskStatus.Done, storyPoints: 5, labels: "maintenance"),
            CreateLabelledTask("Feature", TaskStatus.Done, storyPoints: 8));

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        var support = result.SupportDistribution;
        support.AllStoryPoints.Should().Be(5);
        support.CompletedStoryPoints.Should().Be(3);
        support.MaintenanceAllStoryPoints.Should().Be(5);
        support.MaintenanceCompletedStoryPoints.Should().Be(5);

        var john = support.ByAssignee.Single(a => a.AssigneeId == _testDirectReportId);
        john.AllStoryPoints.Should().Be(5);
        john.CompletedStoryPoints.Should().Be(3);
        support.ByAssignee.Single(a => a.AssigneeId is null).MaintenanceAllStoryPoints.Should().Be(5);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_WorkCategory_ReadsEpicsWithTheManagersNames()
    {
        // Arrange — the manager calls maintenance "KTLO"
        var settings = new AppSettings();
        settings.UpdateWorkCategoryEpicNames(new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechMaintenance] = ["KTLO"]
        });
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var epic = Parent.FromJira("MAR-1", "Epic", "KTLO Q4");
        SetupTasksAnalytics(new TeamTask("Upgrade", storyPoints: 2, parentId: epic.Id));
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([epic]);

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.TasksByWorkCategory.Single(c => c.Category == WorkCategory.TechMaintenance).TaskCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_WorkCategory_SharesTasksAndPointsByEpicName()
    {
        // Arrange — a sub-task under a story under a "Tech Debt" epic, an unestimated task
        // directly under a "Tech Maintenance" epic, and a product task with no epic
        var debtEpic = Parent.FromJira("MAR-1", "Epic", "Tech Debt");
        var maintenanceEpic = Parent.FromJira("MAR-2", "Epic", "Tech Maintenance");
        var story = new TeamTask("Story", type: TaskType.Story, storyPoints: 8, tags: "jira:MAR-3", parentId: debtEpic.Id);
        var storyParent = Parent.FromJira("MAR-3", "Story", "Story");
        SetupTasksAnalytics(
            story,
            new TeamTask("Sub-task", type: TaskType.SubTask, storyPoints: 3, parentId: storyParent.Id),
            new TeamTask("Upgrade", parentId: maintenanceEpic.Id),
            new TeamTask("Feature", storyPoints: 1));
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([debtEpic, maintenanceEpic, storyParent]);

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert — the story is a parent, so only its sub-task counts
        result.TasksByWorkCategory.Select(c => c.Category).Should().Equal(Enum.GetValues<WorkCategory>());
        var debt = result.TasksByWorkCategory.Single(c => c.Category == WorkCategory.TechDebt);
        debt.CategoryName.Should().Be("Tech Debt");
        debt.TaskCount.Should().Be(1);
        debt.StoryPoints.Should().Be(3);
        debt.PercentageOfTasks.Should().Be(33.3);
        debt.PercentageOfStoryPoints.Should().Be(75);
        var maintenance = result.TasksByWorkCategory.Single(c => c.Category == WorkCategory.TechMaintenance);
        maintenance.TaskCount.Should().Be(1);
        maintenance.PercentageOfStoryPoints.Should().Be(0);
        result.TasksByWorkCategory.Single(c => c.Category == WorkCategory.Productisation).StoryPoints.Should().Be(1);
        result.TasksByWorkCategory.Single(c => c.Category == WorkCategory.TechRoadmap).TaskCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_ComponentShare_IsAShareOfStoryPoints()
    {
        // Arrange
        SetupTasksAnalytics(
            CreateLabelledTask("Api work", TaskStatus.Done, storyPoints: 6, components: "API"),
            CreateLabelledTask("Web work", TaskStatus.Todo, storyPoints: 2, components: "Web"));

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.TasksByComponent.Select(c => c.Component).Should().Equal("API", "Web");
        result.TasksByComponent[0].PercentageOfTotalStoryPoints.Should().Be(75);
        result.TasksByComponent[1].PercentageOfTotalStoryPoints.Should().Be(25);
        result.TasksByComponent[0].CompletedStoryPoints.Should().Be(6);
    }

    #endregion

    #region GetDirectReportAnalyticsAsync Tests

    [Fact]
    public async Task GetDirectReportAnalyticsAsync_WhenDirectReportNotFound_ReturnsNull()
    {
        // Arrange
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var result = await _service.GetDirectReportAnalyticsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDirectReportAnalyticsAsync_WhenFound_ReturnsAnalytics()
    {
        // Arrange
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetDirectReportAnalyticsAsync(_testDirectReportId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(_testDirectReportId);
        result.FullName.Should().Be("John Doe");
        result.Tasks.Should().NotBeNull();
    }

    #endregion

    #region GetTeamVelocityAsync Tests

    [Fact]
    public async Task GetTeamVelocityAsync_WithNoCompletedTasks_ReturnsEmptyVelocity()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTeamVelocityAsync();

        // Assert
        result.Sprints.Should().BeEmpty();
        result.AverageVelocity.Should().Be(0);
        result.TotalStoryPointsCompleted.Should().Be(0);
    }

    [Fact]
    public async Task GetTeamVelocityAsync_CarriedOverTask_CountsItsStoryPoints()
    {
        // Arrange
        var task = new TeamTask("T1", sprint: "LP_1Q25_S1", storyPoints: 8);
        task.OverridePreviousSprintsStoryPoints(3);
        task.Complete();
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task });
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { new("LP_1Q25_S1") });
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTeamVelocityAsync();

        // Assert
        result.Sprints.Should().ContainSingle().Which.StoryPointsCompleted.Should().Be(8);
        result.TotalStoryPointsCompleted.Should().Be(8);
    }

    #endregion

    #region GetCapacityAnalysisAsync Tests

    [Fact]
    public async Task GetCapacityAnalysisAsync_WithNoData_ReturnsEmptyAnalysis()
    {
        // Arrange
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _sprintCapacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.Should().NotBeNull();
        result.NextSprint.Should().BeNull();
    }

    /// <summary>
    /// Two direct reports; a past sprint that delivered 10 SP; a current sprint running from
    /// three days ago for two weeks; and optionally the sprint after it.
    /// </summary>
    private (Sprint Past, Sprint Current, DirectReport Other) SetupPredictionHistory(Sprint? next = null, List<Leave>? leaves = null)
    {
        var today = DateTime.UtcNow.Date;
        var past = new Sprint("LP_1Q26_S1");
        past.UpdateDates(today.AddDays(-17), today.AddDays(-4));
        var current = new Sprint("LP_1Q26_S2");
        current.UpdateDates(today.AddDays(-3), today.AddDays(10));
        var delivered = new TeamTask("Delivered", storyPoints: 10, sprint: past.Name);
        delivered.Complete();
        var other = CreateDirectReport("Jane", "Roe", "jane@test.com");

        var sprints = new List<Sprint> { past, current };
        if (next != null) sprints.Add(next);
        SetupCapacityMocks(sprints, [delivered], [_testDirectReport, other], leaves ?? []);
        return (past, current, other);
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_NextSprint_IsProjectedWhenHiveDoesNotHaveItYet()
    {
        // Arrange
        SetupPredictionHistory();
        var today = DateTime.UtcNow.Date;

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert — it follows on from the current sprint and lasts as long
        result.FutureSprints.Should().BeEmpty();
        var next = result.NextSprint!;
        next.IsProjected.Should().BeTrue();
        next.SprintName.Should().BeNull();
        next.StartDate.Should().Be(DateOnly.FromDateTime(today.AddDays(11)));
        next.EndDate.Should().Be(DateOnly.FromDateTime(today.AddDays(24)));
        next.PredictedPoints.Should().Be(10);
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_NextSprint_ScalesByLeaveBookedInIt()
    {
        // Arrange — half the team is away for the whole projected sprint
        var today = DateTime.UtcNow.Date;
        var (_, _, jane) = SetupPredictionHistory();
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Leave(jane.Id, LeaveType.Vacation, today.AddDays(11), today.AddDays(24))]);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.NextSprint!.AvailableMembers.Should().Be(1);
        result.NextSprint.TeamSize.Should().Be(2);
        result.NextSprint.PredictedPoints.Should().Be(5);
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_NextSprint_UsesTheSprintWhenHiveHasIt()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var planned = new Sprint("LP_1Q26_S3");
        planned.UpdateDates(today.AddDays(12), today.AddDays(25));
        SetupPredictionHistory(planned);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        var next = result.NextSprint!;
        next.IsProjected.Should().BeFalse();
        next.SprintName.Should().Be("LP_1Q26_S3");
        next.StartDate.Should().Be(DateOnly.FromDateTime(today.AddDays(12)));
        next.PredictedPoints.Should().Be(result.FutureSprints.Single().PredictedPoints);
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_NextSprint_IsPredictedWhateverTheSprintFilter()
    {
        // Arrange — the filter shows current and past sprints only
        var today = DateTime.UtcNow.Date;
        var planned = new Sprint("LP_1Q26_S3");
        planned.UpdateDates(today.AddDays(11), today.AddDays(24));
        SetupPredictionHistory(planned);

        // Act
        var result = await _service.GetCapacityAnalysisAsync(sprintCount: 3);

        // Assert
        result.FutureSprints.Should().BeEmpty();
        result.NextSprint!.SprintName.Should().Be("LP_1Q26_S3");
        result.NextSprint.PredictedPoints.Should().Be(10);
    }

    #endregion

    #region GetCapacityAnalysisAsync — TotalStoryPoints Tests

    [Fact]
    public async Task GetCapacityAnalysisAsync_TotalStoryPoints_SumsAllSPFromTasksInSprint()
    {
        // Create a current sprint with tasks.
        // TotalStoryPoints should be the sum of all SP from tasks in the sprint (including carried over).
        var currentSprint = new Sprint("LP_1Q25_S1");
        currentSprint.UpdateDates(
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date.AddDays(7));

        var parentId = Guid.NewGuid();
        var parent = new Parent("Epic A");
        typeof(Parent).GetProperty("Id")!.SetValue(parent, parentId);

        // Two tasks in this sprint
        var task1 = new TeamTask("T1", sprint: currentSprint.Name, storyPoints: 5, parentId: parentId);
        task1.Start();
        task1.Complete();
        var task2 = new TeamTask("T2", sprint: currentSprint.Name, storyPoints: 8, parentId: parentId);

        var directReports = CreateDirectReports(3);

        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { currentSprint });
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SprintCapacity>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2 });
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.CurrentSprint.Should().NotBeNull();
        result.CurrentSprint!.TotalStoryPoints.Should().Be(13,
            "sum of all SP from tasks in this sprint (5 + 8)");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_TotalStoryPoints_IncludesCarriedOverPoints()
    {
        // Task with carried over points should include all SP in TotalStoryPoints,
        // and separately track CarriedOverPoints.
        var currentSprint = new Sprint("LP_1Q25_S1");
        currentSprint.UpdateDates(
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date.AddDays(7));

        // Task with 8 total SP, but 3 were from previous sprints
        var task = new TeamTask("T1", sprint: currentSprint.Name, storyPoints: 8);
        task.OverridePreviousSprintsStoryPoints(3);

        var directReports = CreateDirectReports(3);

        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { currentSprint });
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SprintCapacity>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task });
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.CurrentSprint.Should().NotBeNull();
        result.CurrentSprint!.TotalStoryPoints.Should().Be(8,
            "all SP should be counted including carried over");
        result.CurrentSprint!.CarriedOverPoints.Should().Be(3,
            "carried over points should be tracked separately");
    }

    #endregion

    #region GetDashboardInsightsAsync — UnmatchedTaskCount Tests

    [Fact]
    public async Task GetDashboardOverviewAsync_TasksByTypeSP_IncludesUnestimatedTypes()
    {
        // Arrange — bugs, support and spikes are often not estimated
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [],
            tasks:
            [
                new TeamTask("Story", type: TaskType.Story, storyPoints: 5),
                new TeamTask("Bug", type: TaskType.Bug),
                new TeamTask("Support", type: TaskType.Support),
                new TeamTask("Spike", type: TaskType.Spike, storyPoints: 0)
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Tasks.TasksByTypeSP.Should().BeEquivalentTo(new[]
        {
            new { Type = TaskType.Story, TotalStoryPoints = 5, TaskCount = 1 },
            new { Type = TaskType.Bug, TotalStoryPoints = 0, TaskCount = 1 },
            new { Type = TaskType.Support, TotalStoryPoints = 0, TaskCount = 1 },
            new { Type = TaskType.Spike, TotalStoryPoints = 0, TaskCount = 1 }
        });
    }


    [Fact]
    public async Task GetDashboardOverviewAsync_UnmatchedTaskCount_CountsTasksNamingNoProduct()
    {
        // Arrange — one task names a product, one names an unknown component, one names none
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                CreateTaskWithComponents("Matched", "Matching"),
                CreateTaskWithComponents("Unknown", "Billing"),
                CreateTaskWithComponents("None", "")
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.UnmatchedTaskCount.Should().Be(2,
            "one task names a component that is not a product and one names no component");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_UnmatchedTaskCount_ZeroWhenAllTasksNameAProduct()
    {
        // Arrange — component names match ignoring case
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching"), new Product("Rights Manager")],
            tasks:
            [
                CreateTaskWithComponents("T1", "matching"),
                CreateTaskWithComponents("T2", "Billing, Rights Manager")
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.UnmatchedTaskCount.Should().Be(0);
    }

    #endregion

    #region GetProductEngagementAsync

    [Fact]
    public async Task GetProductEngagementAsync_MembersByProduct_CountsEveryAssigneeOfTheProductsTasks()
    {
        // Arrange — two people on Matching, one on Enrichment.
        var second = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");

        SetupInsightsMocks(
            directReports: [_testDirectReport, second],
            products: [new Product("Matching"), new Product("Enrichment")],
            tasks:
            [
                CreateAssignedTask("A1", "Matching", _testDirectReportId),
                CreateAssignedTask("A2", "Matching", second.Id),
                CreateAssignedTask("B1", "Enrichment", _testDirectReportId)
            ]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        var byProduct = result.MembersByProduct;
        byProduct.Should().HaveCount(2);

        var matching = byProduct.Single(p => p.ProductName == "Matching");
        matching.MemberCount.Should().Be(2);
        matching.MemberNames.Should().BeEquivalentTo(["John Doe", "Jane Roe"]);
        matching.IsSilo.Should().BeFalse("two members meets the default MinProductMembers of 2");

        var enrichment = byProduct.Single(p => p.ProductName == "Enrichment");
        enrichment.MemberCount.Should().Be(1);
        enrichment.IsSilo.Should().BeTrue("one member is below the default MinProductMembers of 2");
    }

    [Fact]
    public async Task GetProductEngagementAsync_MembersByProduct_TaskWithSeveralComponentsCountsForEach()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching"), new Product("IPI")],
            tasks: [CreateAssignedTask("Shared", "Matching,IPI", _testDirectReportId)]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.MembersByProduct.Select(p => p.ProductName)
            .Should().BeEquivalentTo(["Matching", "IPI"]);
    }

    [Fact]
    public async Task GetProductEngagementAsync_MembersByProduct_CountsNamesakesAsTwoPeople()
    {
        // Arrange — two different people who share a name
        var namesake = CreateDirectReport("John", "Doe", "john.doe2@test.com");

        SetupInsightsMocks(
            directReports: [_testDirectReport, namesake],
            products: [new Product("Matching")],
            tasks:
            [
                CreateAssignedTask("A1", "Matching", _testDirectReportId),
                CreateAssignedTask("A2", "Matching", namesake.Id)
            ]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.MembersByProduct.Single().MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task GetProductEngagementAsync_MembersByProduct_OrdersSilosFirst()
    {
        // Arrange
        var second = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");

        SetupInsightsMocks(
            directReports: [_testDirectReport, second],
            products: [new Product("Busy"), new Product("Silo")],
            tasks:
            [
                CreateAssignedTask("A1", "Busy", _testDirectReportId),
                CreateAssignedTask("A2", "Busy", second.Id),
                CreateAssignedTask("B1", "Silo", _testDirectReportId)
            ]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert — ascending member count, so the bus-factor risks read first
        result.MembersByProduct.Select(p => p.ProductName)
            .Should().ContainInOrder("Silo", "Busy");
    }

    [Fact]
    public async Task GetProductEngagementAsync_ProductsByMember_CountsDistinctProductsPerPerson()
    {
        // Arrange — John on two products via three tasks
        var matching = new Product("Matching");
        var enrichment = new Product("Enrichment");

        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [matching, enrichment],
            tasks:
            [
                CreateAssignedTask("A1", "Matching", _testDirectReportId),
                CreateAssignedTask("A2", "Matching", _testDirectReportId),
                CreateAssignedTask("B1", "Enrichment", _testDirectReportId)
            ]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        var member = result.ProductsByMember.Should().ContainSingle().Subject;
        member.MemberName.Should().Be("John Doe");
        member.DirectReportId.Should().Be(_testDirectReportId);
        member.ProductCount.Should().Be(2, "two tasks on the same product are one product");
        member.ProductIds.Should().BeEquivalentTo([matching.Id, enrichment.Id]);
    }

    [Fact]
    public async Task GetProductEngagementAsync_ExcludesUnassignedTasks()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks: [CreateTaskWithComponents("Unassigned", "Matching")]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.MembersByProduct.Should().ContainSingle().Which.MemberCount.Should().Be(0);
        result.ProductsByMember.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductEngagementAsync_SurfacesMinProductMembersThreshold()
    {
        // Arrange
        SetupBasicMocks();
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSettingsWithMinProductMembers(3));

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert — the client labels the silo threshold, so it must not hard-code 2
        result.MinProductMembers.Should().Be(3);
    }

    [Fact]
    public async Task GetProductEngagementAsync_NarrowlyEngagedMembers_FlagsWhoWorkedOnFewerThanTheMinimum()
    {
        // Arrange — John has worked on two products, Jane on three, Sam on none.
        var jane = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");
        var sam = CreateDirectReport("Sam", "Poe", "sam.poe@test.com");
        SetupInsightsMocks(
            directReports: [_testDirectReport, jane, sam],
            products: [new Product("Matching"), new Product("Billing"), new Product("Search")],
            tasks:
            [
                CreateAssignedTask("J1", "Matching, Billing", _testDirectReportId),
                CreateAssignedTask("R1", "Matching, Billing, Search", jane.Id)
            ]);
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSettingsWithMinMemberProducts(3));

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert — someone on no product is unengaged, not narrowly engaged
        result.MinMemberProducts.Should().Be(3);
        var narrow = result.NarrowlyEngagedMembers.Should().ContainSingle().Subject;
        narrow.DirectReportId.Should().Be(_testDirectReportId);
        narrow.ProductCount.Should().Be(2);
        result.ProductsByMember.Single(m => m.DirectReportId == _testDirectReportId).IsNarrow.Should().BeTrue();
        result.ProductsByMember.Single(m => m.DirectReportId == jane.Id).IsNarrow.Should().BeFalse();
        result.UnengagedMembers.Should().ContainSingle(u => u.DirectReportId == sam.Id);
    }

    [Fact]
    public async Task GetProductEngagementAsync_NarrowlyEngagedMembers_SkipsWhoIsOnLeaveToday()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks: [CreateAssignedTask("J1", "Matching", _testDirectReportId)]);
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Leave(_testDirectReportId, LeaveType.Vacation, today.AddDays(-1), today.AddDays(2))]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.NarrowlyEngagedMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductEngagementAsync_MinMemberProductsOfOne_FlagsNobody()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks: [CreateAssignedTask("J1", "Matching", _testDirectReportId)]);
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSettingsWithMinMemberProducts(1));

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.NarrowlyEngagedMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductEngagementAsync_IgnoresEpicsButCountsOtherParents()
    {
        // Arrange — the epic and the story are both parents; only the epic is ignored.
        var jane = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");
        var epic = new TeamTask("Epic", "", TaskType.Epic, TaskPriority.Medium,
            assigneeId: jane.Id, components: "Matching");
        var story = new TeamTask("Story", "", TaskType.Story, TaskPriority.Medium,
            assigneeId: _testDirectReportId, components: "Matching");

        SetupInsightsMocks([_testDirectReport, jane], [new Product("Matching")], [epic, story]);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Parent("Epic"), new Parent("Story")]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.MembersByProduct.Single().MemberNames.Should().BeEquivalentTo(["John Doe"]);
    }

    [Fact]
    public async Task GetProductEngagementAsync_CountsOnlyDirectReports()
    {
        // Arrange
        var skipLevel = new DirectReport("Sam", "Skip", "sam@test.com", "Engineer", "Engineering",
            new DateTime(2020, 1, 1), isDirect: false);
        typeof(DirectReport).GetProperty("Id")!.SetValue(skipLevel, Guid.NewGuid());

        SetupInsightsMocks(
            [_testDirectReport, skipLevel],
            [new Product("Matching")],
            [
                CreateAssignedTask("A1", "Matching", _testDirectReportId),
                CreateAssignedTask("A2", "Matching", skipLevel.Id)
            ]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.MembersByProduct.Single().MemberCount.Should().Be(1);
        result.ProductsByMember.Select(m => m.MemberName).Should().BeEquivalentTo(["John Doe"]);
    }

    [Fact]
    public async Task GetProductEngagementAsync_ProductNobodyWorkedOn_IsUnworkedNotASilo()
    {
        // Arrange
        SetupInsightsMocks(
            [_testDirectReport],
            [new Product("Matching"), new Product("Orphan")],
            [CreateAssignedTask("A1", "Matching", _testDirectReportId)]);

        // Act
        var result = await _service.GetProductEngagementAsync();

        // Assert
        result.UnworkedProducts.Select(p => p.ProductName).Should().BeEquivalentTo(["Orphan"]);
        result.KnowledgeSilos.Select(s => s.ProductName).Should().BeEquivalentTo(["Matching"]);
        result.MembersByProduct.Select(p => p.ProductName).Should().ContainInOrder("Orphan", "Matching");
    }

    [Fact]
    public async Task GetProductEngagementAsync_WithMonths_CountsOnlyTasksInSprintsOverlappingThePeriod()
    {
        // Arrange — one sprint running now, one that ended five months ago, one yet to start.
        var today = DateTime.UtcNow.Date;
        var current = new Sprint("LP_1Q26_S1");
        current.UpdateDates(today.AddDays(-5), today.AddDays(5));
        var old = new Sprint("LP_1Q25_S1");
        old.UpdateDates(today.AddMonths(-5).AddDays(-14), today.AddMonths(-5));
        var future = new Sprint("LP_1Q27_S1");
        future.UpdateDates(today.AddDays(20), today.AddDays(33));

        var jane = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");
        var sam = CreateDirectReport("Sam", "Poe", "sam.poe@test.com");
        var ann = CreateDirectReport("Ann", "Moe", "ann.moe@test.com");

        SetupInsightsMocks(
            [_testDirectReport, jane, sam, ann],
            [new Product("Matching")],
            [
                CreateAssignedTaskInSprint("Now", _testDirectReportId, "LP_1Q26_S1"),
                CreateAssignedTaskInSprint("Old", jane.Id, "LP_1Q25_S1"),
                CreateAssignedTaskInSprint("Planned", sam.Id, "LP_1Q27_S1"),
                CreateAssignedTaskInSprint("Unknown sprint", ann.Id, "Someone_Else")
            ],
            sprints: [current, old, future]);

        // Act
        var threeMonths = await _service.GetProductEngagementAsync(3);
        var sixMonths = await _service.GetProductEngagementAsync(6);
        var allTime = await _service.GetProductEngagementAsync();

        // Assert
        threeMonths.Months.Should().Be(3);
        threeMonths.MembersByProduct.Single().MemberNames.Should().BeEquivalentTo(["John Doe"]);
        sixMonths.MembersByProduct.Single().MemberNames.Should().BeEquivalentTo(["John Doe", "Jane Roe"]);
        allTime.Months.Should().BeNull();
        allTime.MembersByProduct.Single().MemberCount.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task GetProductEngagementAsync_NonPositiveMonths_Throws(int months)
    {
        // Arrange
        SetupBasicMocks();

        // Act
        var act = () => _service.GetProductEngagementAsync(months);

        // Assert
        await act.Should().ThrowAsync<Hive.Core.Exceptions.DomainException>();
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsProductsNobodyWorkedOn()
    {
        // Arrange
        SetupInsightsMocks(
            [_testDirectReport],
            [new Product("Matching"), new Product("Orphan")],
            [CreateAssignedTask("A1", "Matching", _testDirectReportId)]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "UnworkedProducts").Subject;
        warning.Title.Should().Be("1 product nobody has worked on");
        warning.Detail.Should().Be("Orphan");
        result.Insights.UnworkedProducts.Select(p => p.ProductName).Should().BeEquivalentTo(["Orphan"]);
        result.Insights.KnowledgeSilos.Select(s => s.ProductName).Should().BeEquivalentTo(["Matching"]);
    }

    #endregion

    #region GetDashboardInsightsAsync — Warnings

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_EmptyWhenThereIsNothingToWarnAbout()
    {
        // Arrange — an empty database. A direct report with no tasks is itself a warning
        // (unengaged), so "nothing is wrong" means no team and no work.
        SetupInsightsMocks(directReports: [], products: [], tasks: []);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_OneWarningPerOverloadedMember()
    {
        // Arrange — three in progress against the default threshold of two.
        var tasks = new List<TeamTask>
        {
            CreateAssignedTask("T1", "Matching", _testDirectReportId),
            CreateAssignedTask("T2", "Matching", _testDirectReportId),
            CreateAssignedTask("T3", "Matching", _testDirectReportId)
        };
        foreach (var task in tasks) task.Start();

        SetupInsightsMocks([_testDirectReport], [new Product("Matching")], tasks);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings.Should().ContainSingle(w => w.Category == "Workload").Subject;
        warning.Title.Should().Be("John Doe is overloaded");
        warning.Detail.Should().Be("3 in progress");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsQuietCareerPlans()
    {
        // Arrange — John's plan is 90 days old and untouched; Jane rated a skill today.
        var jane = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");
        var quiet = new CareerPlan(_testDirectReportId, "Backend Engineer", CareerStage.Mid);
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(quiet, DateTime.UtcNow.AddDays(-90));
        var moving = new CareerPlan(jane.Id, "Backend Engineer", CareerStage.Mid);
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(moving, DateTime.UtcNow.AddDays(-90));
        var rated = new CareerPlanSkill(moving.Id, "Databases", "SQL", SkillCategory.Hard, 0);
        rated.SetLevel(1);
        _careerPlanRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([quiet, moving]);
        _careerPlanSkillRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([rated]);

        SetupInsightsMocks([_testDirectReport, jane], [new Product("Matching")],
            [CreateAssignedTask("A1", "Matching", _testDirectReportId), CreateAssignedTask("A2", "Matching", jane.Id)]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings.Should().ContainSingle(w => w.Category == "CareerPlans").Subject;
        warning.Title.Should().Be("1 career plan has gone quiet");
        warning.Detail.Should().Be($"John Doe: nothing changed in {CareerPlan.QuietDays} days");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsUnengagedMembersAndKnowledgeSilos()
    {
        // Arrange — Jane works on no product; Matching has a single member.
        var jane = CreateDirectReport("Jane", "Roe", "jane.roe@test.com");

        SetupInsightsMocks(
            directReports: [_testDirectReport, jane],
            products: [new Product("Matching")],
            tasks: [CreateAssignedTask("A1", "Matching", _testDirectReportId)]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var unengaged = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "UnengagedMembers").Subject;
        unengaged.Title.Should().Be("1 team member is not engaged in any product");
        unengaged.Detail.Should().Be("Jane Roe");

        var silo = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "KnowledgeSilos").Subject;
        silo.Title.Should().Be("1 product is a knowledge silo");
        silo.Detail.Should().Be("Matching");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsNarrowlyEngagedMembers()
    {
        // Arrange — John has worked on one product, fewer than the default minimum of two
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching"), new Product("Billing")],
            tasks: [CreateAssignedTask("A1", "Matching", _testDirectReportId)]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "NarrowlyEngagedMembers").Subject;
        warning.Title.Should().Be("1 team member has worked on fewer than 2 products");
        warning.Detail.Should().Be(_testDirectReport.FullName);
        result.Insights.NarrowlyEngagedMembers.Should().ContainSingle(m => m.DirectReportId == _testDirectReportId);
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsUnmatchedTasks()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                CreateAssignedTask("Matched", "Matching", _testDirectReportId),
                CreateAssignedTask("Orphan", "Nowhere", _testDirectReportId)
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "UnmatchedTasks")
            .Which.Title.Should().Be("1 task is not attributed to a product");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsSpikesWithStoryPoints()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                new TeamTask("Estimated spike", type: TaskType.Spike, storyPoints: 3, components: "Matching"),
                new TeamTask("Plain spike", type: TaskType.Spike, components: "Matching"),
                new TeamTask("Zero spike", type: TaskType.Spike, storyPoints: 0, components: "Matching"),
                new TeamTask("Story", type: TaskType.Story, storyPoints: 5, components: "Matching")
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "EstimatedSpikes").Subject;
        warning.Title.Should().Be("1 spike has story points");
        warning.Detail.Should().Be("Estimated spike (3 SP)");
        result.Insights.Warnings.Should().NotContain(w => w.Category == "EstimatedSupport");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsSupportWithStoryPoints()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                new TeamTask("Small ticket", type: TaskType.Support, storyPoints: 1, components: "Matching"),
                new TeamTask("Big ticket", type: TaskType.Support, storyPoints: 8, components: "Matching"),
                new TeamTask("Unestimated ticket", type: TaskType.Support, components: "Matching")
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "EstimatedSupport").Subject;
        warning.Title.Should().Be("2 support tickets have story points");
        warning.Detail.Should().Be("Big ticket (8 SP), Small ticket (1 SP)");
        result.Insights.Warnings.Should().NotContain(w => w.Category == "EstimatedSpikes");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsTasksWithoutStoryPoints()
    {
        // Arrange
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                new TeamTask("Unestimated bug", assigneeId: _testDirectReportId, type: TaskType.Bug, components: "Matching"),
                new TeamTask("Zero story", assigneeId: _testDirectReportId, type: TaskType.Story, storyPoints: 0, components: "Matching"),
                new TeamTask("Estimated task", assigneeId: _testDirectReportId, type: TaskType.Task, storyPoints: 3, components: "Matching"),
                new TeamTask("Spike", assigneeId: _testDirectReportId, type: TaskType.Spike, components: "Matching"),
                new TeamTask("Support", assigneeId: _testDirectReportId, type: TaskType.Support, components: "Matching"),
                new TeamTask("Epic", assigneeId: _testDirectReportId, type: TaskType.Epic, components: "Matching"),
                new TeamTask("Unassigned bug", type: TaskType.Bug, components: "Matching"),
                new TeamTask("Outsider's bug", assigneeId: Guid.NewGuid(), type: TaskType.Bug, components: "Matching")
            ]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "UnestimatedTasks").Subject;
        warning.Title.Should().Be("2 tasks have no story points");
        warning.Detail.Should().Be("Unestimated bug (Bug), Zero story (Story)");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_NamesUpcomingStoriesAndTasksWithoutAcceptanceCriteria()
    {
        // Arrange
        var current = new Sprint("MAR_3Q26_S6");
        current.UpdateDates(DateTime.UtcNow.Date.AddDays(-3), DateTime.UtcNow.Date.AddDays(10));
        var started = new TeamTask("Started story", type: TaskType.Story, sprint: current.Name);
        started.Start();
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [],
            tasks:
            [
                new TeamTask("Vague story", "As a user I want it", TaskType.Story, sprint: current.Name),
                new TeamTask("Vague task", type: TaskType.Task, sprint: current.Name),
                new TeamTask("Clear story", "## Acceptance criteria\n- Done", TaskType.Story, sprint: current.Name),
                new TeamTask("A bug", type: TaskType.Bug, sprint: current.Name),
                new TeamTask("Unplanned story", type: TaskType.Story),
                started
            ],
            sprints: [current]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "MissingAcceptanceCriteria").Subject;
        warning.Title.Should().Be("2 upcoming tasks have no acceptance criteria");
        warning.Detail.Should().Contain("Vague story").And.Contain("Vague task");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_LeavesOutParentsWhoseChildrenAllHaveStoryPoints()
    {
        // Arrange — one story sized by its sub-tasks, linked by Jira key; one story whose
        // sub-task has no points, linked by its task
        var sizedStory = new TeamTask("Sized story", assigneeId: _testDirectReportId, type: TaskType.Story, tags: "jira:MAR-1", components: "Matching");
        var sizedParent = Parent.FromJira("MAR-1", "Story", "Sized story");
        var openStory = new TeamTask("Open story", assigneeId: _testDirectReportId, type: TaskType.Story, components: "Matching");
        var openParent = new Parent("Open story", teamTaskId: openStory.Id);
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                sizedStory,
                new TeamTask("Sized sub-task", assigneeId: _testDirectReportId, type: TaskType.SubTask, storyPoints: 2, components: "Matching", parentId: sizedParent.Id),
                new TeamTask("Other sized sub-task", assigneeId: _testDirectReportId, type: TaskType.SubTask, storyPoints: 1, components: "Matching", parentId: sizedParent.Id),
                openStory,
                new TeamTask("Open sub-task", assigneeId: _testDirectReportId, type: TaskType.SubTask, storyPoints: 3, components: "Matching", parentId: openParent.Id),
                new TeamTask("Unsized sub-task", assigneeId: _testDirectReportId, type: TaskType.SubTask, components: "Matching", parentId: openParent.Id)
            ]);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sizedParent, openParent]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "UnestimatedTasks").Which
            .Detail.Should().Be("Open story (Story), Unsized sub-task (SubTask)");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_LeavesOutWorkUnderSpikesAndSupport()
    {
        // Arrange — a support ticket with a sub-task, and a story sized by its task while its
        // spike sub-task has no points
        var support = new TeamTask("Incident", type: TaskType.Support, tags: "jira:MAR-1", components: "Matching");
        var supportParent = Parent.FromJira("MAR-1", "Support", "Incident");
        var story = new TeamTask("Story", type: TaskType.Story, tags: "jira:MAR-2", components: "Matching");
        var storyParent = Parent.FromJira("MAR-2", "Story", "Story");
        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks:
            [
                support,
                new TeamTask("Investigate", type: TaskType.SubTask, components: "Matching", parentId: supportParent.Id),
                story,
                new TeamTask("Build", type: TaskType.SubTask, storyPoints: 3, components: "Matching", parentId: storyParent.Id),
                new TeamTask("Research", type: TaskType.Spike, components: "Matching", parentId: storyParent.Id)
            ]);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([supportParent, storyParent]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings.Should().NotContain(w => w.Category == "UnestimatedTasks");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_NoLowUtilizationWarningWithoutSprints()
    {
        // Arrange — an empty database reports 0% utilization, which is no data, not a warning.
        SetupBasicMocks();

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings.Should().NotContain(w => w.Category == "LowUtilization");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_IncludeCapacityDerivedWarnings()
    {
        // Arrange — a sprint containing today, committed 10 SP, carrying 30 SP of work.
        // The client only fetched capacity when its widget was visible; the warning must
        // be present regardless.
        var sprint = CreateSprintContainingToday();
        var capacity = new SprintCapacity(sprint.Id, 10, 1);
        var task = new TeamTask(
            "Big",
            assigneeId: _testDirectReportId,
            storyPoints: 30,
            components: "Matching",
            sprint: sprint.Name);

        SetupInsightsMocks(
            directReports: [_testDirectReport],
            products: [new Product("Matching")],
            tasks: [task],
            sprints: [sprint],
            capacities: [capacity]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var creep = result.Insights.Warnings
            .Should().ContainSingle(w => w.Category == "ScopeCreep").Subject;
        creep.Title.Should().Be($"20 SP of scope creep in {sprint.Name}");
    }

    #endregion

    #region GetDashboardInsightsAsync — People Warnings

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsOverdue1on1s()
    {
        // Arrange — the last 1:1 was 20 days ago against the default 14-day cadence.
        SetupInsightsMocks([_testDirectReport], [], []);
        SetupMeetings(new OneOnOneMeeting(DaysAgo(20), directReportId: _testDirectReportId));

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings.Should().ContainSingle(w => w.Category == "OneOnOnes").Subject;
        warning.Title.Should().Be("1 overdue 1:1");
        warning.Detail.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_NoOverdue1on1ForSomeoneOnLeaveToday()
    {
        // Arrange
        SetupInsightsMocks([_testDirectReport], [], []);
        SetupMeetings(new OneOnOneMeeting(DaysAgo(40), directReportId: _testDirectReportId));
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Leave(_testDirectReportId, LeaveType.Vacation, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(3))]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings.Should().NotContain(w => w.Category == "OneOnOnes");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_Warnings_ReportsOverdueTasksOldestFirst()
    {
        // Arrange — two open tasks past due, one done task past due, one not due yet
        var today = DateTime.UtcNow.Date;
        var late = new TeamTask("Late", dueDate: today.AddDays(-3), components: "Matching");
        var later = new TeamTask("Very late", dueDate: today.AddDays(-10), components: "Matching");
        var done = new TeamTask("Done late", dueDate: today.AddDays(-5), components: "Matching");
        done.Complete();
        var upcoming = new TeamTask("Upcoming", dueDate: today.AddDays(4), components: "Matching");
        SetupInsightsMocks([], [new Product("Matching")], [late, later, done, upcoming]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings.Should().ContainSingle(w => w.Category == "OverdueTasks").Subject;
        warning.Title.Should().Be("2 overdue tasks");
        warning.Detail.Should().Be(
            $"Very late (due {today.AddDays(-10).ToDisplayDate()}), Late (due {today.AddDays(-3).ToDisplayDate()})");
    }

    #endregion

    #region GetDashboardFocusAsync Tests

    [Fact]
    public async Task GetDashboardFocusAsync_OneOnOnes_EvaluatesEachDirectReportAgainstTheConfiguredCadence()
    {
        // Arrange — a 7-day cadence: John met 3 days ago, Jane 6 days ago, Max 10 days ago,
        // Ann never. Only direct reports are listed.
        var jane = CreateDirectReport("Jane", "Roe", "jane@test.com");
        var max = CreateDirectReport("Max", "Poe", "max@test.com");
        var ann = CreateDirectReport("Ann", "Lee", "ann@test.com");
        var skip = CreateDirectReport("Sam", "Skip", "sam@test.com");
        skip.Update(skip.FirstName, skip.LastName, skip.Email, skip.JobTitle, skip.Department, skip.HireDate, isDirect: false);

        SetupBasicMocks();
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_testDirectReport, jane, max, ann, skip]);
        var settings = new AppSettings();
        settings.UpdateOneOnOneCadence(7);
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        SetupMeetings(
            new OneOnOneMeeting(DaysAgo(3), directReportId: _testDirectReportId),
            new OneOnOneMeeting(DaysAgo(30), directReportId: _testDirectReportId),
            new OneOnOneMeeting(DaysAgo(6), directReportId: jane.Id),
            new OneOnOneMeeting(DaysAgo(10), directReportId: max.Id),
            // A 1:1 dated in the future has not happened yet
            new OneOnOneMeeting(DaysAgo(-2), directReportId: max.Id));

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert — most urgent first
        result.OneOnOnes.CadenceDays.Should().Be(7);
        result.OneOnOnes.People.Select(p => (p.FullName, p.Status)).Should().Equal(
            ("Ann Lee", "NeverMet"),
            ("Max Poe", "Overdue"),
            ("Jane Roe", "DueSoon"),
            ("John Doe", "OnTrack"));
        result.OneOnOnes.People.Single(p => p.FullName == "Max Poe").DaysUntilDue.Should().Be(-3);
        result.OneOnOnes.People.Single(p => p.FullName == "John Doe").LastMeetingDate.Should().Be(DaysAgo(3));
        result.OneOnOnes.NeedsAttentionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardFocusAsync_Todos_ListsHotItemsOfTheJournalNoteWithTheGreatestTitle()
    {
        // Arrange
        SetupBasicMocks();
        var journal = new NoteFolder("Journal");
        var older = new ManagerNote("2026-09-27", "# Journal\n\n## hot 🔥\n- Yesterday's fire", folderId: journal.Id);
        var latest = new ManagerNote("2026-09-28", "# Journal\n\n## hot 🔥\n- Write a downtime document\n- Check the review template\n\n## cold 🧊\n- Write a proposal", folderId: journal.Id);
        var early = new ManagerNote("2026-09-01", "## hot\n- Old news", folderId: journal.Id);
        SetupJournal(journal, older, latest, early);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.Todos.HasJournal.Should().BeTrue();
        result.Todos.NoteId.Should().Be(latest.Id);
        result.Todos.NoteTitle.Should().Be("2026-09-28");
        result.Todos.Items.Should().Equal("Write a downtime document", "Check the review template");
    }

    [Fact]
    public async Task GetDashboardFocusAsync_Todos_ReadsOnlyTheJournalFolder()
    {
        // Arrange
        SetupBasicMocks();
        var drafts = new NoteFolder("Drafts");
        _noteFolderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteFolder> { drafts });

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.Todos.HasJournal.Should().BeFalse();
        result.Todos.NoteId.Should().BeNull();
        result.Todos.Items.Should().BeEmpty();
        _noteRepositoryMock.Verify(r => r.GetByFolderAsync(drafts.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardFocusAsync_Todos_EmptyJournal_HasNoNote()
    {
        // Arrange
        SetupBasicMocks();
        SetupJournal(new NoteFolder("journal"));

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.Todos.HasJournal.Should().BeTrue();
        result.Todos.NoteId.Should().BeNull();
        result.Todos.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardFocusAsync_Leave_SplitsTodayFromTheNextTwoWeeks()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        SetupBasicMocks();
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Leave(_testDirectReportId, LeaveType.Sick, today, today.AddDays(1)),
                new Leave(_testDirectReportId, LeaveType.Vacation, today.AddDays(5), today.AddDays(9)),
                new Leave(_testDirectReportId, LeaveType.Vacation, today.AddDays(40), today.AddDays(41)),
                new Leave(_testDirectReportId, LeaveType.Vacation, today.AddDays(-10), today.AddDays(-8))
            ]);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.Leave.OutToday.Should().ContainSingle().Which.Type.Should().Be("Sick");
        var upcoming = result.Leave.Upcoming.Should().ContainSingle().Subject;
        upcoming.FullName.Should().Be("John Doe");
        upcoming.StartDate.Should().Be(DateOnly.FromDateTime(today.AddDays(5)));
    }

    [Fact]
    public async Task GetDashboardFocusAsync_CurrentSprint_NextSprintHasNotStarted()
    {
        // Arrange — no sprint contains today; the next one starts in a week.
        var today = DateTime.UtcNow.Date;
        var quarter = (today.Month - 1) / 3 + 1;
        var next = new Sprint($"LP_{quarter}Q{today:yy}_S2");
        next.UpdateDates(today.AddDays(7), today.AddDays(20));
        SetupBasicMocks();
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([next]);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        var current = result.CurrentSprint!;
        current.IsActive.Should().BeFalse();
        current.Pace.Should().Be("NotStarted");
        // Blocked is listed even when nothing is blocked
        current.ByStatus.Should().ContainSingle(c => c.StatusName == "Blocked").Which.TaskCount.Should().Be(0);
        current.ElapsedPercent.Should().Be(0);
        current.WorkingDaysLeft.Should().Be(current.WorkingDays);
    }

    [Fact]
    public async Task GetDashboardFocusAsync_CurrentSprint_BehindWhenLittleIsDoneLateInTheSprint()
    {
        // Arrange — a sprint ending today with nothing done.
        var today = DateTime.UtcNow.Date;
        var quarter = (today.Month - 1) / 3 + 1;
        var sprint = new Sprint($"LP_{quarter}Q{today:yy}_S3");
        sprint.UpdateDates(today.AddDays(-13), today);
        SetupBasicMocks();
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([sprint]);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TeamTask("Open", storyPoints: 8, sprint: sprint.Name)]);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.CurrentSprint!.Pace.Should().Be("Behind");
    }

    [Fact]
    public async Task GetDashboardFocusAsync_CurrentSprint_NullWithoutSprints()
    {
        // Arrange
        SetupBasicMocks();

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        result.CurrentSprint.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardFocusAsync_CurrentSprint_SummarisesProgressScopeAndBlockedWork()
    {
        // Arrange — committed 10 SP; 13 SP of new work (one task carries 2 SP from before),
        // 5 of them done, and one blocked task that came from Jira.
        var sprint = CreateSprintContainingToday();
        var done = new TeamTask("Done work", storyPoints: 5, sprint: sprint.Name);
        done.Complete();
        var blocked = new TeamTask("Stuck", assigneeId: _testDirectReportId, storyPoints: 3,
            tags: "jira:MAR-7,imported-from-jira", sprint: sprint.Name);
        blocked.Block();
        var carried = new TeamTask("Carried", storyPoints: 7, sprint: sprint.Name);
        carried.OverridePreviousSprintsStoryPoints(2);
        var elsewhere = new TeamTask("Other sprint", storyPoints: 8, sprint: "LP_1Q20_S1");

        SetupBasicMocks();
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([sprint]);
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SprintCapacity(sprint.Id, 10, 1)]);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([done, blocked, carried, elsewhere]);
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl("https://team.atlassian.net/browse");
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert
        var current = result.CurrentSprint!;
        current.Name.Should().Be(sprint.Name);
        current.IsActive.Should().BeTrue();
        current.TotalTasks.Should().Be(3);
        current.DoneTasks.Should().Be(1);
        current.NewPoints.Should().Be(13);
        current.NewCompletedPoints.Should().Be(5);
        current.CompletedPercent.Should().Be(38);
        current.CarriedOverPoints.Should().Be(2);
        current.ScopeCreepPoints.Should().Be(5);
        current.WorkingDaysLeft.Should().BeInRange(1, current.WorkingDays);
        current.Pace.Should().BeOneOf("Ahead", "OnPace", "Behind");
        current.ByStatus.Should().Contain(s => s.StatusName == "Blocked" && s.TaskCount == 1 && s.StoryPoints == 3);

        var stuck = current.BlockedTasks.Should().ContainSingle().Subject;
        stuck.JiraKey.Should().Be("MAR-7");
        stuck.JiraUrl.Should().Be("https://team.atlassian.net/browse/MAR-7");
        stuck.AssigneeName.Should().Be("John Doe");
    }

    #endregion

    #region Stale tasks, tenure milestones and sprint trends

    [Fact]
    public async Task GetDashboardOverviewAsync_TaskUnchangedPastTheThreshold_RaisesStaleTasksWarning()
    {
        // Arrange — the default threshold is 5 days
        SetupBasicMocks();
        var stuck = new TeamTask("Stuck");
        stuck.Start();
        SetStatusChangedAt(stuck, DateTime.UtcNow.AddDays(-8));
        var moving = new TeamTask("Moving");
        moving.Start();
        var parked = new TeamTask("Parked");
        SetStatusChangedAt(parked, DateTime.UtcNow.AddDays(-30));
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([stuck, moving, parked]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        var warning = result.Insights.Warnings.Should().ContainSingle(w => w.Category == "StaleTasks").Subject;
        warning.Title.Should().Be("1 task stuck for 5+ days");
        warning.Detail.Should().Be("Stuck (InProgress, 8 days)");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_StaleThreshold_ComesFromSettings()
    {
        // Arrange
        SetupBasicMocks();
        var settings = new AppSettings();
        settings.UpdateStaleTaskDays(10);
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var task = new TeamTask("In review a week");
        task.Start();
        task.MoveToReview();
        SetStatusChangedAt(task, DateTime.UtcNow.AddDays(-7));
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.Warnings.Should().NotContain(w => w.Category == "StaleTasks");
    }

    [Fact]
    public async Task GetDashboardFocusAsync_TenureMilestones_ListsOnlyThoseWithinTheWeek()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var anniversary = CreateDirectReport("Ann", "Lee", "ann@test.com");
        anniversary.Update("Ann", "Lee", "ann@test.com", "Engineer", "Eng",
            today.AddDays(2).AddYears(-3).ToDateTime(TimeOnly.MinValue), isDirect: true);
        var newcomer = CreateDirectReport("Nia", "New", "nia@test.com");
        newcomer.Update("Nia", "New", "nia@test.com", "Engineer", "Eng",
            today.AddDays(3).AddMonths(-6).ToDateTime(TimeOnly.MinValue), isDirect: true);
        var later = CreateDirectReport("Lou", "Later", "lou@test.com");
        later.Update("Lou", "Later", "lou@test.com", "Engineer", "Eng",
            today.AddDays(20).AddYears(-2).ToDateTime(TimeOnly.MinValue), isDirect: true);

        SetupBasicMocks();
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([anniversary, newcomer, later]);

        // Act
        var result = await _service.GetDashboardFocusAsync();

        // Assert — soonest first; Lou's anniversary is too far off to show
        result.TenureMilestones.Select(m => (m.FullName, m.Kind, m.Years)).Should().Equal(
            ("Ann Lee", "Anniversary", 3),
            ("Nia New", "SixMonths", 0));
        result.TenureMilestones[0].DaysUntil.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardFocusAsync_NoMilestoneThisWeek_ReturnsNone()
    {
        SetupBasicMocks();

        var result = await _service.GetDashboardFocusAsync();

        result.TenureMilestones.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCarryOverTrendAsync_CountsWorkCarriedInAndOutOfEachSprint()
    {
        // Arrange
        SetupSprintHistory(
            SprintTask("A", "LP_1Q26_S1", 3),
            SprintTask("B", "LP_1Q26_S1,LP_1Q26_S2", 5),
            SprintTask("C", "LP_1Q26_S2", 2),
            SprintTask("D", "LP_1Q26_S2,LP_1Q26_S3", 8),
            SprintTask("E", "LP_1Q26_S3", 1));

        // Act
        var result = await _service.GetCarryOverTrendAsync();

        // Assert
        result.Sprints.Select(s => (s.SprintName, s.TotalPoints, s.CarriedInPoints, s.CarriedInPercent, s.CarriedOutTasks)).Should().Equal(
            ("LP_1Q26_S1", 8, 0, 0.0, 1),
            ("LP_1Q26_S2", 15, 5, 33.3, 1),
            ("LP_1Q26_S3", 9, 8, 88.9, 0));
        result.Sprints[1].NewPoints.Should().Be(10);
        result.AverageCarriedInPercent.Should().Be(40.6);
    }

    [Fact]
    public async Task GetCarryOverTrendAsync_WithASprintCount_KeepsTheLatestSprints()
    {
        SetupSprintHistory(
            SprintTask("A", "LP_1Q26_S1", 3),
            SprintTask("B", "LP_1Q26_S2", 5),
            SprintTask("C", "LP_1Q26_S3", 2));

        var result = await _service.GetCarryOverTrendAsync(sprintCount: 2);

        result.Sprints.Select(s => s.SprintName).Should().Equal("LP_1Q26_S2", "LP_1Q26_S3");
    }

    [Fact]
    public async Task GetBugSupportInflowAsync_CountsTicketsInTheFirstSprintTheyWereIn()
    {
        // Arrange — the support ticket carried into S3 arrived in S2 and is counted there only.
        // Maintenance is the "maintenance" label; a bug with that label is a bug and maintenance.
        SetupSprintHistory(
            SprintTask("Bug 1", "LP_1Q26_S1", 2, TaskType.Bug),
            SprintTask("Story", "LP_1Q26_S1", 5, TaskType.Story),
            SprintTask("Ticket", "LP_1Q26_S2,LP_1Q26_S3", 3, TaskType.Support),
            SprintTask("Upgrade", "LP_1Q26_S2", 0, TaskType.Task, labels: "maintenance"),
            SprintTask("Bug 2", "LP_1Q26_S3", 1, TaskType.Bug, labels: "maintenance"));

        // Act
        var result = await _service.GetBugSupportInflowAsync();

        // Assert
        result.Sprints.Select(s => (s.SprintName, s.NewTasks, s.BugTasks, s.SupportTasks, s.MaintenanceTasks)).Should().Equal(
            ("LP_1Q26_S1", 2, 1, 0, 0),
            ("LP_1Q26_S2", 2, 0, 1, 1),
            ("LP_1Q26_S3", 1, 1, 0, 1));
        result.Sprints[0].InflowPercent.Should().Be(50);
        result.AverageBugsPerSprint.Should().Be(0.7);
        result.AverageSupportPerSprint.Should().Be(0.3);
        result.AverageMaintenancePerSprint.Should().Be(0.7);
        // The labelled bug counts once in the share: 4 of the 5 new tasks
        result.InflowPercent.Should().Be(80);
        result.MaintenanceLabels.Should().Equal("maintenance");
    }

    [Fact]
    public async Task GetBugSupportInflowAsync_IgnoresSprintsHiveDoesNotKnow()
    {
        SetupSprintHistory(SprintTask("Bug", "OTHER_1Q26_S1", 1, TaskType.Bug));

        var result = await _service.GetBugSupportInflowAsync();

        result.Sprints.Should().BeEmpty();
        result.InflowPercent.Should().Be(0);
    }

    private void SetupSprintHistory(params TeamTask[] tasks)
    {
        SetupBasicMocks();
        var start = new DateTime(2026, 1, 5);
        var sprints = Enumerable.Range(0, 3).Select(i =>
        {
            var sprint = new Sprint($"LP_1Q26_S{i + 1}");
            sprint.UpdateDates(start.AddDays(14 * i), start.AddDays(14 * i + 13));
            return sprint;
        }).ToList();
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sprints);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
    }

    private static TeamTask SprintTask(
        string title, string sprint, int storyPoints, TaskType type = TaskType.Task, string? labels = null) =>
        new(title, type: type, storyPoints: storyPoints, sprint: sprint, labels: labels);

    private static void SetStatusChangedAt(TeamTask task, DateTime value) =>
        typeof(TeamTask).GetProperty(nameof(TeamTask.StatusChangedAt))!.SetValue(task, value);

    #endregion

    #region Helper Methods

    private static DirectReport CreateDirectReport(string firstName, string lastName, string email)
    {
        var report = new DirectReport(
            firstName,
            lastName,
            email,
            "Software Engineer",
            "Engineering",
            new DateTime(2020, 1, 1));

        typeof(DirectReport).GetProperty("Id")!.SetValue(report, Guid.NewGuid());
        return report;
    }

    private static TeamTask CreateAssignedTask(string title, string components, Guid assigneeId)
    {
        return new TeamTask(
            title,
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            assigneeId: assigneeId,
            components: components);
    }

    private static TeamTask CreateAssignedTaskInSprint(string title, Guid assigneeId, string sprint)
    {
        return new TeamTask(
            title,
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            assigneeId: assigneeId,
            components: "Matching",
            sprint: sprint);
    }

    private static Sprint CreateSprintContainingToday()
    {
        var today = DateTime.UtcNow.Date;
        var quarter = (today.Month - 1) / 3 + 1;
        var sprint = new Sprint($"LP_{quarter}Q{today:yy}_S1");
        sprint.UpdateDates(today.AddDays(-3), today.AddDays(10));
        return sprint;
    }

    private static AppSettings CreateSettingsWithMinMemberProducts(int minMemberProducts)
    {
        var settings = new AppSettings();
        settings.UpdateMinMemberProducts(minMemberProducts);
        return settings;
    }

    private static AppSettings CreateSettingsWithMinProductMembers(int minProductMembers)
    {
        var settings = new AppSettings();
        settings.UpdateDashboardThresholds(
            settings.MaxInProgressTasks,
            settings.MaxBlockedTasks,
            settings.MaxInReviewTasks,
            minProductMembers);
        return settings;
    }

    private void SetupInsightsMocks(
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Product> products,
        IReadOnlyList<TeamTask> tasks,
        IReadOnlyList<Sprint>? sprints = null,
        IReadOnlyList<SprintCapacity>? capacities = null)
    {
        SetupBasicMocks();
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports.ToList());
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks.ToList());
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((sprints ?? []).ToList());
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((capacities ?? []).ToList());
    }

    private static DateOnly DaysAgo(int days) => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);

    private void SetupMeetings(params OneOnOneMeeting[] meetings) =>
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(meetings.ToList());

    private void SetupJournal(NoteFolder journal, params ManagerNote[] notes)
    {
        _noteFolderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteFolder> { journal });
        _noteRepositoryMock.Setup(r => r.GetByFolderAsync(journal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes.ToList());
    }

    private void SetupBasicMocks()
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());
        _noteFolderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteFolder>());
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SprintCapacity>());
    }

    private TeamTask CreateTask(string title, TaskStatus status)
    {
        var task = new TeamTask(
            title,
            description: "Description",
            type: TaskType.Task,
            priority: TaskPriority.Medium,
            assigneeId: null,
            projectId: null,
            dueDate: null,
            storyPoints: null,
            tags: "",
            labels: "",
            components: "",
            sprint: null);

        if (status == TaskStatus.Done)
        {
            task.Start();
            task.Complete();
        }
        else if (status == TaskStatus.InProgress)
        {
            task.Start();
        }

        return task;
    }

    private TeamTask CreateTaskWithAssignee(string title, TaskStatus status, Guid? assigneeId)
    {
        var task = new TeamTask(
            title,
            description: "Description",
            type: TaskType.Task,
            priority: TaskPriority.Medium,
            assigneeId: assigneeId,
            projectId: null,
            dueDate: null,
            storyPoints: null,
            tags: "",
            labels: "",
            components: "",
            sprint: null);

        if (status == TaskStatus.Done)
        {
            task.Start();
            task.Complete();
        }
        else if (status == TaskStatus.InProgress)
        {
            task.Start();
        }

        return task;
    }

    private TeamTask CreateTaskWithSprintAndPoints(string title, TaskStatus status, string sprint, int storyPoints)
    {
        var task = new TeamTask(
            title,
            description: "Description",
            type: TaskType.Task,
            priority: TaskPriority.Medium,
            assigneeId: null,
            projectId: null,
            dueDate: null,
            storyPoints: storyPoints,
            tags: "",
            labels: "",
            components: sprint,
            sprint: null);

        if (status == TaskStatus.Done)
        {
            task.Start();
            task.Complete();
        }

        return task;
    }

    private static TeamTask CreateLabelledTask(
        string title, TaskStatus status, int storyPoints, string labels = "", string? components = null, Guid? assigneeId = null,
        TaskType type = TaskType.Task)
    {
        var task = new TeamTask(title, type: type, assigneeId: assigneeId, storyPoints: storyPoints, labels: labels, components: components);
        if (status == TaskStatus.InProgress || status == TaskStatus.Done)
        {
            task.Start();
        }
        if (status == TaskStatus.Done)
        {
            task.Complete();
        }
        else if (status == TaskStatus.Todo)
        {
            task.MoveToTodo();
        }

        return task;
    }

    private void SetupTasksAnalytics(params TeamTask[] tasks)
    {
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks.ToList());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Parent>());
    }

    private TeamTask CreateTaskWithComponents(string title, string components)
    {
        return new TeamTask(
            title,
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            components: components);
    }

    /// <summary>
    /// Creates N direct reports (all IsDirect=true) with deterministic IDs.
    /// </summary>
    private List<DirectReport> CreateDirectReports(int count)
    {
        var list = new List<DirectReport>();
        for (int i = 0; i < count; i++)
        {
            var dr = new DirectReport(
                $"Person{i}",
                $"Last{i}",
                $"person{i}@test.com",
                "Dev",
                "Eng",
                new DateTime(2020, 1, 1));
            list.Add(dr);
        }
        return list;
    }

    /// <summary>
    /// Creates past, current, and future Sprint entities with dates relative to DateTime.UtcNow.
    /// Past sprints end before today, current sprint spans today, future sprints start after today.
    /// Each sprint is a 14-day span.
    /// </summary>
    private (List<Sprint> past, Sprint current, List<Sprint> future) CreateSprintsForPrediction(
        int pastCount, int futureCount)
    {
        var now = DateTime.UtcNow.Date;

        var past = new List<Sprint>();
        for (int i = 1; i <= pastCount; i++)
        {
            var sprint = new Sprint($"LP_4Q25_S{i}");
            // Past sprints: 14-day spans ending well before today
            var start = now.AddDays(-14 * (pastCount - i + 2));
            sprint.UpdateDates(start, start.AddDays(13));
            past.Add(sprint);
        }

        var current = new Sprint("LP_1Q26_S1");
        current.UpdateDates(now.AddDays(-7), now.AddDays(6));

        var future = new List<Sprint>();
        for (int i = 1; i <= futureCount; i++)
        {
            var sprint = new Sprint($"LP_2Q26_S{i}");
            var start = now.AddDays(14 * i);
            sprint.UpdateDates(start, start.AddDays(13));
            future.Add(sprint);
        }

        return (past, current, future);
    }

    /// <summary>
    /// Sets up all repository mocks needed for GetCapacityAnalysisAsync.
    /// </summary>
    private void SetupCapacityMocks(
        List<Sprint> sprints,
        List<TeamTask> tasks,
        List<DirectReport> directReports,
        List<Leave> leaves,
        List<SprintCapacity>? capacities = null)
    {
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(capacities ?? new List<SprintCapacity>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
    }

    #endregion
}
