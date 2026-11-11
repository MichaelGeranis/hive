using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Changes to a quarter's plan: quarters, initiatives and what hangs off them, the roster and
/// the allocation grid. What the plan adds up to is read from <see cref="IPlanningBoardService"/>.
/// </summary>
public interface IQuarterlyPlanningService
{
    // Quarters
    Task<IReadOnlyList<QuarterDto>> GetAllQuartersAsync(CancellationToken cancellationToken = default);
    Task<QuarterDto?> GetQuarterByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuarterDto> CreateQuarterAsync(CreateQuarterDto dto, CancellationToken cancellationToken = default);
    Task<QuarterDto> UpdateQuarterAsync(Guid id, UpdateQuarterDto dto, CancellationToken cancellationToken = default);
    Task<QuarterDto> ActivateQuarterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuarterDto> CompleteQuarterAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteQuarterAsync(Guid id, CancellationToken cancellationToken = default);

    // Initiatives
    Task<Guid> CreateInitiativeAsync(CreateInitiativeDto dto, CancellationToken cancellationToken = default);
    Task<Guid> AddBucketAsync(Guid quarterId, AddBucketDto dto, CancellationToken cancellationToken = default);
    Task UpdateInitiativeAsync(Guid id, UpdateInitiativeDto dto, CancellationToken cancellationToken = default);
    Task ChangeInitiativeStatusAsync(Guid id, ChangeInitiativeStatusDto dto, CancellationToken cancellationToken = default);
    Task ReorderInitiativesAsync(Guid quarterId, ReorderInitiativesDto dto, CancellationToken cancellationToken = default);
    Task<Guid> CarryOverInitiativeAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteInitiativeAsync(Guid id, CancellationToken cancellationToken = default);

    // People on initiatives
    Task<Guid> AddInitiativeMemberAsync(Guid initiativeId, CreateInitiativeMemberDto dto, CancellationToken cancellationToken = default);
    Task UpdateInitiativeMemberAsync(Guid memberId, UpdateInitiativeMemberDto dto, CancellationToken cancellationToken = default);
    Task RemoveInitiativeMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

    // Dependencies
    Task<Guid> AddExternalDependencyAsync(Guid initiativeId, SaveExternalDependencyDto dto, CancellationToken cancellationToken = default);
    Task UpdateExternalDependencyAsync(Guid id, SaveExternalDependencyDto dto, CancellationToken cancellationToken = default);
    Task DeleteExternalDependencyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateDependencyAsync(CreateInitiativeDependencyDto dto, CancellationToken cancellationToken = default);
    Task DeleteDependencyAsync(Guid id, CancellationToken cancellationToken = default);

    // Roster, grid and sprint notes
    Task SetRosterMemberAsync(Guid quarterId, SetRosterMemberDto dto, CancellationToken cancellationToken = default);
    Task RemoveRosterMemberAsync(Guid quarterId, Guid directReportId, CancellationToken cancellationToken = default);
    Task SetCellsAsync(Guid quarterId, SetCellsDto dto, CancellationToken cancellationToken = default);
    Task SetSprintNotesAsync(Guid quarterId, Guid sprintId, SprintNotesDto dto, CancellationToken cancellationToken = default);
}
