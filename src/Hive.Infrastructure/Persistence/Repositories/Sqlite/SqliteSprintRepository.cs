using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSprintRepository : ISprintRepository
{
    private readonly HiveDbContext _context;

    public SqliteSprintRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints.FindAsync([id], cancellationToken);
    }

    public async Task<Sprint?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .FirstOrDefaultAsync(x => x.Name == name.Trim(), cancellationToken);
    }

    public async Task<IReadOnlyList<Sprint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Quarter)
            .ThenByDescending(x => x.SprintNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sprint>> GetByTeamAsync(string teamName, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .Where(x => x.TeamName == teamName.Trim())
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Quarter)
            .ThenByDescending(x => x.SprintNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sprint>> GetByYearQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .Where(x => x.Year == year && x.Quarter == quarter)
            .OrderBy(x => x.SprintNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sprint>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints
            .Where(x => x.Year == year)
            .OrderBy(x => x.Quarter)
            .ThenBy(x => x.SprintNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Sprint> AddAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        _context.Sprints.Add(sprint);
        await _context.SaveChangesAsync(cancellationToken);
        return sprint;
    }

    public async Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        _context.Sprints.Update(sprint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Sprints.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Sprints.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Sprints.AnyAsync(x => x.Name == name.Trim(), cancellationToken);
    }
}
