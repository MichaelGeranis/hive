using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
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
    /// <param name="sprintCount">Optional number of recent sprints to include for task metrics. If null, returns all tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard overview with team and tasks metrics, insights and warnings.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboard([FromQuery] int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating dashboard overview with sprintCount={SprintCount}", sprintCount);
        var dashboard = await _service.GetDashboardOverviewAsync(sprintCount, cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Gets who has worked on which product, for the Product engagement widget.
    /// </summary>
    /// <param name="months">How many months back to look, by the tasks' sprints. If null, all time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Members per product, products per member, and the products nobody has worked on.</returns>
    [HttpGet("product-engagement")]
    [ProducesResponseType(typeof(ProductEngagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductEngagementDto>> GetProductEngagement([FromQuery] int? months = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.GetProductEngagementAsync(months, cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets what needs the manager's attention now: the current sprint, 1:1s against the
    /// cadence, to-dos coming due and who is away. Not affected by the sprint filter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard's focus panel.</returns>
    [HttpGet("focus")]
    [ProducesResponseType(typeof(DashboardFocusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardFocusDto>> GetFocus(CancellationToken cancellationToken)
    {
        var focus = await _service.GetDashboardFocusAsync(cancellationToken);
        return Ok(focus);
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
        var analytics = await _service.GetTasksAnalyticsAsync(null, cancellationToken);
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
    /// Gets team velocity metrics based on completed story points per sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sprint velocity data and averages.</returns>
    [HttpGet("team-velocity")]
    [ProducesResponseType(typeof(TeamVelocityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamVelocityDto>> GetTeamVelocity([FromQuery] int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating team velocity report with sprintCount={SprintCount}", sprintCount);
        var velocity = await _service.GetTeamVelocityAsync(sprintCount, cancellationToken);
        return Ok(velocity);
    }

    /// <summary>
    /// Gets, per sprint, how much work was carried in from an earlier sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("carry-over")]
    [ProducesResponseType(typeof(CarryOverTrendDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarryOverTrendDto>> GetCarryOverTrend([FromQuery] int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating carry-over trend with sprintCount={SprintCount}", sprintCount);
        return Ok(await _service.GetCarryOverTrendAsync(sprintCount, cancellationToken));
    }

    /// <summary>
    /// Gets, per sprint, the new bugs and support tickets, by Jira issue type.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent sprints to include. If null, returns all sprints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("bug-support-inflow")]
    [ProducesResponseType(typeof(BugSupportInflowDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BugSupportInflowDto>> GetBugSupportInflow([FromQuery] int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating bug and support inflow with sprintCount={SprintCount}", sprintCount);
        return Ok(await _service.GetBugSupportInflowAsync(sprintCount, cancellationToken));
    }

    /// <summary>
    /// Gets capacity analysis comparing planned vs actual story points by sprint.
    /// </summary>
    /// <param name="sprintCount">Optional number of recent past sprints to include. If null, returns all sprints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Capacity analysis with past, current, and future sprint data.</returns>
    [HttpGet("capacity-analysis")]
    [ProducesResponseType(typeof(CapacityAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CapacityAnalysisDto>> GetCapacityAnalysis([FromQuery] int? sprintCount = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating capacity analysis report with sprintCount={SprintCount}", sprintCount);
        var analysis = await _service.GetCapacityAnalysisAsync(sprintCount, cancellationToken);
        return Ok(analysis);
    }
}
