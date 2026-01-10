using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

public class ChecklistInstanceRepository : IChecklistInstanceRepository
{
    private readonly InMemoryDbContext _context;

    public ChecklistInstanceRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ChecklistInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstances.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<ChecklistInstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstances.Values
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstance>>(entities);
    }

    public Task<IReadOnlyList<ChecklistInstance>> GetByTypeAsync(ChecklistType type, CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstances.Values
            .Where(x => x.Type == type)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstance>>(entities);
    }

    public Task<IReadOnlyList<ChecklistInstance>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistInstances.Values
            .Where(x => x.TemplateId == templateId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstance>>(entities);
    }

    public Task<IReadOnlyList<ChecklistInstance>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistInstances.Values
            .Where(x => x.Status == ChecklistInstanceStatus.NotStarted
                        || x.Status == ChecklistInstanceStatus.InProgress);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        var entities = query
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistInstance>>(entities);
    }

    public Task<ChecklistInstance> AddAsync(ChecklistInstance instance, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistInstances.TryAdd(instance.Id, instance))
        {
            throw new InvalidOperationException($"ChecklistInstance with id '{instance.Id}' already exists.");
        }
        return Task.FromResult(instance);
    }

    public Task UpdateAsync(ChecklistInstance instance, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistInstances.ContainsKey(instance.Id))
        {
            throw new InvalidOperationException($"ChecklistInstance with id '{instance.Id}' not found.");
        }
        _context.ChecklistInstances[instance.Id] = instance;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistInstances.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ChecklistInstances.ContainsKey(id));
    }
}
