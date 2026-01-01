using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

public class ReportingServiceTests
{
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IPerformanceReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IMeetingNoteRepository> _noteRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly ReportingService _service;

    public ReportingServiceTests()
    {
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _reviewRepositoryMock = new Mock<IPerformanceReviewRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _noteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();

        _service = new ReportingService(
            _directReportRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object);
    }

    #region Dashboard Overview Tests

    [Fact]
    public async Task GetDashboardOverviewAsync_ReturnsCompleteOverview()
    {
        // Arrange
        SetupEmptyRepositories();

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
    public async Task GetDashboardOverviewAsync_WithData_ReturnsCorrectCounts()
    {
        // Arrange
        var directReports = CreateDirectReports(3);
        var reviews = CreateReviews(directReports[0].Id, 2);
        var meetings = CreateMeetings(directReports[0].Id, 5);
        var tasks = CreateTasks(3);
        var projects = CreateProjects(2);

        SetupRepositories(directReports, reviews, meetings, new List<MeetingNote>(), tasks, projects);

        // Act
        var result = await _service.GetDashboardOverviewAsync();

        // Assert
        result.Team.TotalDirectReports.Should().Be(3);
        result.Reviews.TotalReviews.Should().Be(2);
        result.OneOnOnes.TotalMeetings.Should().Be(5);
        result.Tasks.Tasks.TotalTasks.Should().Be(3);
        result.Tasks.Projects.TotalProjects.Should().Be(2);
    }

    #endregion

    #region Reviews Analytics Tests

    [Fact]
    public async Task GetReviewsAnalyticsAsync_WithNoReviews_ReturnsZeroCounts()
    {
        // Arrange
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.TotalReviews.Should().Be(0);
        result.DraftReviews.Should().Be(0);
        result.CompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetReviewsAnalyticsAsync_CalculatesStatusBreakdownCorrectly()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var reviews = new List<PerformanceReview>
        {
            CreateReview(directReportId, ReviewStatus.Draft),
            CreateReview(directReportId, ReviewStatus.Draft),
            CreateReview(directReportId, ReviewStatus.Submitted),
            CreateReview(directReportId, ReviewStatus.Acknowledged),
            CreateReview(directReportId, ReviewStatus.Completed)
        };

        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.TotalReviews.Should().Be(5);
        result.DraftReviews.Should().Be(2);
        result.SubmittedReviews.Should().Be(1);
        result.AcknowledgedReviews.Should().Be(1);
        result.CompletedReviews.Should().Be(1);
        result.CompletionRate.Should().Be(20); // 1/5 = 20%
    }

    [Fact]
    public async Task GetReviewsAnalyticsAsync_CalculatesRatingDistribution()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var reviews = new List<PerformanceReview>
        {
            CreateCompletedReviewWithRating(directReportId, PerformanceRating.ExceedsExpectations),
            CreateCompletedReviewWithRating(directReportId, PerformanceRating.ExceedsExpectations),
            CreateCompletedReviewWithRating(directReportId, PerformanceRating.MeetsExpectations)
        };

        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetReviewsAnalyticsAsync();

        // Assert
        result.RatingDistribution.Should().NotBeEmpty();
        var exceedsCount = result.RatingDistribution.FirstOrDefault(r => r.Rating == PerformanceRating.ExceedsExpectations);
        exceedsCount.Should().NotBeNull();
        exceedsCount!.Count.Should().Be(2);
    }

    #endregion

    #region One-on-Ones Analytics Tests

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_WithNoMeetings_ReturnsZeroCounts()
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
        result.CompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_CalculatesStatusBreakdownCorrectly()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var meetings = new List<OneOnOneMeeting>
        {
            CreateMeeting(directReportId, isPast: false),  // Future meeting
            CreateMeeting(directReportId, isPast: false),  // Future meeting
            CreateMeeting(directReportId, isPast: true),   // Past meeting
            CreateMeeting(directReportId, isPast: true),   // Past meeting
            CreateMeeting(directReportId, isPast: true),   // Past meeting
        };

        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetOneOnOnesAnalyticsAsync();

