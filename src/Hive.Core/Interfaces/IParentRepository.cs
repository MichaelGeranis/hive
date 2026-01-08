using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Parent entity.
/// </summary>
public interface IParentRepository
{
    Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Parent?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Parent>> GetByMatchingLabelsAsync(IEnumerable<string> labels, CancellationToken cancellationToken = default);
    Task<Parent> AddAsync(Parent parent, CancellationToken cancellationToken = default);
    Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
