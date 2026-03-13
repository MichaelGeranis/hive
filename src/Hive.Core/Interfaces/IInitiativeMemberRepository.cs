using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for InitiativeMember entity.
/// </summary>
public interface IInitiativeMemberRepository
{
    Task<InitiativeMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeMember>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeMember>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<InitiativeMember> AddAsync(InitiativeMember member, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid initiativeId, Guid directReportId, CancellationToken cancellationToken = default);
}