        // Assert
        result.TotalMeetings.Should().Be(5);
        result.CompletedMeetings.Should().Be(3);  // Past meetings
        result.ScheduledMeetings.Should().Be(2);  // Future meetings
        result.CancelledMeetings.Should().Be(0);  // No longer tracking
        result.CompletionRate.Should().Be(60);    // 3/5 = 60%
    }

    [Fact]
    public async Task GetOneOnOnesAnalyticsAsync_CalculatesAverageMeetingDuration()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var meeting1 = CreateMeeting(directReportId, isPast: true, 30);
        var meeting2 = CreateMeeting(directReportId, isPast: true, 60);

        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting1, meeting2 });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetOneOnOnesAnalyticsAsync();

        // Assert
        result.TotalMeetingMinutes.Should().Be(90);
        result.AverageMeetingDuration.Should().Be(45);
    }

    #endregion

    #region Tasks Analytics Tests

    [Fact]
    public async Task GetTasksAnalyticsAsync_WithNoTasks_ReturnsZeroCounts()
    {
        // Arrange
        SetupEmptyRepositories();

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Tasks.TotalTasks.Should().Be(0);
        result.Projects.TotalProjects.Should().Be(0);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_CalculatesTaskStatusBreakdown()
    {
        // Arrange
        var tasks = new List<TeamTask>
        {
            CreateTask(TaskStatus.Backlog),
            CreateTask(TaskStatus.Todo),
            CreateTask(TaskStatus.InProgress),
            CreateTask(TaskStatus.InReview),
            CreateTask(TaskStatus.Done),
            CreateTask(TaskStatus.Cancelled)
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Tasks.TotalTasks.Should().Be(6);
        result.Tasks.BacklogTasks.Should().Be(1);
        result.Tasks.TodoTasks.Should().Be(1);
        result.Tasks.InProgressTasks.Should().Be(1);
        result.Tasks.InReviewTasks.Should().Be(1);
        result.Tasks.DoneTasks.Should().Be(1);
        result.Tasks.CancelledTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetTasksAnalyticsAsync_CalculatesProjectStatusBreakdown()
    {
        // Arrange
        var projects = new List<Project>
        {
            CreateProject(ProjectStatus.Planning),
            CreateProject(ProjectStatus.Active),
            CreateProject(ProjectStatus.OnHold),
            CreateProject(ProjectStatus.Completed),
            CreateProject(ProjectStatus.Cancelled)
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.GetTasksAnalyticsAsync();

        // Assert
        result.Projects.TotalProjects.Should().Be(5);
        result.Projects.PlanningProjects.Should().Be(1);
        result.Projects.ActiveProjects.Should().Be(1);
        result.Projects.OnHoldProjects.Should().Be(1);
        result.Projects.CompletedProjects.Should().Be(1);
        result.Projects.CancelledProjects.Should().Be(1);
    }

    #endregion

    #region Direct Report Analytics Tests

    [Fact]
    public async Task GetDirectReportAnalyticsAsync_WhenNotExists_ReturnsNull()
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
    public async Task GetDirectReportAnalyticsAsync_WhenExists_ReturnsAnalytics()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow.AddYears(-1));

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _reviewRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _taskRepositoryMock.Setup(r => r.GetByAssigneeIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetDirectReportAnalyticsAsync(directReport.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(directReport.Id);
        result.FullName.Should().Be("John Doe");
        result.TenureMonths.Should().BeGreaterThan(0);
    }

    #endregion

    #region One-on-One Frequency Report Tests

    [Fact]
    public async Task GetOneOnOneFrequencyReportAsync_WithNoDirectReports_ReturnsEmptyList()
    {
        // Arrange
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.GetOneOnOneFrequencyReportAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOneOnOneFrequencyReportAsync_CalculatesFrequencyStatus()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var recentMeeting = CreateMeeting(directReport.Id, isPast: true, 30);  // Past meeting

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { recentMeeting });

        // Act
        var result = await _service.GetOneOnOneFrequencyReportAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportId.Should().Be(directReport.Id);
        result[0].CompletedMeetings.Should().Be(1);
    }

    #endregion

    #region Action Items Summary Tests

    [Fact]
    public async Task GetActionItemsSummaryAsync_WithNoActionItems_ReturnsZeroCounts()
    {
        // Arrange
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetActionItemsSummaryAsync();

        // Assert
        result.TotalActionItems.Should().Be(0);
        result.OpenItems.Should().Be(0);
        result.CompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetActionItemsSummaryAsync_CalculatesStatusBreakdown()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var actionItems = new List<MeetingNote>
        {
            CreateActionItem(meetingId, ActionItemStatus.Open),
            CreateActionItem(meetingId, ActionItemStatus.Open),
            CreateActionItem(meetingId, ActionItemStatus.InProgress),
            CreateActionItem(meetingId, ActionItemStatus.Completed),
            CreateActionItem(meetingId, ActionItemStatus.Completed),
            CreateActionItem(meetingId, ActionItemStatus.Cancelled)
        };

        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actionItems);

        // Act
        var result = await _service.GetActionItemsSummaryAsync();

        // Assert
        result.TotalActionItems.Should().Be(6);
        result.OpenItems.Should().Be(2);
        result.InProgressItems.Should().Be(1);
        result.CompletedItems.Should().Be(2);
        result.CancelledItems.Should().Be(1);
        result.CompletionRate.Should().BeApproximately(33.3, 0.1); // 2/6 = 33.3%
    }

    #endregion

    #region Tasks By Assignee Report Tests

    [Fact]
    public async Task GetTasksByAssigneeReportAsync_GroupsTasksByAssignee()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var task1 = CreateTask(TaskStatus.Done, directReport.Id);
        var task2 = CreateTask(TaskStatus.InProgress, directReport.Id);
        var unassignedTask = CreateTask(TaskStatus.Backlog, null);

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, unassignedTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });

        // Act
        var result = await _service.GetTasksByAssigneeReportAsync();

        // Assert
        result.Should().HaveCount(2); // John Doe + Unassigned
        var johnTasks = result.FirstOrDefault(r => r.AssigneeId == directReport.Id);
        johnTasks.Should().NotBeNull();
        johnTasks!.TotalTasks.Should().Be(2);
        johnTasks.CompletedTasks.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyRepositories()
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
    }

    private void SetupRepositories(
        List<DirectReport> directReports,
        List<PerformanceReview> reviews,
        List<OneOnOneMeeting> meetings,
        List<MeetingNote> notes,
        List<TeamTask> tasks,
        List<Project> projects)
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        foreach (var dr in directReports)
        {
            _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(dr.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(meetings.Where(m => m.DirectReportId == dr.Id).ToList());
        }
    }

    private static List<DirectReport> CreateDirectReports(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new DirectReport($"First{i}", $"Last{i}", $"user{i}@test.com", "Engineer", "Engineering", DateTime.UtcNow.AddYears(-1)))
            .ToList();
    }

    private static List<PerformanceReview> CreateReviews(Guid directReportId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new PerformanceReview(directReportId, $"Q{i} 2024", DateTime.UtcNow))
            .ToList();
    }

    private static PerformanceReview CreateReview(Guid directReportId, ReviewStatus status)
    {
        var review = new PerformanceReview(directReportId, $"Q1 2024 - {Guid.NewGuid()}", DateTime.UtcNow);
        if (status == ReviewStatus.Submitted || status == ReviewStatus.Acknowledged || status == ReviewStatus.Completed)
        {
            review.UpdateContent("Strengths", "Areas", "Goals", "Notes", PerformanceRating.MeetsExpectations);
            review.Submit();
        }
        if (status == ReviewStatus.Acknowledged || status == ReviewStatus.Completed)
        {
            review.Acknowledge();
        }
        if (status == ReviewStatus.Completed)
        {
            review.Complete();
        }
        return review;
    }

    private static PerformanceReview CreateCompletedReviewWithRating(Guid directReportId, PerformanceRating rating)
    {
        var review = new PerformanceReview(directReportId, $"Q1 2024 - {Guid.NewGuid()}", DateTime.UtcNow);
        review.UpdateContent("Strengths", "Areas", "Goals", "Notes", rating);
        review.Submit();
        review.Acknowledge();
        review.Complete();
        return review;
    }

    private static List<OneOnOneMeeting> CreateMeetings(Guid directReportId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new OneOnOneMeeting(directReportId, DateTime.UtcNow.AddDays(i), 30))
            .ToList();
    }

    private static OneOnOneMeeting CreateMeeting(Guid directReportId, bool isPast, int duration = 30)
    {
        // Past meetings have date in the past, future meetings have date in the future
        var meetingDate = isPast ? DateTime.UtcNow.AddDays(-7) : DateTime.UtcNow.AddDays(7);
        return new OneOnOneMeeting(directReportId, meetingDate, duration);
    }

    private static List<TeamTask> CreateTasks(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new TeamTask($"Task {i}"))
            .ToList();
    }

    private static TeamTask CreateTask(TaskStatus status, Guid? assigneeId = null)
    {
        var task = new TeamTask($"Task {Guid.NewGuid()}", assigneeId: assigneeId);
        switch (status)
        {
            case TaskStatus.Todo:
                task.MoveToTodo();
                break;
            case TaskStatus.InProgress:
                task.MoveToTodo();
                task.Start();
                break;
            case TaskStatus.InReview:
                task.MoveToTodo();
                task.Start();
                task.MoveToReview();
                break;
            case TaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.Complete();
                break;
            case TaskStatus.Cancelled:
                task.Cancel();
                break;
        }
        return task;
    }

    private static List<Project> CreateProjects(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Project($"Project {i}"))
            .ToList();
    }

    private static Project CreateProject(ProjectStatus status)
    {
        var project = new Project($"Project {Guid.NewGuid()}");
        switch (status)
        {
            case ProjectStatus.Active:
                project.Activate();
                break;
            case ProjectStatus.OnHold:
                project.Activate();
                project.PutOnHold();
                break;
            case ProjectStatus.Completed:
                project.Complete();
                break;
            case ProjectStatus.Cancelled:
                project.Cancel();
                break;
        }
        return project;
    }

    private static MeetingNote CreateActionItem(Guid meetingId, ActionItemStatus status)
    {
        var note = new MeetingNote(meetingId, "Action item content", NoteCategory.ActionItem);
        if (status == ActionItemStatus.InProgress)
        {
            note.UpdateActionStatus(ActionItemStatus.InProgress);
        }
        else if (status == ActionItemStatus.Completed)
        {
            note.CompleteAction();
        }
        else if (status == ActionItemStatus.Cancelled)
        {
            note.UpdateActionStatus(ActionItemStatus.Cancelled);
        }
        return note;
    }

    #endregion
}
