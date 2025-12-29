using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing leave requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _service;
    private readonly ILogger<LeavesController> _logger;

    public LeavesController(ILeaveService service, ILogger<LeavesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all leave requests.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all leave requests");
        var leaves = await _service.GetAllAsync(cancellationToken);
        return Ok(leaves);
    }

    /// <summary>
    /// Gets a leave request by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leave request with ID: {Id}", id);
        var leave = await _service.GetByIdAsync(id, cancellationToken);

        if (leave is null)
        {
            return NotFound(new { message = $"Leave request with ID '{id}' not found." });
        }

        return Ok(leave);
    }

    /// <summary>
    /// Gets leave requests for a specific team member.
    /// </summary>
    [HttpGet("by-member/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leaves for direct report: {DirectReportId}", directReportId);
        var leaves = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(leaves);
    }

    /// <summary>
    /// Gets leave requests by status.
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetByStatus(
        string status,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting leaves with status: {Status}", status);
            var leaves = await _service.GetByStatusAsync(status, cancellationToken);
            return Ok(leaves);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets leave requests within a date range.
    /// </summary>
    [HttpGet("by-date-range")]
    [ProducesResponseType(typeof(IEnumerable<LeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leaves between {StartDate} and {EndDate}", startDate, endDate);
        var leaves = await _service.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return Ok(leaves);
    }

    /// <summary>
    /// Gets upcoming leave requests.
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IEnumerable<LeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetUpcoming(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting upcoming leaves for next {Days} days", days);
        var leaves = await _service.GetUpcomingAsync(days, cancellationToken);
        return Ok(leaves);
    }

    /// <summary>
    /// Gets team leave overview for dashboard.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(TeamLeaveOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamLeaveOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting team leave overview");
        var overview = await _service.GetTeamOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    /// <summary>
    /// Gets monthly leave trend.
    /// </summary>
    [HttpGet("monthly-trend")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyLeaveSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonthlyLeaveSummaryDto>>> GetMonthlyTrend(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting monthly leave trend for {Months} months", months);
        var trend = await _service.GetMonthlyTrendAsync(months, cancellationToken);
        return Ok(trend);
    }

    /// <summary>
    /// Gets team leave balances for a year.
    /// </summary>
    [HttpGet("balances/{year:int}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaveBalanceDto>>> GetBalances(
        int year,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leave balances for year {Year}", year);
        var balances = await _service.GetTeamBalancesAsync(year, cancellationToken);
        return Ok(balances);
    }

    /// <summary>
    /// Creates a new leave request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> Create(
        [FromBody] CreateLeaveDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating leave request for direct report: {DirectReportId}", dto.DirectReportId);
            var leave = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = leave.Id }, leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing leave request.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> Update(
        Guid id,
        [FromBody] UpdateLeaveDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating leave request: {Id}", id);
            var leave = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Approves a leave request.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> Approve(
        Guid id,
        [FromBody] ApproveLeaveDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Approving leave request: {Id}", id);
            var leave = await _service.ApproveAsync(id, dto, cancellationToken);
            return Ok(leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rejects a leave request.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> Reject(
        Guid id,
        [FromBody] RejectLeaveDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Rejecting leave request: {Id}", id);
            var leave = await _service.RejectAsync(id, dto, cancellationToken);
            return Ok(leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a leave request.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(LeaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveDto>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Cancelling leave request: {Id}", id);
            var leave = await _service.CancelAsync(id, cancellationToken);
            return Ok(leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a leave request.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting leave request: {Id}", id);
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
