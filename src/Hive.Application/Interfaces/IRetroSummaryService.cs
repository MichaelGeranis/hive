using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// The AI summary of the team's latest retrospectives.
/// </summary>
public interface IRetroSummaryService
{
    /// <summary>
    /// The retro summary, made again first when the retros changed since the last one (or
    /// always, with <paramref name="forceRefresh"/>). A model failure never throws: the last
    /// summary is returned with the error.
    /// </summary>
    Task<RetroSummaryDto> GetAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
