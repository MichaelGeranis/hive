using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IProductRepository.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly InMemoryDbContext _context;

    public ProductRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Products.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Products.Values
            .OrderBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Product>>(entities);
    }

    public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (!_context.Products.TryAdd(product.Id, product))
        {
            throw new InvalidOperationException($"Product with id '{product.Id}' already exists.");
        }
        return Task.FromResult(product);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (!_context.Products.ContainsKey(product.Id))
        {
            throw new InvalidOperationException($"Product with id '{product.Id}' not found.");
        }
        _context.Products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Products.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.Products.Values
            .Any(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
