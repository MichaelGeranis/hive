using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSprintGoalRepository : ISprintGoalRepository
{
    private readonly HiveDbContext _context;

    public SqliteSprintGoalRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SprintGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SprintGoals.FindAsync([id], cancellationToken);
    }

    public async Task<SprintGoal?> GetByQuarterSprintAsync(Guid quarterId, Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _context.SprintGoals
            .FirstOrDefaultAsync(x => x.QuarterId == quarterId && x.SprintId == sprintId, cancellationToken);
    }

    public async Task<IReadOnlyList<SprintGoal>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        return await _context.SprintGoals
            .Where(x => x.QuarterId == quarterId)
            .ToListAsync(cancellationToken);
    }

    public async Task<SprintGoal> AddAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default)
    {
        _context.SprintGoals.Add(sprintGoal);
        await _context.SaveChangesAsync(cancellationToken);
        return sprintGoal;
    }

    public async Task UpdateAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default)
    {
        _context.SprintGoals.Update(sprintGoal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SprintGoals.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.SprintGoals.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
