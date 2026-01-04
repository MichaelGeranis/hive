using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportingService> _serviceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _serviceMock = new Mock<IReportingService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();
        _controller = new ReportsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboard_ReturnsOkWithDashboardOverview()
    {
        // Arrange
        var dashboard = CreateDashboardOverview();
        _serviceMock.Setup(s => s.GetDashboardOverviewAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _controller.GetDashboard(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<DashboardOverviewDto>();
    }

    [Fact]
    public async Task GetDashboard_CallsServiceMethod()
    {
        // Arrange
        var dashboard = CreateDashboardOverview();
        _serviceMock.Setup(s => s.GetDashboardOverviewAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        await _controller.GetDashboard(null, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.GetDashboardOverviewAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Reviews Analytics Tests

    [Fact]
    public async Task GetReviewsAnalytics_ReturnsOkWithReviewsOverview()
    {
        // Arrange
        var reviews = CreateReviewsOverview();
        _serviceMock.Setup(s => s.GetReviewsAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsAnalytics(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ReviewsOverviewDto>();
    }

    #endregion

    #region One-on-Ones Analytics Tests

    [Fact]
    public async Task GetOneOnOnesAnalytics_ReturnsOkWithOneOnOnesOverview()
    {
        // Arrange
        var oneOnOnes = CreateOneOnOnesOverview();
        _serviceMock.Setup(s => s.GetOneOnOnesAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(oneOnOnes);

        // Act
        var result = await _controller.GetOneOnOnesAnalytics(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<OneOnOnesOverviewDto>();
    }

    #endregion

    #region Tasks Analytics Tests

    [Fact]
    public async Task GetTasksAnalytics_ReturnsOkWithTasksOverview()
    {
        // Arrange
        var tasks = CreateTasksOverview();
        _serviceMock.Setup(s => s.GetTasksAnalyticsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetTasksAnalytics(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<TasksOverviewDto>();
    }

    #endregion

    #region Direct Report Analytics Tests

    [Fact]
    public async Task GetDirectReportAnalytics_WhenExists_ReturnsOkWithAnalytics()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var analytics = CreateDirectReportAnalytics(directReportId);
        _serviceMock.Setup(s => s.GetDirectReportAnalyticsAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analytics);

        // Act
        var result = await _controller.GetDirectReportAnalytics(directReportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<DirectReportAnalyticsDto>();
    }

    [Fact]
    public async Task GetDirectReportAnalytics_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetDirectReportAnalyticsAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReportAnalyticsDto?)null);

        // Act
        var result = await _controller.GetDirectReportAnalytics(directReportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region One-on-One Frequency Report Tests

    [Fact]
    public async Task GetOneOnOneFrequencyReport_ReturnsOkWithFrequencyData()
    {
        // Arrange
        var frequencyData = CreateFrequencyReport();
        _serviceMock.Setup(s => s.GetOneOnOneFrequencyReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(frequencyData);

        // Act
        var result = await _controller.GetOneOnOneFrequencyReport(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<OneOnOneFrequencyDto>>();
    }

    #endregion

    #region Action Items Summary Tests

    [Fact]
    public async Task GetActionItemsSummary_ReturnsOkWithSummary()
    {
        // Arrange
        var summary = CreateActionItemsSummary();
        _serviceMock.Setup(s => s.GetActionItemsSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetActionItemsSummary(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ActionItemsSummaryDto>();
    }

    #endregion

    #region Tasks By Assignee Report Tests

    [Fact]
    public async Task GetTasksByAssigneeReport_ReturnsOkWithReport()
    {
        // Arrange
        var report = CreateTasksByAssigneeReport();
        _serviceMock.Setup(s => s.GetTasksByAssigneeReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTasksByAssigneeReport(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<TasksByAssigneeDto>>();
    }

    #endregion

    #region Helper Methods

    private static DashboardOverviewDto CreateDashboardOverview()
    {
        return new DashboardOverviewDto
        {
            Team = new TeamOverviewDto { TotalDirectReports = 5 },
            Reviews = CreateReviewsOverview(),
            OneOnOnes = CreateOneOnOnesOverview(),
            Tasks = CreateTasksOverview(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static ReviewsOverviewDto CreateReviewsOverview()
    {
        return new ReviewsOverviewDto
        {
            TotalReviews = 10,
            DraftReviews = 2,
            SubmittedReviews = 3,
            AcknowledgedReviews = 2,
            CompletedReviews = 3,
            CompletionRate = 30,
            RatingDistribution = new List<RatingDistributionDto>(),
            ReviewsByPeriod = new List<ReviewByPeriodDto>()
        };
    }

    private static OneOnOnesOverviewDto CreateOneOnOnesOverview()
    {
        return new OneOnOnesOverviewDto
        {
            TotalMeetings = 25,
            CompletedMeetings = 20,
            ScheduledMeetings = 5,
            CancelledMeetings = 0,
            RescheduledMeetings = 0,
            CompletionRate = 80,
            TotalMeetingMinutes = 600,
            AverageMeetingDuration = 30,
            FrequencyByDirectReport = new List<OneOnOneFrequencyDto>(),
            ActionItemsSummary = new List<ActionItemsSummaryDto>()
        };
    }

    private static TasksOverviewDto CreateTasksOverview()
    {
        return new TasksOverviewDto
        {
            Projects = new ProjectsSummaryDto { TotalProjects = 5, ActiveProjects = 3 },
            Tasks = new TasksSummaryDto { TotalTasks = 50, DoneTasks = 30 },
            TasksByAssignee = new List<TasksByAssigneeDto>(),
            TasksByType = new List<TasksByTypeDto>(),
            TasksByPriority = new List<TasksByPriorityDto>(),
            Productivity = new ProductivityMetricsDto { }
        };
    }

    private static DirectReportAnalyticsDto CreateDirectReportAnalytics(Guid directReportId)
    {
        return new DirectReportAnalyticsDto
        {
            DirectReportId = directReportId,
            FullName = "John Doe",
            JobTitle = "Software Engineer",
            TenureMonths = 24,
            Reviews = new ReviewsAnalyticsDto { TotalReviews = 4 },
            OneOnOnes = new OneOnOneAnalyticsDto { TotalMeetings = 20 },
            Tasks = new TaskAnalyticsDto { TotalTasks = 15 }
        };
    }

    private static IReadOnlyList<OneOnOneFrequencyDto> CreateFrequencyReport()
    {
        return new List<OneOnOneFrequencyDto>
        {
            new()
            {
                DirectReportId = Guid.NewGuid(),
                DirectReportName = "John Doe",
                TotalMeetings = 10,
                CompletedMeetings = 8,
                DaysSinceLastMeeting = 7,
                FrequencyStatus = "On Track"
            },
            new()
            {
                DirectReportId = Guid.NewGuid(),
                DirectReportName = "Jane Smith",
                TotalMeetings = 8,
                CompletedMeetings = 6,
                DaysSinceLastMeeting = 21,
                FrequencyStatus = "Overdue"
            }
        };
    }

    private static ActionItemsSummaryDto CreateActionItemsSummary()
    {
        return new ActionItemsSummaryDto
        {
            TotalActionItems = 30,
            OpenItems = 10,
            InProgressItems = 5,
            CompletedItems = 12,
            CancelledItems = 3,
            OverdueItems = 2,
            CompletionRate = 40
        };
    }

    private static IReadOnlyList<TasksByAssigneeDto> CreateTasksByAssigneeReport()
    {
        return new List<TasksByAssigneeDto>
        {
            new()
            {
                AssigneeId = Guid.NewGuid(),
                AssigneeName = "John Doe",
                TotalTasks = 15,
                CompletedTasks = 10,
                InProgressTasks = 3,
                OverdueTasks = 1,
                CompletionRate = 66.7
            },
            new()
            {
                AssigneeId = null,
                AssigneeName = "Unassigned",
                TotalTasks = 5,
                CompletedTasks = 0,
                InProgressTasks = 0,
                OverdueTasks = 0,
                CompletionRate = 0
            }
        };
    }

    #endregion
}
