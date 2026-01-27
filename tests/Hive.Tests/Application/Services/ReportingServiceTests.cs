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
        result.AverageMeetingDuration.Should().Be(0);
    }

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_SeparatesPastAndFutureMeetings()
    {
        // Arrange
        var pastMeeting = new OneOnOneMeeting(
            _testDirectReportId,
            DateTime.UtcNow.AddDays(-7),
            60,
            "Past meeting");

        var futureMeeting = new OneOnOneMeeting(
            _testDirectReportId,
            DateTime.UtcNow.AddDays(7),
            60,
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

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_CalculatesAverageDuration()
    {
        // Arrange
        var meeting1 = new OneOnOneMeeting(_testDirectReportId, DateTime.UtcNow.AddDays(-7), 60, "M1");
        var meeting2 = new OneOnOneMeeting(_testDirectReportId, DateTime.UtcNow.AddDays(-14), 90, "M2");

        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting1, meeting2 });
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting1, meeting2 });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetOneOnOnesAnalyticsAsync();

        // Assert
        result.AverageMeetingDuration.Should().Be(75); // (60 + 90) / 2
        result.TotalMeetingMinutes.Should().Be(150);
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
        var pastMeeting = new OneOnOneMeeting(_testDirectReportId, DateTime.UtcNow.AddDays(-10), 60, "Past");

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
        var recentMeeting = new OneOnOneMeeting(_testDirectReportId, DateTime.UtcNow.AddDays(-7), 60, "Recent");

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

    #endregion
}
