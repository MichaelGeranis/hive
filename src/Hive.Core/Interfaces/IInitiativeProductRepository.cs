using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for InitiativeProduct entity.
/// </summary>
public interface IInitiativeProductRepository
{
    Task<IReadOnlyList<InitiativeProduct>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeProduct>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeProduct>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<InitiativeProduct> AddAsync(InitiativeProduct entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
}
