using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteParentRepository : IParentRepository
{
    private readonly HiveDbContext _context;

    public SqliteParentRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Parents.FindAsync([id], cancellationToken);
    }

    public async Task<Parent?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name.Trim(), cancellationToken);
    }

    public async Task<Parent?> GetByJiraKeyAsync(string jiraKey, CancellationToken cancellationToken = default)
    {
        // Keys are stored upper-case by the entity
        var key = jiraKey.Trim().ToUpperInvariant();
        return await _context.Parents.AsNoTracking().FirstOrDefaultAsync(x => x.JiraKey == key, cancellationToken);
    }

    public async Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Parents
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Parent> AddAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        _context.Parents.Add(parent);
        await _context.SaveChangesAsync(cancellationToken);
        return parent;
    }

    public async Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        _context.Parents.UpdateUntracked(parent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Parents.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Parents.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Parents.AnyAsync(x => x.Name == name.Trim(), cancellationToken);
    }
}
