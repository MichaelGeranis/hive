using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ChecklistInstancesController : ControllerBase
{
    private readonly IChecklistService _service;
    private readonly ILogger<ChecklistInstancesController> _logger;

    public ChecklistInstancesController(IChecklistService service, ILogger<ChecklistInstancesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============== Instances ==============

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChecklistInstanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistInstanceDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all checklist instances");
        var instances = await _service.GetAllInstancesAsync(cancellationToken);
        return Ok(instances);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting checklist instance: {Id}", id);
        var instance = await _service.GetInstanceByIdAsync(id, cancellationToken);

        if (instance is null)
        {
            return NotFound(new { message = $"Checklist instance with ID '{id}' not found." });
        }

        return Ok(instance);
    }

    [HttpGet("{id:guid}/with-items")]
    [ProducesResponseType(typeof(ChecklistInstanceWithItemsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceWithItemsDto>> GetWithItems(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting checklist instance with items: {Id}", id);
        var instance = await _service.GetInstanceWithItemsAsync(id, cancellationToken);

        if (instance is null)
        {
            return NotFound(new { message = $"Checklist instance with ID '{id}' not found." });
        }

        return Ok(instance);
    }

    [HttpGet("by-type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<ChecklistInstanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistInstanceDto>>> GetByType(
        ChecklistType type,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting checklist instances by type: {Type}", type);
        var instances = await _service.GetInstancesByTypeAsync(type, cancellationToken);
        return Ok(instances);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<ChecklistInstanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistInstanceDto>>> GetActive(
        [FromQuery] ChecklistType? type = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting active checklist instances (type: {Type})", type);
        var instances = await _service.GetActiveInstancesAsync(type, cancellationToken);
        return Ok(instances);
    }

    [HttpPost("interview")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> CreateInterview(
        [FromBody] CreateInterviewInstanceDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating interview instance for candidate: {CandidateName}", dto.CandidateName);

        try
        {
            var created = await _service.CreateInterviewInstanceAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("onboarding")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> CreateOnboarding(
        [FromBody] CreateOnboardingInstanceDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating onboarding instance for: {NewHireName}", dto.NewHireName);

        try
        {
            var created = await _service.CreateOnboardingInstanceAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting checklist instance: {Id}", id);

        try
        {
            var updated = await _service.StartInstanceAsync(id, cancellationToken);
            return Ok(updated);
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

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing checklist instance: {Id}", id);

        try
        {
            var updated = await _service.CompleteInstanceAsync(id, cancellationToken);
            return Ok(updated);
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

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling checklist instance: {Id}", id);

        try
        {
            var updated = await _service.CancelInstanceAsync(id, cancellationToken);
            return Ok(updated);
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

    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(typeof(ChecklistInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceDto>> UpdateNotes(
        Guid id,
        [FromBody] string notes,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating notes for checklist instance: {Id}", id);

        try
        {
            var updated = await _service.UpdateInstanceNotesAsync(id, notes, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting checklist instance: {Id}", id);

        try
        {
            await _service.DeleteInstanceAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============== Instance Items ==============

    [HttpPost("items/{itemId:guid}/complete")]
    [ProducesResponseType(typeof(ChecklistInstanceItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceItemDto>> CompleteItem(
        Guid itemId,
        [FromBody] CompleteChecklistItemDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing checklist item: {ItemId}", itemId);

        try
        {
            var updated = await _service.CompleteItemAsync(itemId, dto, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("items/{itemId:guid}/skip")]
    [ProducesResponseType(typeof(ChecklistInstanceItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceItemDto>> SkipItem(
        Guid itemId,
        [FromBody] SkipChecklistItemDto? dto = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Skipping checklist item: {ItemId}", itemId);

        try
        {
            var updated = await _service.SkipItemAsync(itemId, dto?.Notes, cancellationToken);
            return Ok(updated);
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

    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(ChecklistInstanceItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistInstanceItemDto>> UpdateItem(
        Guid itemId,
        [FromBody] UpdateChecklistItemDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating checklist item: {ItemId}", itemId);

        try
        {
            var updated = await _service.UpdateItemAsync(itemId, dto, cancellationToken);
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

    [HttpGet("items/overdue")]
    [ProducesResponseType(typeof(IEnumerable<ChecklistInstanceItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistInstanceItemDto>>> GetOverdueItems(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting overdue checklist items");
        var items = await _service.GetOverdueItemsAsync(cancellationToken);
        return Ok(items);
    }
}
