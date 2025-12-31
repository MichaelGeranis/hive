using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for generating reports and analytics dashboards.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _service;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportingService service, ILogger<ReportsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets the complete dashboard overview with all key metrics.
    /// </summary>
    /// <returns>Dashboard overview with team, reviews, one-on-ones, and tasks metrics.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboard(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating dashboard overview");
        var dashboard = await _service.GetDashboardOverviewAsync(cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Gets performance reviews analytics.
    /// </summary>
    /// <returns>Reviews overview with status breakdown and rating distribution.</returns>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ReviewsOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewsOverviewDto>> GetReviewsAnalytics(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating reviews analytics");
        var analytics = await _service.GetReviewsAnalyticsAsync(cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Gets one-on-one meetings analytics.
    /// </summary>
    /// <returns>One-on-ones overview with frequency and action items data.</returns>
    [HttpGet("one-on-ones")]
    [ProducesResponseType(typeof(OneOnOnesOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OneOnOnesOverviewDto>> GetOneOnOnesAnalytics(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating one-on-ones analytics");
        var analytics = await _service.GetOneOnOnesAnalyticsAsync(cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Gets tasks and projects analytics.
    /// </summary>
    /// <returns>Tasks overview with project summary and productivity metrics.</returns>
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(TasksOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TasksOverviewDto>> GetTasksAnalytics(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating tasks analytics");
        var analytics = await _service.GetTasksAnalyticsAsync(cancellationToken);
        return Ok(analytics);
    }

    /// <summary>
    /// Gets detailed analytics for a specific direct report.
    /// </summary>
    /// <param name="directReportId">The direct report ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed analytics for the direct report.</returns>
    [HttpGet("direct-reports/{directReportId:guid}")]
    [ProducesResponseType(typeof(DirectReportAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DirectReportAnalyticsDto>> GetDirectReportAnalytics(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating analytics for direct report: {DirectReportId}", directReportId);
        var analytics = await _service.GetDirectReportAnalyticsAsync(directReportId, cancellationToken);

        if (analytics is null)
        {
            return NotFound(new { message = $"Direct report with ID '{directReportId}' not found." });
        }

        return Ok(analytics);
    }

    /// <summary>
    /// Gets one-on-one meeting frequency report for all direct reports.
    /// </summary>
    /// <returns>Meeting frequency data per direct report.</returns>
    [HttpGet("one-on-one-frequency")]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneFrequencyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneFrequencyDto>>> GetOneOnOneFrequencyReport(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating one-on-one frequency report");
        var report = await _service.GetOneOnOneFrequencyReportAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Gets action items summary from one-on-one meetings.
    /// </summary>
    /// <returns>Summary of action items with status breakdown.</returns>
    [HttpGet("action-items")]
    [ProducesResponseType(typeof(ActionItemsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ActionItemsSummaryDto>> GetActionItemsSummary(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating action items summary");
        var summary = await _service.GetActionItemsSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gets tasks grouped by assignee.
    /// </summary>
    /// <returns>Task distribution and metrics per assignee.</returns>
    [HttpGet("tasks-by-assignee")]
    [ProducesResponseType(typeof(IEnumerable<TasksByAssigneeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TasksByAssigneeDto>>> GetTasksByAssigneeReport(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating tasks by assignee report");
        var report = await _service.GetTasksByAssigneeReportAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Gets team velocity metrics based on completed story points per sprint.
    /// </summary>
    /// <returns>Sprint velocity data and averages.</returns>
    [HttpGet("team-velocity")]
    [ProducesResponseType(typeof(TeamVelocityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamVelocityDto>> GetTeamVelocity(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating team velocity report");
        var velocity = await _service.GetTeamVelocityAsync(cancellationToken);
        return Ok(velocity);
    }

    /// <summary>
    /// Gets estimation accuracy metrics comparing estimated hours to actual time spent.
    /// </summary>
    /// <returns>Estimation accuracy by sprint, assignee, and project.</returns>
    [HttpGet("estimation-accuracy")]
    [ProducesResponseType(typeof(EstimationAccuracyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EstimationAccuracyDto>> GetEstimationAccuracy(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating estimation accuracy report");
        var accuracy = await _service.GetEstimationAccuracyAsync(cancellationToken);
        return Ok(accuracy);
    }
}
