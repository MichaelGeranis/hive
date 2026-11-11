using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for quarterly planning: quarters, their options and lanes, the roster and
/// allocation grid, and the computed board, draft, scenarios, commitment and review.
/// </summary>
[ApiController]
[Route("api/quarterly-planning")]
[Authorize]
[Produces("application/json")]
public class QuarterlyPlanningController : ControllerBase
{
    private readonly IQuarterlyPlanningService _planningService;
    private readonly IPlanningBoardService _boardService;
    private readonly ILogger<QuarterlyPlanningController> _logger;

    public QuarterlyPlanningController(
        IQuarterlyPlanningService planningService,
        IPlanningBoardService boardService,
        ILogger<QuarterlyPlanningController> logger)
    {
        _planningService = planningService;
        _boardService = boardService;
        _logger = logger;
    }

    #region Quarters

    /// <summary>Gets all quarters, newest first.</summary>
    [HttpGet("quarters")]
    [ProducesResponseType(typeof(IEnumerable<QuarterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuarterDto>>> GetAllQuarters(CancellationToken cancellationToken) =>
        Ok(await _planningService.GetAllQuartersAsync(cancellationToken));

    /// <summary>Gets a quarter by ID.</summary>
    [HttpGet("quarters/{id:guid}")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuarterDto>> GetQuarterById(Guid id, CancellationToken cancellationToken)
    {
        var quarter = await _planningService.GetQuarterByIdAsync(id, cancellationToken);
        return quarter is null ? NotFound(new { message = $"Quarter with ID '{id}' not found." }) : Ok(quarter);
    }

    /// <summary>Creates a quarter, with its Maintenance and Tech roadmap lanes.</summary>
    [HttpPost("quarters")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult> CreateQuarter([FromBody] CreateQuarterDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            _logger.LogInformation("Creating quarter Q{Quarter} {Year}", dto.QuarterNumber, dto.Year);
            var created = await _planningService.CreateQuarterAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetQuarterById), new { id = created.Id }, created);
        });

    /// <summary>Updates a quarter's OKR reference and target mix.</summary>
    [HttpPut("quarters/{id:guid}")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    public Task<ActionResult> UpdateQuarter(Guid id, [FromBody] UpdateQuarterDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _planningService.UpdateQuarterAsync(id, dto, cancellationToken)));

    [HttpPost("quarters/{id:guid}/activate")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    public Task<ActionResult> ActivateQuarter(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _planningService.ActivateQuarterAsync(id, cancellationToken)));

    [HttpPost("quarters/{id:guid}/complete")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    public Task<ActionResult> CompleteQuarter(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _planningService.CompleteQuarterAsync(id, cancellationToken)));

    /// <summary>Deletes a quarter and everything planned in it.</summary>
    [HttpDelete("quarters/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> DeleteQuarter(Guid id, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.DeleteQuarterAsync(id, cancellationToken);
            return NoContent();
        });

    #endregion

    #region Board

    /// <summary>Gets the quarter's plan with capacity, waterline, mix and insights.</summary>
    [HttpGet("quarters/{id:guid}/board")]
    [ProducesResponseType(typeof(PlanningBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> GetBoard(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _boardService.GetBoardAsync(id, cancellationToken)));

    /// <summary>Proposes allocations for the free cells; with apply=true, writes them.</summary>
    [HttpPost("quarters/{id:guid}/draft")]
    [ProducesResponseType(typeof(PlanDraftDto), StatusCodes.Status200OK)]
    public Task<ActionResult> Draft(Guid id, [FromQuery] bool apply, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _boardService.DraftAsync(id, apply, cancellationToken)));

    /// <summary>What the plan looks like if people are away or options start later. Nothing is saved.</summary>
    [HttpPost("quarters/{id:guid}/scenario")]
    [ProducesResponseType(typeof(ScenarioDto), StatusCodes.Status200OK)]
    public Task<ActionResult> RunScenario(Guid id, [FromBody] ScenarioRequestDto request, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _boardService.RunScenarioAsync(id, request, cancellationToken)));

    /// <summary>Commits the plan as it stands, as the baseline for the review.</summary>
    [HttpPost("quarters/{id:guid}/commit")]
    [ProducesResponseType(typeof(QuarterDto), StatusCodes.Status200OK)]
    public Task<ActionResult> Commit(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _boardService.CommitAsync(id, cancellationToken)));

    /// <summary>The committed plan against reality: options, sprint goals and a summary to share.</summary>
    [HttpGet("quarters/{id:guid}/review")]
    [ProducesResponseType(typeof(PlanReviewDto), StatusCodes.Status200OK)]
    public Task<ActionResult> GetReview(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Ok(await _boardService.GetReviewAsync(id, cancellationToken)));

    /// <summary>Delivered options by size, across quarters: estimate against what they took.</summary>
    [HttpGet("calibration")]
    [ProducesResponseType(typeof(CalibrationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalibrationDto>> GetCalibration(CancellationToken cancellationToken) =>
        Ok(await _boardService.GetCalibrationAsync(cancellationToken));

    /// <summary>Exports the plan as a Plan-Catalogue spreadsheet.</summary>
    [HttpGet("quarters/{id:guid}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public Task<ActionResult> Export(Guid id, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            var bytes = await _boardService.ExportToExcelAsync(id, cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plan-Catalogue.xlsx");
        });

    #endregion

    #region Roster, grid, notes, order

    [HttpPut("quarters/{id:guid}/roster")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> SetRosterMember(Guid id, [FromBody] SetRosterMemberDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.SetRosterMemberAsync(id, dto, cancellationToken);
            return NoContent();
        });

    [HttpDelete("quarters/{id:guid}/roster/{directReportId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> RemoveRosterMember(Guid id, Guid directReportId, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.RemoveRosterMemberAsync(id, directReportId, cancellationToken);
            return NoContent();
        });

    /// <summary>Replaces whole cells of the allocation grid.</summary>
    [HttpPut("quarters/{id:guid}/cells")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> SetCells(Guid id, [FromBody] SetCellsDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.SetCellsAsync(id, dto, cancellationToken);
            return NoContent();
        });

    [HttpPut("quarters/{id:guid}/sprints/{sprintId:guid}/notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> SetSprintNotes(Guid id, Guid sprintId, [FromBody] SprintNotesDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.SetSprintNotesAsync(id, sprintId, dto, cancellationToken);
            return NoContent();
        });

    /// <summary>Sets the priority order of the quarter's options.</summary>
    [HttpPut("quarters/{id:guid}/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> Reorder(Guid id, [FromBody] ReorderInitiativesDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.ReorderInitiativesAsync(id, dto, cancellationToken);
            return NoContent();
        });

    /// <summary>Adds a standing lane (Maintenance or Tech roadmap) the quarter is missing.</summary>
    [HttpPost("quarters/{id:guid}/lanes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> AddLane(Guid id, [FromBody] AddBucketDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.AddBucketAsync(id, dto, cancellationToken) }));

    #endregion

    #region Initiatives

    [HttpPost("initiatives")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> CreateInitiative([FromBody] CreateInitiativeDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.CreateInitiativeAsync(dto, cancellationToken) }));

    [HttpPut("initiatives/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> UpdateInitiative(Guid id, [FromBody] UpdateInitiativeDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.UpdateInitiativeAsync(id, dto, cancellationToken);
            return NoContent();
        });

    [HttpPut("initiatives/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> ChangeStatus(Guid id, [FromBody] ChangeInitiativeStatusDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.ChangeInitiativeStatusAsync(id, dto, cancellationToken);
            return NoContent();
        });

    /// <summary>Copies an option to the next quarter as a leftover.</summary>
    [HttpPost("initiatives/{id:guid}/carry-over")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> CarryOver(Guid id, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.CarryOverInitiativeAsync(id, cancellationToken) }));

    [HttpDelete("initiatives/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> DeleteInitiative(Guid id, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.DeleteInitiativeAsync(id, cancellationToken);
            return NoContent();
        });

    [HttpPost("initiatives/{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> AddMember(Guid id, [FromBody] CreateInitiativeMemberDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.AddInitiativeMemberAsync(id, dto, cancellationToken) }));

    [HttpPut("members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> UpdateMember(Guid memberId, [FromBody] UpdateInitiativeMemberDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.UpdateInitiativeMemberAsync(memberId, dto, cancellationToken);
            return NoContent();
        });

    [HttpDelete("members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> RemoveMember(Guid memberId, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.RemoveInitiativeMemberAsync(memberId, cancellationToken);
            return NoContent();
        });

    #endregion

    #region Dependencies

    [HttpPost("initiatives/{id:guid}/external-dependencies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> AddExternalDependency(Guid id, [FromBody] SaveExternalDependencyDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.AddExternalDependencyAsync(id, dto, cancellationToken) }));

    [HttpPut("external-dependencies/{dependencyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> UpdateExternalDependency(Guid dependencyId, [FromBody] SaveExternalDependencyDto dto, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.UpdateExternalDependencyAsync(dependencyId, dto, cancellationToken);
            return NoContent();
        });

    [HttpDelete("external-dependencies/{dependencyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> DeleteExternalDependency(Guid dependencyId, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.DeleteExternalDependencyAsync(dependencyId, cancellationToken);
            return NoContent();
        });

    [HttpPost("dependencies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public Task<ActionResult> CreateDependency([FromBody] CreateInitiativeDependencyDto dto, CancellationToken cancellationToken) =>
        Handle(async () => Created(string.Empty, new { id = await _planningService.CreateDependencyAsync(dto, cancellationToken) }));

    [HttpDelete("dependencies/{dependencyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<ActionResult> DeleteDependency(Guid dependencyId, CancellationToken cancellationToken) =>
        Handle(async () =>
        {
            await _planningService.DeleteDependencyAsync(dependencyId, cancellationToken);
            return NoContent();
        });

    #endregion

    /// <summary>Maps the service's exceptions to HTTP status codes.</summary>
    private async Task<ActionResult> Handle(Func<Task<ActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ValidationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
