using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// What a quarter's plan adds up to: the board with capacity, waterline, mix and insights; a
/// draft of the grid; what-if scenarios; the commitment and the review against it.
/// </summary>
public interface IPlanningBoardService
{
    Task<PlanningBoardDto> GetBoardAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<PlanDraftDto> DraftAsync(Guid quarterId, bool apply, CancellationToken cancellationToken = default);
    Task<ScenarioDto> RunScenarioAsync(Guid quarterId, ScenarioRequestDto request, CancellationToken cancellationToken = default);
    Task<QuarterDto> CommitAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<PlanReviewDto> GetReviewAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<CalibrationDto> GetCalibrationAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportToExcelAsync(Guid quarterId, CancellationToken cancellationToken = default);
}
