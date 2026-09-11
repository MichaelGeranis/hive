using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for 1:1 meetings. Each 1:1 is a single markdown note whose person and
/// date are derived from its tags.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OneOnOneMeetingsController : ControllerBase
{
    private readonly IOneOnOneMeetingService _service;
    private readonly ILogger<OneOnOneMeetingsController> _logger;

    public OneOnOneMeetingsController(IOneOnOneMeetingService service, ILogger<OneOnOneMeetingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Lists 1:1s newest first.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100).</param>
    /// <param name="directReportId">Limit to the 1:1s held with one person.</param>
    /// <param name="unlinked">Limit to 1:1s whose tags name nobody Hive recognises.</param>
    /// <param name="search">Search term matched against title, content and tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OneOnOneMeetingDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? directReportId = null,
        [FromQuery] bool unlinked = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting 1:1s page {PageNumber}, report: {DirectReportId}, unlinked: {Unlinked}",
            pageNumber, directReportId, unlinked);

        var pagination = new MeetingPaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            DirectReportId = directReportId,
            UnlinkedOnly = unlinked,
            SearchTerm = search
        };

        var meetings = await _service.GetFilteredPagedAsync(pagination, cancellationToken);
        return Ok(meetings);
    }

    /// <summary>
    /// Gets the total number of 1:1s logged.
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetCount(CancellationToken cancellationToken)
    {
        var count = await _service.GetTotalCountAsync(cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Gets how many 1:1s have been held with each person.
    /// </summary>
    [HttpGet("counts")]
    [ProducesResponseType(typeof(IEnumerable<MeetingCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingCountDto>>> GetCounts(CancellationToken cancellationToken)
    {
        var counts = await _service.GetCountsAsync(cancellationToken);
        return Ok(counts);
    }

    /// <summary>
    /// Gets every 1:1 held with one person.
    /// </summary>
    [HttpGet("by-report/{directReportId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<OneOnOneMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OneOnOneMeetingDto>>> GetByDirectReport(
        Guid directReportId,
        CancellationToken cancellationToken)
    {
        var meetings = await _service.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return Ok(meetings);
    }

    /// <summary>
    /// Gets a 1:1 by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await _service.GetByIdAsync(id, cancellationToken);

        if (meeting is null)
        {
            return NotFound(new { message = $"1:1 with ID '{id}' not found." });
        }

        return Ok(meeting);
    }

    /// <summary>
    /// Starts a blank 1:1 note, ready to be written into during the meeting.
    /// </summary>
    [HttpPost("blank")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OneOnOneMeetingDto>> CreateBlank(
        [FromBody] CreateBlankMeetingDto? dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting a blank 1:1 note");
        var meeting = await _service.CreateBlankAsync(dto ?? new CreateBlankMeetingDto(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = meeting.Id }, meeting);
    }

    /// <summary>
    /// Saves the body of a 1:1. The title is derived from the first line of the content.
    /// </summary>
    [HttpPut("{id:guid}/content")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> UpdateContent(
        Guid id,
        [FromBody] UpdateMeetingContentDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var meeting = await _service.UpdateContentAsync(id, dto, cancellationToken);
            return Ok(meeting);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Replaces the tags of a 1:1, re-deriving who it is with and when it happened.
    /// </summary>
    [HttpPut("{id:guid}/tags")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> UpdateTags(
        Guid id,
        [FromBody] UpdateMeetingTagsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating tags on 1:1 {Id}", id);
            var meeting = await _service.UpdateTagsAsync(id, dto, cancellationToken);
            return Ok(meeting);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Moves a 1:1 to another day.
    /// </summary>
    [HttpPut("{id:guid}/date")]
    [ProducesResponseType(typeof(OneOnOneMeetingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OneOnOneMeetingDto>> UpdateDate(
        Guid id,
        [FromBody] UpdateMeetingDateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var meeting = await _service.UpdateDateAsync(id, dto, cancellationToken);
            return Ok(meeting);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a 1:1.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting 1:1 {Id}", id);
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
