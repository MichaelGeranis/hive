using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Sprint entity.
/// </summary>
public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sprint?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByTeamAsync(string teamName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByYearQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<Sprint> AddAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
