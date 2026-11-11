using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteInitiativeProductRepository : IInitiativeProductRepository
{
    private readonly HiveDbContext _context;

    public SqliteInitiativeProductRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<InitiativeProduct>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeProducts
            .AsNoTracking()
            .Where(x => x.InitiativeId == initiativeId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeProduct>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = await _context.Initiatives
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return await _context.InitiativeProducts
            .AsNoTracking()
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.InitiativeProducts
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);

        _context.InitiativeProducts.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<InitiativeProduct> AddAsync(InitiativeProduct entity, CancellationToken cancellationToken = default)
    {
        _context.InitiativeProducts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.InitiativeProducts.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.InitiativeProducts.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<InitiativeProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeProducts.AsNoTracking().OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
