using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for ReportingService.
/// </summary>
public class ReportingServiceTests
{
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IPerformanceReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IMeetingNoteRepository> _noteRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ISprintCapacityRepository> _sprintCapacityRepositoryMock;
    private readonly Mock<IAppSettingsRepository> _appSettingsRepositoryMock;
    private readonly Mock<IParentRepository> _parentRepositoryMock;
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly ReportingService _service;

    private readonly DirectReport _testDirectReport;
    private readonly Guid _testDirectReportId = Guid.NewGuid();

    public ReportingServiceTests()
    {
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _reviewRepositoryMock = new Mock<IPerformanceReviewRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _noteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _sprintCapacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _appSettingsRepositoryMock = new Mock<IAppSettingsRepository>();
        _parentRepositoryMock = new Mock<IParentRepository>();
        _leaveRepositoryMock = new Mock<ILeaveRepository>();

        _service = new ReportingService(
            _directReportRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _sprintCapacityRepositoryMock.Object,
            _appSettingsRepositoryMock.Object,
            _parentRepositoryMock.Object,
            _leaveRepositoryMock.Object);

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
        result.Reviews.Should().NotBeNull();
        result.OneOnOnes.Should().NotBeNull();
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

    #region GetReviewsAnalyticsAsync Tests

    [Fact]
    public async Task GetReviewsAnalyticsAsync_WithNoReviews_ReturnsEmptyStats()
    {
        // Arrange
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalReviews.Should().Be(0);
        result.RatingDistribution.Should().HaveCount(4); // All rating values with 0 counts
        result.RatingDistribution.Should().OnlyContain(r => r.Count == 0);
    }

    [Fact]
    public async Task GetReviewsAnalyticsAsync_WithReviews_CalculatesCorrectStats()
    {
        // Arrange
        var reviews = new List<PerformanceReview>
        {
            CreateReview(_testDirectReportId, PerformanceRating.NotRated),
            CreateReview(_testDirectReportId, PerformanceRating.MeetsExpectations),
            CreateReview(_testDirectReportId, PerformanceRating.ExceedsExpectations)
        };

        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.TotalReviews.Should().Be(3);
        result.RatingDistribution.Should().HaveCount(4); // All rating values
        result.RatingDistribution.Should().Contain(r => r.Rating == PerformanceRating.MeetsExpectations && r.Count == 1);
        result.RatingDistribution.Should().Contain(r => r.Rating == PerformanceRating.ExceedsExpectations && r.Count == 1);
        result.RatingDistribution.Should().Contain(r => r.Rating == PerformanceRating.NeedsImprovement && r.Count == 0);
        result.RatingDistribution.Should().Contain(r => r.Rating == PerformanceRating.Outstanding && r.Count == 0);
    }

    [Fact]
    public async Task GetReviewsAnalyticsAsync_FiltersOutIndirectReports()
    {
        // Arrange
        var indirectReport = new DirectReport(
            "Jane",
            "Smith",
            "jane.smith@test.com",
            "Developer",
            "Engineering",
            new DateTime(2021, 1, 1));

        var indirectReportId = Guid.NewGuid();
        var idProperty = typeof(DirectReport).GetProperty("Id");
        idProperty!.SetValue(indirectReport, indirectReportId);

        // Mark as indirect
        var isDirectProperty = typeof(DirectReport).GetProperty("IsDirect");
        isDirectProperty!.SetValue(indirectReport, false);

        var reviews = new List<PerformanceReview>
        {
            CreateReview(_testDirectReportId, PerformanceRating.MeetsExpectations),
            CreateReview(indirectReportId, PerformanceRating.ExceedsExpectations)
        };

        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport, indirectReport });

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.TotalReviews.Should().Be(1); // Only direct report's review
    }

    #endregion

    #region GetOneOnOnesAnalyticsAsync Tests

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_WithNoMeetings_ReturnsEmptyStats()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetOneOnOnesAnalyticsAsync();

        // Assert
        result.TotalMeetings.Should().Be(0);
        result.CompletedMeetings.Should().Be(0);
        result.ScheduledMeetings.Should().Be(0);
    }

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_SeparatesPastAndFutureMeetings()
    {
        // Arrange
        var pastMeeting = new OneOnOneMeeting(
            _testDirectReportId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            null,
            "Past meeting");

        var futureMeeting = new OneOnOneMeeting(
            _testDirectReportId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            null,
            "Future meeting");

        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { pastMeeting, futureMeeting });
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { pastMeeting, futureMeeting });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetOneOnOnesAnalyticsAsync();

        // Assert
        result.TotalMeetings.Should().Be(2);
        result.CompletedMeetings.Should().Be(1); // Past meeting
        result.ScheduledMeetings.Should().Be(1); // Future meeting
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
        _reviewRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetDirectReportAnalyticsAsync(_testDirectReportId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(_testDirectReportId);
        result.FullName.Should().Be("John Doe");
        result.Reviews.Should().NotBeNull();
        result.OneOnOnes.Should().NotBeNull();
        result.Tasks.Should().NotBeNull();
    }

    #endregion

    #region GetOneOnOneFrequencyReportAsync Tests

    [Fact]
    public async Task GetOneOnOneFrequencyReportAsync_ReturnsFrequencyForAllDirectReports()
    {
        // Arrange
        var pastMeeting = new OneOnOneMeeting(_testDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), null, "Past");

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { pastMeeting });

        // Act
        var result = await _service.GetOneOnOneFrequencyReportAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportId.Should().Be(_testDirectReportId);
        result[0].DirectReportName.Should().Be("John Doe");
        result[0].DaysSinceLastMeeting.Should().Be(10);
    }

    [Fact]
    public async Task GetOneOnOneFrequencyReportAsync_CalculatesFrequencyStatus()
    {
        // Arrange
        var recentMeeting = new OneOnOneMeeting(_testDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), null, "Recent");

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { recentMeeting });

        // Act
        var result = await _service.GetOneOnOneFrequencyReportAsync();

        // Assert
        result[0].FrequencyStatus.Should().Be("On Track"); // Within 14 days
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
    public async Task GetTeamVelocityAsync_CalculatesVelocityBySprint()
    {
        // Arrange
        var sprint1 = new Sprint("LP_1Q25_S1");
        var sprint2 = new Sprint("LP_1Q25_S2");

        var task1 = CreateTaskWithSprintAndPoints("T1", TaskStatus.Done, "LP_1Q25_S1", 3);
        var task2 = CreateTaskWithSprintAndPoints("T2", TaskStatus.Done, "LP_1Q25_S1", 5);
        var task3 = CreateTaskWithSprintAndPoints("T3", TaskStatus.Done, "LP_1Q25_S2", 8);

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, task3 });
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { sprint1, sprint2 });
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());

        // Act
        var result = await _service.GetTeamVelocityAsync();

        // Assert
        result.Sprints.Should().HaveCount(2);
        result.TotalStoryPointsCompleted.Should().Be(16); // 3 + 5 + 8
        result.AverageVelocity.Should().Be(8); // 16 / 2 sprints
    }

    #endregion

    #region GetCapacityAnalysisAsync — Predicted SP Tests

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_SameAvailability_KeepsSameSP()
    {
        // Past sprints had 4/5 members available and completed 40 SP each.
        // Future sprint also has 4/5 available.
        // Prediction should be 40 SP (ratio = 4/4 = 1.0).
        var (pastSprints, currentSprint, futureSprints) = CreateSprintsForPrediction(
            pastCount: 3, futureCount: 1);

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(5);

        // 1 person on leave during each past sprint AND the future sprint
        var leaves = new List<Leave>();
        foreach (var ps in pastSprints)
        {
            leaves.Add(new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                ps.StartDate!.Value,
                ps.EndDate!.Value));
        }
        leaves.Add(new Leave(
            directReports[0].Id,
            LeaveType.Vacation,
            futureSprints[0].StartDate!.Value,
            futureSprints[0].EndDate!.Value));

        // 40 SP completed per past sprint
        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 40));
        }

        SetupCapacityMocks(allSprints, tasks, directReports, leaves);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(40,
            "same availability ratio (4/4) should produce the same SP as the past average");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_ReducedAvailability_ScalesDown()
    {
        // Past sprints had full team of 4 (no leaves), completed 40 SP each.
        // Future sprint has 1 person on leave → 3 available.
        // Prediction should be 40 * (3/4) = 30 SP.
        var (pastSprints, currentSprint, futureSprints) = CreateSprintsForPrediction(
            pastCount: 2, futureCount: 1);

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(4);

        // No leaves in past sprints; 1 full-sprint leave in the future sprint
        var leaves = new List<Leave>
        {
            new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                futureSprints[0].StartDate!.Value,
                futureSprints[0].EndDate!.Value)
        };

        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 40));
        }

        SetupCapacityMocks(allSprints, tasks, directReports, leaves);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(30,
            "reduced availability (3/4 of past average) should scale down proportionally");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_IncreasedAvailability_ScalesUp()
    {
        // Past sprints had 3/5 members available (2 on leave), completed 30 SP each.
        // Future sprint has full team (5/5, no leaves).
        // Prediction should be 30 * (5/3) = 50 SP.
        var (pastSprints, currentSprint, futureSprints) = CreateSprintsForPrediction(
            pastCount: 2, futureCount: 1);

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(5);

        // 2 people on leave during each past sprint, no leaves in future
        var leaves = new List<Leave>();
        foreach (var ps in pastSprints)
        {
            leaves.Add(new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                ps.StartDate!.Value,
                ps.EndDate!.Value));
            leaves.Add(new Leave(
                directReports[1].Id,
                LeaveType.Vacation,
                ps.StartDate!.Value,
                ps.EndDate!.Value));
        }

        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 30));
        }

        SetupCapacityMocks(allSprints, tasks, directReports, leaves);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(50,
            "full availability (5/3 of past average) should scale up proportionally");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_NoPastLeaves_NoFutureLeaves_KeepsSameSP()
    {
        // Past sprints had full team (no leaves), completed 40 SP each.
        // Future sprint also has full team (no leaves).
        // Prediction should be 40 SP (ratio = 5/5 = 1.0).
        var (pastSprints, currentSprint, futureSprints) = CreateSprintsForPrediction(
            pastCount: 2, futureCount: 1);

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(5);

        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 40));
        }

        SetupCapacityMocks(allSprints, tasks, directReports, new List<Leave>());

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(40,
            "identical full availability should produce the same SP");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_CommitmentBasedMode_IgnoresAvailabilityRatio()
    {
        // When committed points are set on the future sprint,
        // Mode 1 (commitment-based) should be used instead of velocity-based.
        var (pastSprints, currentSprint, futureSprints) = CreateSprintsForPrediction(
            pastCount: 2, futureCount: 1);

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(5);

        // 40 SP completed per past sprint and current sprint, with 50 committed → 80% utilization
        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 40));
        }
        tasks.Add(CreateTaskWithSprintAndPoints("T-" + currentSprint.Name, TaskStatus.Done, currentSprint.Name, 40));

        var pastCapacities = pastSprints.Select(ps =>
            new SprintCapacity(ps.Id, 50, 5)).ToList();
        var currentCapacity = new SprintCapacity(currentSprint.Id, 50, 5);
        // Future sprint also has committed points
        var futureCapacity = new SprintCapacity(futureSprints[0].Id, 60, 5);
        var allCapacities = pastCapacities.Append(currentCapacity).Append(futureCapacity).ToList();

        // Leave in future sprint — should NOT affect Mode 1
        var leaves = new List<Leave>
        {
            new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                futureSprints[0].StartDate!.Value,
                futureSprints[0].EndDate!.Value)
        };

        SetupCapacityMocks(allSprints, tasks, directReports, leaves, allCapacities);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert — Mode 1: 60 committed * 80% utilization = 48
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(48,
            "commitment-based mode uses committed * utilization%, not availability ratio");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_PredictedSP_PartialLeaveOverlap_ScalesProportionally()
    {
        // Past sprints had full team of 4 (no leaves), completed 40 SP each.
        // Future sprint: 1 person on leave for only half the sprint (5 of 10 working days).
        // Lost capacity = 5/10 = 0.5 person → effective available = 3.5
        // Prediction should be 40 * (3.5/4) = 35 SP.

        // Find the next Monday from today for deterministic working-day calculations
        var now = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // Ensure future, not today

        // Past sprints: two 12-day spans (Mon-Fri, Mon-Fri = 10 working days each)
        var pastSprint1 = new Sprint("LP_4Q25_S1");
        pastSprint1.UpdateDates(now.AddDays(-60), now.AddDays(-60 + 11));
        var pastSprint2 = new Sprint("LP_4Q25_S2");
        pastSprint2.UpdateDates(now.AddDays(-48), now.AddDays(-48 + 11));
        var pastSprints = new List<Sprint> { pastSprint1, pastSprint2 };

        // Current sprint spanning today
        var currentSprint = new Sprint("LP_1Q26_S1");
        currentSprint.UpdateDates(now.AddDays(-7), now.AddDays(6));

        // Future sprint: starts on the next Monday, runs Mon-Fri + Mon-Fri (10 working days)
        var futureStart = now.AddDays(daysUntilMonday);
        var futureSprint = new Sprint("LP_2Q26_S1");
        futureSprint.UpdateDates(futureStart, futureStart.AddDays(11)); // 12 calendar days = 10 working days
        var futureSprints = new List<Sprint> { futureSprint };

        var allSprints = pastSprints.Concat(futureSprints).Append(currentSprint).ToList();
        var directReports = CreateDirectReports(4);

        // Leave covers only the first week of the future sprint (Mon-Fri = 5 working days)
        var leaves = new List<Leave>
        {
            new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                futureStart,
                futureStart.AddDays(4)) // Mon-Fri = 5 working days
        };

        var tasks = new List<TeamTask>();
        foreach (var ps in pastSprints)
        {
            tasks.Add(CreateTaskWithSprintAndPoints("T-" + ps.Name, TaskStatus.Done, ps.Name, 40));
        }

        SetupCapacityMocks(allSprints, tasks, directReports, leaves);

        // Act
        var result = await _service.GetCapacityAnalysisAsync();

        // Assert — 5 leave days / 10 working days = 0.5 lost → 3.5 available
        // ratio = 3.5 / 4.0 = 0.875 → 40 * 0.875 = 35
        result.FutureSprints.Should().HaveCount(1);
        result.FutureSprints[0].PredictedPoints.Should().Be(35,
            "half-sprint leave should reduce prediction proportionally");
    }

    #endregion

    #region GetCapacityAnalysisAsync — TotalStoryPoints Tests

    [Fact]
    public async Task GetCapacityAnalysisAsync_TotalStoryPoints_SumsParentSPForCurrentSprint()
    {
        // Create a current sprint with tasks linked to parents.
        // TotalStoryPoints should be the sum of SP from parent child tasks.
        var currentSprint = new Sprint("LP_1Q25_S1");
        currentSprint.UpdateDates(
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date.AddDays(7));

        var parentId = Guid.NewGuid();
        var parent = new Parent("Epic A");
        typeof(Parent).GetProperty("Id")!.SetValue(parent, parentId);

        // Two tasks in this sprint, both under the same parent
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
            "sum of child task SP (5 + 8) for the parent in this sprint");
    }

    [Fact]
    public async Task GetCapacityAnalysisAsync_TotalStoryPoints_ZeroWhenNoParentTasks()
    {
        // Sprint tasks with no parent → TotalStoryPoints should be 0.
        var currentSprint = new Sprint("LP_1Q25_S1");
        currentSprint.UpdateDates(
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date.AddDays(7));

        var task = new TeamTask("T1", sprint: currentSprint.Name, storyPoints: 5);

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
        result.CurrentSprint!.TotalStoryPoints.Should().Be(0);
    }

    #endregion

    #region GetDashboardInsightsAsync — UnmatchedTaskCount Tests

    [Fact]
    public async Task GetDashboardOverviewAsync_UnmatchedTaskCount_CountsTasksNotMatchingAnyProject()
    {
        // 2 tasks: one with labels matching a project, one with no matching labels.
        var project = new Project("Project A", labels: "frontend,api");

        var matchedTask = CreateTaskWithLabels("Matched", "frontend");
        var unmatchedTask = CreateTaskWithLabels("Unmatched", "backend");
        var noLabelsTask = CreateTaskWithLabels("NoLabels", "");

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { matchedTask, unmatchedTask, noLabelsTask });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings("[]"));

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert — "backend" doesn't match "frontend,api"; empty labels don't match either
        result.Insights.UnmatchedTaskCount.Should().Be(2,
            "one task has non-matching labels and one has empty labels");
    }

    [Fact]
    public async Task GetDashboardOverviewAsync_UnmatchedTaskCount_ZeroWhenAllTasksMatch()
    {
        var project = new Project("Project A", labels: "frontend,api");

        var task1 = CreateTaskWithLabels("T1", "frontend");
        var task2 = CreateTaskWithLabels("T2", "api");

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2 });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings("[]"));

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Insights.UnmatchedTaskCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private void SetupBasicMocks()
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());
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
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings("[]"));
    }

    private PerformanceReview CreateReview(Guid directReportId, PerformanceRating rating)
    {
        var review = new PerformanceReview(
            directReportId,
            "2024",
            DateTime.UtcNow);

        review.UpdateContent("Strengths", "Areas", "Manager notes", rating);

        return review;
    }

    private TeamTask CreateTask(string title, TaskStatus status)
    {
        var task = new TeamTask(
            title,
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "",
            "",
            "",
            null,
            null);

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
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            assigneeId,
            null,
            null,
            null,
            null,
            "",
            "",
            "",
            null,
            null);

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
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            storyPoints,
            "",
            "",
            sprint,
            null,
            null);

        if (status == TaskStatus.Done)
        {
            task.Start();
            task.Complete();
        }

        return task;
    }

    private TeamTask CreateTaskWithLabels(string title, string labels)
    {
        return new TeamTask(
            title,
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            labels: labels);
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
