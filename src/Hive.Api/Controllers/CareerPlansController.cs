using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for direct reports' career plans and their skill levels.
/// </summary>
[ApiController]
[Route("api/career-plans")]
[Authorize]
[Produces("application/json")]
public class CareerPlansController : ControllerBase
{
    private readonly ICareerPlanService _service;
    private readonly ILogger<CareerPlansController> _logger;

    public CareerPlansController(ICareerPlanService service, ILogger<CareerPlansController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets every career plan, by direct report name. Summaries carry no scores.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CareerPlanSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CareerPlanSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting career plans");
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Gets what the career plans need from the manager, for the dashboard.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(CareerOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CareerOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetOverviewAsync(cancellationToken));
    }

    /// <summary>
    /// Gets a career plan with its skills, ladder reading and game.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareerPlanDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _service.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { message = $"Career plan with ID '{id}' not found." });
        }

        return Ok(plan);
    }

    /// <summary>
    /// Gets a direct report's career plan.
    /// </summary>
    [HttpGet("by-direct-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareerPlanDto>> GetByDirectReport(Guid directReportId, CancellationToken cancellationToken)
    {
        var plan = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { message = "This direct report has no career plan yet." });
        }

        return Ok(plan);
    }

    /// <summary>
    /// Makes a direct report's career plan from a roadmap.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CareerPlanDto>> Create([FromBody] CreateCareerPlanDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating career plan for direct report {DirectReportId}", dto.DirectReportId);
        var result = await Handle(() => _service.CreateAsync(dto, cancellationToken));
        return result.Result is OkObjectResult { Value: CareerPlanDto created }
            ? CreatedAtAction(nameof(GetById), new { id = created.Id }, created)
            : result;
    }

    /// <summary>
    /// Changes the stage of the person's current role, or the notes.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CareerPlanDto>> Update(Guid id, [FromBody] UpdateCareerPlanDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.UpdateAsync(id, dto, cancellationToken));

    /// <summary>
    /// Deletes a plan and its skills.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting career plan {Id}", id);
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rates a skill, returning the plan and what the rating unlocked.
    /// </summary>
    [HttpPut("{id:guid}/skills/{skillId:guid}/level")]
    [ProducesResponseType(typeof(SkillLevelChangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<SkillLevelChangeDto>> SetSkillLevel(Guid id, Guid skillId, [FromBody] SetSkillLevelDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.SetSkillLevelAsync(id, skillId, dto, cancellationToken));

    /// <summary>
    /// Puts a skill in or out of what the person is working on now.
    /// </summary>
    [HttpPut("{id:guid}/skills/{skillId:guid}/focus")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CareerPlanDto>> SetSkillFocus(Guid id, Guid skillId, [FromBody] SetSkillFocusDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.SetSkillFocusAsync(id, skillId, dto, cancellationToken));

    /// <summary>
    /// Adds a skill to this plan only.
    /// </summary>
    [HttpPost("{id:guid}/skills")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CareerPlanDto>> AddSkill(Guid id, [FromBody] AddCareerPlanSkillDto dto, CancellationToken cancellationToken) =>
        Handle(() => _service.AddSkillAsync(id, dto, cancellationToken));

    /// <summary>
    /// Removes a skill from a plan.
    /// </summary>
    [HttpDelete("{id:guid}/skills/{skillId:guid}")]
    [ProducesResponseType(typeof(CareerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CareerPlanDto>> DeleteSkill(Guid id, Guid skillId, CancellationToken cancellationToken) =>
        Handle(() => _service.DeleteSkillAsync(id, skillId, cancellationToken));

    private async Task<ActionResult<T>> Handle<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
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
}
