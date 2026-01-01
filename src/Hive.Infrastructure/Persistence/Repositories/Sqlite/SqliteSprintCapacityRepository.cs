using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSprintCapacityRepository : ISprintCapacityRepository
{
    private readonly HiveDbContext _context;

    public SqliteSprintCapacityRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SprintCapacity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SprintCapacities.FindAsync([id], cancellationToken);
    }

    public async Task<SprintCapacity?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _context.SprintCapacities
            .FirstOrDefaultAsync(x => x.SprintId == sprintId, cancellationToken);
    }

    public async Task<IReadOnlyList<SprintCapacity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SprintCapacities
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SprintCapacity>> GetBySprintIdsAsync(IEnumerable<Guid> sprintIds, CancellationToken cancellationToken = default)
    {
        var idsList = sprintIds.ToList();
        return await _context.SprintCapacities
            .Where(x => idsList.Contains(x.SprintId))
            .ToListAsync(cancellationToken);
    }

    public async Task<SprintCapacity> AddAsync(SprintCapacity capacity, CancellationToken cancellationToken = default)
    {
        _context.SprintCapacities.Add(capacity);
        await _context.SaveChangesAsync(cancellationToken);
        return capacity;
    }

    public async Task UpdateAsync(SprintCapacity capacity, CancellationToken cancellationToken = default)
    {
        _context.SprintCapacities.Update(capacity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SprintCapacities.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.SprintCapacities.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _context.SprintCapacities.AnyAsync(x => x.SprintId == sprintId, cancellationToken);
    }
}
