using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for InitiativeDependency entity.
/// </summary>
public interface IInitiativeDependencyRepository
{
    Task<InitiativeDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeDependency>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeDependency>> GetByDependentInitiativeAsync(Guid dependentInitiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeDependency>> GetByDependencyInitiativeAsync(Guid dependencyInitiativeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InitiativeDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<InitiativeDependency> AddAsync(InitiativeDependency dependency, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid dependentInitiativeId, Guid dependencyInitiativeId, CancellationToken cancellationToken = default);
}
