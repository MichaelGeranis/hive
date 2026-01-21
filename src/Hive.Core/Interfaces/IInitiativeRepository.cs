using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Initiative entity.
/// </summary>
public interface IInitiativeRepository
{
    Task<Initiative?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Initiative>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Initiative>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Initiative>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Initiative>> GetByStatusAsync(InitiativeStatus status, CancellationToken cancellationToken = default);
    Task<Initiative> AddAsync(Initiative initiative, CancellationToken cancellationToken = default);
    Task UpdateAsync(Initiative initiative, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
