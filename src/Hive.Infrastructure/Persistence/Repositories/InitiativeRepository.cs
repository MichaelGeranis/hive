using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IInitiativeRepository.
/// </summary>
public class InitiativeRepository : IInitiativeRepository
{
    private readonly InMemoryDbContext _context;

    public InitiativeRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Initiative?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Initiatives.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Initiative>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Initiatives.Values
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Initiative>>(entities);
    }

    public Task<IReadOnlyList<Initiative>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var entities = _context.Initiatives.Values
            .Where(x => x.QuarterId == quarterId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Initiative>>(entities);
    }

    public Task<IReadOnlyList<Initiative>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = _context.Initiatives.Values
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Initiative>>(entities);
    }

    public Task<IReadOnlyList<Initiative>> GetByStatusAsync(InitiativeStatus status, CancellationToken cancellationToken = default)
    {
        var entities = _context.Initiatives.Values
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Initiative>>(entities);
    }

    public Task<Initiative> AddAsync(Initiative initiative, CancellationToken cancellationToken = default)
    {
        if (!_context.Initiatives.TryAdd(initiative.Id, initiative))
        {
            throw new InvalidOperationException($"Initiative with id '{initiative.Id}' already exists.");
        }
        return Task.FromResult(initiative);
    }

    public Task UpdateAsync(Initiative initiative, CancellationToken cancellationToken = default)
    {
        if (!_context.Initiatives.ContainsKey(initiative.Id))
        {
            throw new InvalidOperationException($"Initiative with id '{initiative.Id}' not found.");
        }
        _context.Initiatives[initiative.Id] = initiative;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Initiatives.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Initiatives.ContainsKey(id));
    }
}
