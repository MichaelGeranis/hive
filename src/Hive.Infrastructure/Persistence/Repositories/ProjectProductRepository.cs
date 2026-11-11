using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IProjectProductRepository.
/// </summary>
public class ProjectProductRepository : IProjectProductRepository
{
    private readonly InMemoryDbContext _context;

    public ProjectProductRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<IReadOnlyList<ProjectProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ProjectProduct>>(_context.ProjectProducts.Values.ToList());
    }

    public Task<IReadOnlyList<ProjectProduct>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ProjectProducts.Values
            .Where(x => x.ProjectId == projectId)
            .ToList();
        return Task.FromResult<IReadOnlyList<ProjectProduct>>(entities);
    }

    public Task<IReadOnlyList<ProjectProduct>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ProjectProducts.Values
            .Where(x => x.ProductId == productId)
            .ToList();
        return Task.FromResult<IReadOnlyList<ProjectProduct>>(entities);
    }

    public Task<ProjectProduct> AddAsync(ProjectProduct link, CancellationToken cancellationToken = default)
    {
        if (_context.ProjectProducts.Values.Any(x => x.ProjectId == link.ProjectId && x.ProductId == link.ProductId))
        {
            throw new InvalidOperationException("The project already serves this product.");
        }

        if (!_context.ProjectProducts.TryAdd(link.Id, link))
        {
            throw new InvalidOperationException($"ProjectProduct with id '{link.Id}' already exists.");
        }
        return Task.FromResult(link);
    }

    public Task DeleteByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        RemoveWhere(x => x.ProjectId == projectId);
        return Task.CompletedTask;
    }

    public Task DeleteByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        RemoveWhere(x => x.ProductId == productId);
        return Task.CompletedTask;
    }

    private void RemoveWhere(Func<ProjectProduct, bool> predicate)
    {
        foreach (var id in _context.ProjectProducts.Values.Where(predicate).Select(x => x.Id).ToList())
        {
            _context.ProjectProducts.TryRemove(id, out _);
        }
    }
}
