using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IParentRepository.
/// </summary>
public class ParentRepository : IParentRepository
{
    private readonly InMemoryDbContext _context;

    public ParentRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Parents.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Parent?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = _context.Parents.Values
            .FirstOrDefault(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Parents.Values
            .OrderBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Parent>>(entities);
    }

    public Task<IReadOnlyList<Parent>> GetByMatchingLabelsAsync(IEnumerable<string> labels, CancellationToken cancellationToken = default)
    {
        var labelSet = labels.Select(l => l.Trim().ToLowerInvariant()).ToHashSet();
        var entities = _context.Parents.Values
            .Where(p => !string.IsNullOrEmpty(p.Labels) &&
                        p.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Any(pl => labelSet.Contains(pl.Trim().ToLowerInvariant())))
            .OrderBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Parent>>(entities);
    }

    public Task<Parent> AddAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        if (!_context.Parents.TryAdd(parent.Id, parent))
        {
            throw new InvalidOperationException($"Parent with id '{parent.Id}' already exists.");
        }
        return Task.FromResult(parent);
    }

    public Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        if (!_context.Parents.ContainsKey(parent.Id))
        {
            throw new InvalidOperationException($"Parent with id '{parent.Id}' not found.");
        }
        _context.Parents[parent.Id] = parent;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Parents.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var exists = _context.Parents.Values
            .Any(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
