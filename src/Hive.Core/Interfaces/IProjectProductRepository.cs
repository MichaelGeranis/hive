using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for the links between projects and the products they serve.
/// </summary>
public interface IProjectProductRepository
{
    Task<IReadOnlyList<ProjectProduct>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectProduct>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectProduct>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProjectProduct> AddAsync(ProjectProduct link, CancellationToken cancellationToken = default);
    Task DeleteByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task DeleteByProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
