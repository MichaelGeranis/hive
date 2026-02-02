using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for quarterly planning operations.
/// </summary>
[ApiController]
[Route("api/quarterly-planning")]
[Authorize]
[Produces("application/json")]
public class QuarterlyPlanningController : ControllerBase
{
    private readonly IQuarterlyPlanningService _planningService;
    private readonly IQuarterlyPlanningInsightsService _insightsService;
    private readonly ILogger<QuarterlyPlanningController> _logger;

    public QuarterlyPlanningController(
        IQuarterlyPlanningService planningService,
        IQuarterlyPlanningInsightsService insightsService,
        ILogger<QuarterlyPlanningController> logger)
    {
        _planningService = planningService;
        _insightsService = insightsService;
        _logger = logger;
    }

    #region Quarter Endpoints

    /// <summary>
    /// Gets all quarters.
    /// </summary>
    [HttpGet("quarters")]
    [ProducesResponseType(typeof(IEnumerable<QuarterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuarterDto>>> GetAllQuarters(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all quarters");
        var quarters = await _planningService.GetAllQuartersAsync(cancellationToken);
        return Ok(quarters);
    }

    /// <summary>
    /// Gets the currently active quarter.
    /// </summary>
    [HttpGet("quarters/active")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> GetActiveQuarter(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active quarter");
        var quarter = await _planningService.GetActiveQuarterAsync(cancellationToken);
        if (quarter is null)
        {
            return NotFound(new { message = "No active quarter found." });
        }
        return Ok(quarter);
    }

    /// <summary>
    /// Gets a quarter by ID.
    /// </summary>
    [HttpGet("quarters/{id:guid}")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> GetQuarterById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting quarter with ID: {Id}", id);
        var quarter = await _planningService.GetQuarterByIdAsync(id, cancellationToken);
        if (quarter is null)
        {
            return NotFound(new { message = $"Quarter with ID '{id}' not found." });
        }
        return Ok(quarter);
    }

    /// <summary>
    /// Gets a quarter by year and quarter number.
    /// </summary>
    [HttpGet("quarters/{year:int}/{quarterNumber:int}")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> GetQuarterByYearQuarter(int year, int quarterNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting quarter: Q{Quarter} {Year}", quarterNumber, year);
        var quarter = await _planningService.GetQuarterByYearQuarterAsync(year, quarterNumber, cancellationToken);
        if (quarter is null)
        {
            return NotFound(new { message = $"Quarter Q{quarterNumber} {year} not found." });
        }
        return Ok(quarter);
    }

    /// <summary>
    /// Creates a new quarter.
    /// </summary>
    [HttpPost("quarters")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuarterDto>> CreateQuarter([FromBody] CreateQuarterDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating quarter: Q{Quarter} {Year}", dto.QuarterNumber, dto.Year);
        try
        {
            var created = await _planningService.CreateQuarterAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetQuarterById), new { id = created.Id }, created);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates a quarter.
    /// </summary>
    [HttpPut("quarters/{id:guid}")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> UpdateQuarter(Guid id, [FromBody] UpdateQuarterDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating quarter with ID: {Id}", id);
        try
        {
            var updated = await _planningService.UpdateQuarterAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Activates a quarter.
    /// </summary>
    [HttpPost("quarters/{id:guid}/activate")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> ActivateQuarter(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating quarter with ID: {Id}", id);
        try
        {
            var activated = await _planningService.ActivateQuarterAsync(id, cancellationToken);
            return Ok(activated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Marks a quarter as completed.
    /// </summary>
    [HttpPost("quarters/{id:guid}/complete")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> CompleteQuarter(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing quarter with ID: {Id}", id);
        try
        {
            var completed = await _planningService.CompleteQuarterAsync(id, cancellationToken);
            return Ok(completed);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a quarter and all associated data.
    /// </summary>
    [HttpDelete("quarters/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuarter(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting quarter with ID: {Id}", id);
        try
        {
            await _planningService.DeleteQuarterAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Initiative Endpoints

    /// <summary>
    /// Gets initiatives for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/initiatives")]
    [ProducesResponseType(typeof(IEnumerable<InitiativeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InitiativeDto>>> GetInitiativesByQuarter(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting initiatives for quarter: {QuarterId}", quarterId);
        var initiatives = await _planningService.GetInitiativesByQuarterAsync(quarterId, cancellationToken);
        return Ok(initiatives);
    }

    /// <summary>
    /// Gets an initiative by ID.
    /// </summary>
    [HttpGet("initiatives/{id:guid}")]
    [ProducesResponseType(typeof(InitiativeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InitiativeDto>> GetInitiativeById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting initiative with ID: {Id}", id);
        var initiative = await _planningService.GetInitiativeByIdAsync(id, cancellationToken);
        if (initiative is null)
        {
            return NotFound(new { message = $"Initiative with ID '{id}' not found." });
        }
        return Ok(initiative);
    }

    /// <summary>
    /// Creates a new initiative.
    /// </summary>
    [HttpPost("initiatives")]
    [ProducesResponseType(typeof(InitiativeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InitiativeDto>> CreateInitiative([FromBody] CreateInitiativeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating initiative: {Name}", dto.Name);
        try
        {
            var created = await _planningService.CreateInitiativeAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetInitiativeById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an initiative.
    /// </summary>
    [HttpPut("initiatives/{id:guid}")]
    [ProducesResponseType(typeof(InitiativeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InitiativeDto>> UpdateInitiative(Guid id, [FromBody] UpdateInitiativeDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating initiative with ID: {Id}", id);
        try
        {
            var updated = await _planningService.UpdateInitiativeAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an initiative.
    /// </summary>
    [HttpDelete("initiatives/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInitiative(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting initiative with ID: {Id}", id);
        try
        {
            await _planningService.DeleteInitiativeAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Allocation Endpoints

    /// <summary>
    /// Gets allocations for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/allocations")]
    [ProducesResponseType(typeof(IEnumerable<AllocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AllocationDto>>> GetAllocationsByQuarter(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting allocations for quarter: {QuarterId}", quarterId);
        var allocations = await _planningService.GetAllocationsByQuarterAsync(quarterId, cancellationToken);
        return Ok(allocations);
    }

    /// <summary>
    /// Creates a new allocation.
    /// </summary>
    [HttpPost("allocations")]
    [ProducesResponseType(typeof(AllocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AllocationDto>> CreateAllocation([FromBody] CreateAllocationDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating allocation");
        try
        {
            var created = await _planningService.CreateAllocationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetAllocationById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets an allocation by ID.
    /// </summary>
    [HttpGet("allocations/{id:guid}")]
    [ProducesResponseType(typeof(AllocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllocationDto>> GetAllocationById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting allocation with ID: {Id}", id);
        var allocation = await _planningService.GetAllocationByIdAsync(id, cancellationToken);
        if (allocation is null)
        {
            return NotFound(new { message = $"Allocation with ID '{id}' not found." });
        }
        return Ok(allocation);
    }

    /// <summary>
    /// Deletes an allocation.
    /// </summary>
    [HttpDelete("allocations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAllocation(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting allocation with ID: {Id}", id);
        try
        {
            await _planningService.DeleteAllocationAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Sprint Goal Endpoints

    /// <summary>
    /// Gets sprint goals for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/sprint-goals")]
    [ProducesResponseType(typeof(IEnumerable<SprintGoalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SprintGoalDto>>> GetSprintGoalsByQuarter(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint goals for quarter: {QuarterId}", quarterId);
        var goals = await _planningService.GetSprintGoalsByQuarterAsync(quarterId, cancellationToken);
        return Ok(goals);
    }

    /// <summary>
    /// Creates or updates a sprint goal.
    /// </summary>
    [HttpPut("sprint-goals")]
    [ProducesResponseType(typeof(SprintGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintGoalDto>> UpsertSprintGoal([FromBody] UpsertSprintGoalDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Upserting sprint goal");
        try
        {
            var goal = await _planningService.UpsertSprintGoalAsync(dto, cancellationToken);
            return Ok(goal);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Dependency Endpoints

    /// <summary>
    /// Gets dependencies for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/dependencies")]
    [ProducesResponseType(typeof(IEnumerable<InitiativeDependencyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InitiativeDependencyDto>>> GetDependenciesByQuarter(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting dependencies for quarter: {QuarterId}", quarterId);
        var dependencies = await _planningService.GetDependenciesByQuarterAsync(quarterId, cancellationToken);
        return Ok(dependencies);
    }

    /// <summary>
    /// Creates a new dependency.
    /// </summary>
    [HttpPost("dependencies")]
    [ProducesResponseType(typeof(InitiativeDependencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InitiativeDependencyDto>> CreateDependency([FromBody] CreateInitiativeDependencyDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating dependency");
        try
        {
            var created = await _planningService.CreateDependencyAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetDependenciesByQuarter), new { quarterId = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a dependency.
    /// </summary>
    [HttpDelete("dependencies/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDependency(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting dependency with ID: {Id}", id);
        try
        {
            await _planningService.DeleteDependencyAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Board and Insights Endpoints

    /// <summary>
    /// Gets the full planning board data for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/board")]
    [ProducesResponseType(typeof(PlanningBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanningBoardDto>> GetPlanningBoard(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting planning board for quarter: {QuarterId}", quarterId);
        try
        {
            var board = await _planningService.GetPlanningBoardAsync(quarterId, cancellationToken);
            return Ok(board);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets analysis insights for a quarter.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/insights")]
    [ProducesResponseType(typeof(PlanningInsightsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanningInsightsDto>> GetInsights(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting insights for quarter: {QuarterId}", quarterId);
        try
        {
            var insights = await _insightsService.GenerateInsightsAsync(quarterId, cancellationToken);
            return Ok(insights);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Export Endpoints

    /// <summary>
    /// Exports the planning board to Excel format.
    /// </summary>
    [HttpGet("quarters/{quarterId:guid}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportPlanningBoardToExcel(Guid quarterId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exporting planning board for quarter: {QuarterId}", quarterId);
        try
        {
            var excelBytes = await _planningService.ExportPlanningBoardToExcelAsync(quarterId, cancellationToken);
            var quarter = await _planningService.GetQuarterByIdAsync(quarterId, cancellationToken);
            var fileName = $"{quarter?.Name.Replace(" ", "-")}_Plan-Catalogue.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion
}
