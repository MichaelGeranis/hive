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

    #region Product Engagement Tests

    [Fact]
    public async Task GetProductEngagement_ReturnsOkWithEngagementForThePeriod()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetProductEngagementAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductEngagementDto { Months = 3 });

        // Act
        var result = await _controller.GetProductEngagement(3, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ProductEngagementDto>().Which.Months.Should().Be(3);
    }

    [Fact]
    public async Task GetProductEngagement_InvalidPeriod_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetProductEngagementAsync(0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Hive.Core.Exceptions.DomainException("The period must be a positive number of months."));

        // Act
        var result = await _controller.GetProductEngagement(0, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Focus Tests

    [Fact]
    public async Task GetFocus_ReturnsOkWithTheFocusPanel()
    {
        // Arrange
        var focus = new DashboardFocusDto
        {
            Today = new DateOnly(2026, 9, 24),
            OneOnOnes = new OneOnOneCadenceOverviewDto { CadenceDays = 14, NeedsAttentionCount = 1 }
        };
        _serviceMock.Setup(s => s.GetDashboardFocusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(focus);

        // Act
        var result = await _controller.GetFocus(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(focus);
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

    #region Helper Methods

    private static DashboardOverviewDto CreateDashboardOverview()
    {
        return new DashboardOverviewDto
        {
            Team = new TeamOverviewDto { TotalDirectReports = 5 },
            Tasks = CreateTasksOverview(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static TasksOverviewDto CreateTasksOverview()
    {
        return new TasksOverviewDto
        {
            Projects = new ProjectsSummaryDto { TotalProjects = 5 },
            Tasks = new TasksSummaryDto { TotalTasks = 50, DoneTasks = 30 },
            TasksByAssignee = new List<TasksByAssigneeDto>(),
            TasksByType = new List<TasksByTypeDto>(),
            TasksByPriority = new List<TasksByPriorityDto>()
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
            OneOnOnes = new OneOnOneAnalyticsDto { TotalMeetings = 20 },
            Tasks = new TaskAnalyticsDto { TotalTasks = 15 }
        };
    }

    #endregion

    #region Sprint trends

    [Fact]
    public async Task GetCarryOverTrend_PassesTheSprintCountAndReturnsTheTrend()
    {
        var trend = new CarryOverTrendDto { AverageCarriedInPercent = 12.5 };
        _serviceMock.Setup(s => s.GetCarryOverTrendAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(trend);

        var result = await _controller.GetCarryOverTrend(6);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(trend);
    }

    [Fact]
    public async Task GetBugSupportInflow_PassesTheSprintCountAndReturnsTheInflow()
    {
        var inflow = new BugSupportInflowDto { InflowPercent = 30 };
        _serviceMock.Setup(s => s.GetBugSupportInflowAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(inflow);

        var result = await _controller.GetBugSupportInflow(3);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(inflow);
    }

    #endregion
}
