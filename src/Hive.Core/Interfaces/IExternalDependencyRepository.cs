using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for ExternalDependency entity.
/// </summary>
public interface IExternalDependencyRepository
{
    Task<IReadOnlyList<ExternalDependency>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExternalDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalDependency>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<ExternalDependency> AddAsync(ExternalDependency entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExternalDependency entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
