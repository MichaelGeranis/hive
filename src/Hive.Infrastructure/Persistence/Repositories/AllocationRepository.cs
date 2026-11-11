using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IAllocationRepository.
/// </summary>
public class AllocationRepository : IAllocationRepository
{
    private readonly InMemoryDbContext _context;

    public AllocationRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Allocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Allocations.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Allocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Allocations.Values.ToList();
        return Task.FromResult<IReadOnlyList<Allocation>>(entities);
    }

    public Task<IReadOnlyList<Allocation>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.Allocations.Values
            .Where(x => x.InitiativeId == initiativeId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Allocation>>(entities);
    }

    public Task<IReadOnlyList<Allocation>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.Allocations.Values
            .Where(x => x.DirectReportId == directReportId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Allocation>>(entities);
    }

    public Task<IReadOnlyList<Allocation>> GetBySprintAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var entities = _context.Allocations.Values
            .Where(x => x.SprintId == sprintId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Allocation>>(entities);
    }

    public Task<IReadOnlyList<Allocation>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        // Get all initiative IDs for this quarter
        var quarterInitiativeIds = _context.Initiatives.Values
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToHashSet();

        var entities = _context.Allocations.Values
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .ToList();
        return Task.FromResult<IReadOnlyList<Allocation>>(entities);
    }

    public Task<Allocation?> GetByInitiativeDirectReportSprintAsync(
        Guid initiativeId,
        Guid directReportId,
        Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        var entity = _context.Allocations.Values
            .FirstOrDefault(x =>
                x.InitiativeId == initiativeId &&
                x.DirectReportId == directReportId &&
                x.SprintId == sprintId);
        return Task.FromResult(entity);
    }

    public Task<Allocation> AddAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        if (!_context.Allocations.TryAdd(allocation.Id, allocation))
        {
            throw new InvalidOperationException($"Allocation with id '{allocation.Id}' already exists.");
        }
        return Task.FromResult(allocation);
    }

    public Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        if (!_context.Allocations.ContainsKey(allocation.Id))
        {
            throw new InvalidOperationException($"Allocation with id '{allocation.Id}' not found.");
        }
        _context.Allocations[allocation.Id] = allocation;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Allocations.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var toRemove = _context.Allocations.Values
            .Where(x => x.InitiativeId == initiativeId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _context.Allocations.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }
}
