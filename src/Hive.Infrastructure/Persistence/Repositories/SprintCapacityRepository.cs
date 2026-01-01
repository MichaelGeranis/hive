using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISprintCapacityRepository.
/// </summary>
public class SprintCapacityRepository : ISprintCapacityRepository
{
    private readonly InMemoryDbContext _context;

    public SprintCapacityRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SprintCapacity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SprintCapacities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<SprintCapacity?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var entity = _context.SprintCapacities.Values
            .FirstOrDefault(x => x.SprintId == sprintId);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<SprintCapacity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.SprintCapacities.Values
            .OrderBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<SprintCapacity>>(entities);
    }

    public Task<IReadOnlyList<SprintCapacity>> GetBySprintIdsAsync(IEnumerable<Guid> sprintIds, CancellationToken cancellationToken = default)
    {
        var sprintIdSet = sprintIds.ToHashSet();
        var entities = _context.SprintCapacities.Values
            .Where(x => sprintIdSet.Contains(x.SprintId))
            .ToList();
        return Task.FromResult<IReadOnlyList<SprintCapacity>>(entities);
    }

    public Task<SprintCapacity> AddAsync(SprintCapacity capacity, CancellationToken cancellationToken = default)
    {
        if (!_context.SprintCapacities.TryAdd(capacity.Id, capacity))
        {
            throw new InvalidOperationException($"SprintCapacity with id '{capacity.Id}' already exists.");
        }
        return Task.FromResult(capacity);
    }

    public Task UpdateAsync(SprintCapacity capacity, CancellationToken cancellationToken = default)
    {
        if (!_context.SprintCapacities.ContainsKey(capacity.Id))
        {
            throw new InvalidOperationException($"SprintCapacity with id '{capacity.Id}' not found.");
        }
        _context.SprintCapacities[capacity.Id] = capacity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SprintCapacities.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var exists = _context.SprintCapacities.Values
            .Any(x => x.SprintId == sprintId);
        return Task.FromResult(exists);
    }
}
