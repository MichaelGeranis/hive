using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Api.Controllers;

/// <summary>
/// API Controller for backup and restore operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _service;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService service, ILogger<BackupController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Exports all data as a backup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete backup of all data.</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(BackupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BackupDto>> Export(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exporting backup");

        var backup = await _service.ExportAsync(cancellationToken);
        return Ok(backup);
    }

    /// <summary>
    /// Imports data from a backup.
    /// </summary>
    /// <param name="backup">The backup data to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the restore operation.</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(RestoreResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestoreResultDto>> Import([FromBody] BackupDto backup, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Importing backup from {Date}", backup.ExportedAt);

        if (backup == null)
        {
            return BadRequest(new { message = "Backup data is required" });
        }

        var result = await _service.ImportAsync(backup, false, cancellationToken);
        return Ok(result);
    }
}
