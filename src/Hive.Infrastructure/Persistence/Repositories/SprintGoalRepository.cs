using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISprintGoalRepository.
/// </summary>
public class SprintGoalRepository : ISprintGoalRepository
{
    private readonly InMemoryDbContext _context;

    public SprintGoalRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SprintGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SprintGoals.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<SprintGoal?> GetByQuarterSprintAsync(Guid quarterId, Guid sprintId, CancellationToken cancellationToken = default)
    {
        var entity = _context.SprintGoals.Values
            .FirstOrDefault(x => x.QuarterId == quarterId && x.SprintId == sprintId);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<SprintGoal>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var entities = _context.SprintGoals.Values
            .Where(x => x.QuarterId == quarterId)
            .ToList();
        return Task.FromResult<IReadOnlyList<SprintGoal>>(entities);
    }

    public Task<SprintGoal> AddAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default)
    {
        if (!_context.SprintGoals.TryAdd(sprintGoal.Id, sprintGoal))
        {
            throw new InvalidOperationException($"SprintGoal with id '{sprintGoal.Id}' already exists.");
        }
        return Task.FromResult(sprintGoal);
    }

    public Task UpdateAsync(SprintGoal sprintGoal, CancellationToken cancellationToken = default)
    {
        if (!_context.SprintGoals.ContainsKey(sprintGoal.Id))
        {
            throw new InvalidOperationException($"SprintGoal with id '{sprintGoal.Id}' not found.");
        }
        _context.SprintGoals[sprintGoal.Id] = sprintGoal;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SprintGoals.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
