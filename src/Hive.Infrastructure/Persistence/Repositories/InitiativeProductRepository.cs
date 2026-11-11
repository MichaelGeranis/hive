using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IInitiativeProductRepository.
/// </summary>
public class InitiativeProductRepository : IInitiativeProductRepository
{
    private readonly InMemoryDbContext _context;

    public InitiativeProductRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<IReadOnlyList<InitiativeProduct>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeProducts.Values
            .Where(x => x.InitiativeId == initiativeId)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeProduct>>(entities);
    }

    public Task<IReadOnlyList<InitiativeProduct>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = _context.Initiatives.Values
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToHashSet();

        var entities = _context.InitiativeProducts.Values
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .OrderBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeProduct>>(entities);
    }

    public Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        foreach (var id in _context.InitiativeProducts.Values.Where(x => x.InitiativeId == initiativeId).Select(x => x.Id).ToList())
        {
            _context.InitiativeProducts.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<InitiativeProduct> AddAsync(InitiativeProduct entity, CancellationToken cancellationToken = default)
    {
        if (!_context.InitiativeProducts.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"InitiativeProduct with id '{entity.Id}' already exists.");
        }
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.InitiativeProducts.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InitiativeProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InitiativeProduct> entities = _context.InitiativeProducts.Values.OrderBy(x => x.CreatedAt).ToList();
        return Task.FromResult(entities);
    }
}
