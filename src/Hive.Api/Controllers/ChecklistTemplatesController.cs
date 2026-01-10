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
public class ChecklistTemplatesController : ControllerBase
{
    private readonly IChecklistService _service;
    private readonly ILogger<ChecklistTemplatesController> _logger;

    public ChecklistTemplatesController(IChecklistService service, ILogger<ChecklistTemplatesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============== Templates ==============

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChecklistTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all checklist templates");
        var templates = await _service.GetAllTemplatesAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChecklistTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting checklist template: {Id}", id);
        var template = await _service.GetTemplateByIdAsync(id, cancellationToken);

        if (template is null)
        {
            return NotFound(new { message = $"Checklist template with ID '{id}' not found." });
        }

        return Ok(template);
    }

    [HttpGet("{id:guid}/with-items")]
    [ProducesResponseType(typeof(ChecklistTemplateWithItemsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateWithItemsDto>> GetWithItems(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting checklist template with items: {Id}", id);
        var template = await _service.GetTemplateWithItemsAsync(id, cancellationToken);

        if (template is null)
        {
            return NotFound(new { message = $"Checklist template with ID '{id}' not found." });
        }

        return Ok(template);
    }

    [HttpGet("by-type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<ChecklistTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetByType(
        ChecklistType type,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting checklist templates by type: {Type}", type);
        var templates = await _service.GetTemplatesByTypeAsync(type, includeInactive, cancellationToken);
        return Ok(templates);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<ChecklistTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetActive(
        [FromQuery] ChecklistType? type = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting active checklist templates (type: {Type})", type);
        var templates = await _service.GetActiveTemplatesAsync(type, cancellationToken);
        return Ok(templates);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChecklistTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChecklistTemplateDto>> Create(
        [FromBody] CreateChecklistTemplateDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating checklist template: {Name}", dto.Name);

        try
        {
            var created = await _service.CreateTemplateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ChecklistTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateDto>> Update(
        Guid id,
        [FromBody] UpdateChecklistTemplateDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating checklist template: {Id}", id);

        try
        {
            var updated = await _service.UpdateTemplateAsync(id, dto, cancellationToken);
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting checklist template: {Id}", id);

        try
        {
            await _service.DeleteTemplateAsync(id, cancellationToken);
            return NoContent();
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

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ChecklistTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating checklist template: {Id}", id);

        try
        {
            var updated = await _service.ActivateTemplateAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ChecklistTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating checklist template: {Id}", id);

        try
        {
            var updated = await _service.DeactivateTemplateAsync(id, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ============== Template Items ==============

    [HttpPost("{templateId:guid}/items")]
    [ProducesResponseType(typeof(ChecklistTemplateItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateItemDto>> AddItem(
        Guid templateId,
        [FromBody] CreateChecklistTemplateItemDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding item to template: {TemplateId}", templateId);

        try
        {
            var created = await _service.AddTemplateItemAsync(templateId, dto, cancellationToken);
            return Created($"/api/checklisttemplates/items/{created.Id}", created);
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

    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(ChecklistTemplateItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistTemplateItemDto>> UpdateItem(
        Guid itemId,
        [FromBody] UpdateChecklistTemplateItemDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating template item: {ItemId}", itemId);

        try
        {
            var updated = await _service.UpdateTemplateItemAsync(itemId, dto, cancellationToken);
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

    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting template item: {ItemId}", itemId);

        try
        {
            await _service.DeleteTemplateItemAsync(itemId, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{templateId:guid}/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderItems(
        Guid templateId,
        [FromBody] ReorderItemsDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reordering items for template: {TemplateId}", templateId);

        try
        {
            await _service.ReorderTemplateItemsAsync(templateId, dto.ItemIds, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
