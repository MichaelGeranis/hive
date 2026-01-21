using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for generating quarterly planning insights.
/// </summary>
public interface IQuarterlyPlanningInsightsService
{
    /// <summary>
    /// Generates insights for a quarter's planning data.
    /// </summary>
    Task<PlanningInsightsDto> GenerateInsightsAsync(Guid quarterId, CancellationToken cancellationToken = default);
}
