using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for quarterly planning operations.
/// </summary>
public interface IQuarterlyPlanningService
{
    // Quarter operations
    Task<QuarterDto?> GetQuarterByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuarterDto?> GetQuarterByYearQuarterAsync(int year, int quarterNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuarterDto>> GetAllQuartersAsync(CancellationToken cancellationToken = default);
    Task<QuarterDto?> GetActiveQuarterAsync(CancellationToken cancellationToken = default);
    Task<QuarterDto> CreateQuarterAsync(CreateQuarterDto dto, CancellationToken cancellationToken = default);
    Task<QuarterDto> UpdateQuarterAsync(Guid id, UpdateQuarterDto dto, CancellationToken cancellationToken = default);
    Task<QuarterDto> ActivateQuarterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuarterDto> CompleteQuarterAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteQuarterAsync(Guid id, CancellationToken cancellationToken = default);

    // Initiative operations
    Task<InitiativeDto?> GetInitiativeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeDto>> GetInitiativesByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<InitiativeDto> CreateInitiativeAsync(CreateInitiativeDto dto, CancellationToken cancellationToken = default);
    Task<InitiativeDto> UpdateInitiativeAsync(Guid id, UpdateInitiativeDto dto, CancellationToken cancellationToken = default);
    Task DeleteInitiativeAsync(Guid id, CancellationToken cancellationToken = default);

    // Allocation operations
    Task<AllocationDto?> GetAllocationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationDto>> GetAllocationsByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<AllocationDto> CreateAllocationAsync(CreateAllocationDto dto, CancellationToken cancellationToken = default);
    Task DeleteAllocationAsync(Guid id, CancellationToken cancellationToken = default);

    // Sprint Goal operations
    Task<IReadOnlyList<SprintGoalDto>> GetSprintGoalsByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<SprintGoalDto> UpsertSprintGoalAsync(UpsertSprintGoalDto dto, CancellationToken cancellationToken = default);

    // Dependency operations
    Task<IReadOnlyList<InitiativeDependencyDto>> GetDependenciesByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<InitiativeDependencyDto> CreateDependencyAsync(CreateInitiativeDependencyDto dto, CancellationToken cancellationToken = default);
    Task DeleteDependencyAsync(Guid id, CancellationToken cancellationToken = default);

    // Board operations
    Task<PlanningBoardDto> GetPlanningBoardAsync(Guid quarterId, CancellationToken cancellationToken = default);
}
