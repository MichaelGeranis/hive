using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for managing application settings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly IAppSettingsService _service;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IAppSettingsService service, ILogger<SettingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets application settings.
    /// </summary>
    /// <returns>The current application settings.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AppSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppSettingsDto>> Get(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting application settings");
        var settings = await _service.GetAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// Updates application settings.
    /// </summary>
    /// <param name="dto">The updated settings data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(AppSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppSettingsDto>> Update([FromBody] UpdateAppSettingsDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating application settings");

        try
        {
            var updated = await _service.UpdateAsync(dto, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
