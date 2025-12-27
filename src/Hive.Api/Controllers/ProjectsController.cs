using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing projects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService service, ILogger<ProjectsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <returns>List of all projects.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all projects");
        var projects = await _service.GetAllAsync(cancellationToken);
        return Ok(projects);
    }

    /// <summary>
    /// Gets a project by ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project with ID: {Id}", id);
        var project = await _service.GetByIdAsync(id, cancellationToken);

        if (project is null)
        {
            return NotFound(new { message = $"Project with ID '{id}' not found." });
        }

        return Ok(project);
    }

    /// <summary>
    /// Gets projects by status.
    /// </summary>
    /// <param name="status">The project status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of projects with the specified status.</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetByStatus(ProjectStatus status, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting projects with status: {Status}", status);
        var projects = await _service.GetByStatusAsync(status, cancellationToken);
        return Ok(projects);
    }

    /// <summary>
    /// Gets all active projects (Planning or Active status).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active projects.</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetActive(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active projects");
        var projects = await _service.GetActiveAsync(cancellationToken);
        return Ok(projects);
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="dto">The project data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created project.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new project: {Name}", dto.Name);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="dto">The updated project data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating project with ID: {Id}", id);

        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
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
    /// Activates a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating project with ID: {Id}", id);

        try
        {
            var updated = await _service.ActivateAsync(id, cancellationToken);
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

    /// <summary>
    /// Puts a project on hold.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPost("{id:guid}/hold")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> PutOnHold(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Putting project on hold with ID: {Id}", id);

        try
        {
            var updated = await _service.PutOnHoldAsync(id, cancellationToken);
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

    /// <summary>
    /// Completes a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing project with ID: {Id}", id);

        try
        {
            var updated = await _service.CompleteAsync(id, cancellationToken);
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

    /// <summary>
    /// Reopens a completed or cancelled project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPost("{id:guid}/reopen")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Reopen(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reopening project with ID: {Id}", id);

        try
        {
            var updated = await _service.ReopenAsync(id, cancellationToken);
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

    /// <summary>
    /// Cancels a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling project with ID: {Id}", id);

        try
        {
            var updated = await _service.CancelAsync(id, cancellationToken);
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

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting project with ID: {Id}", id);

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
}
