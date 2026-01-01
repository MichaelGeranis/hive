using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISprintRepository.
/// </summary>
public class SprintRepository : ISprintRepository
{
    private readonly InMemoryDbContext _context;

    public SprintRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Sprints.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Sprint?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = _context.Sprints.Values
            .FirstOrDefault(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Sprint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Sprints.Values
            .OrderByDescending(x => x.GetSortOrder())
            .ToList();
        return Task.FromResult<IReadOnlyList<Sprint>>(entities);
    }

    public Task<IReadOnlyList<Sprint>> GetByTeamAsync(string teamName, CancellationToken cancellationToken = default)
    {
        var entities = _context.Sprints.Values
            .Where(x => x.TeamName.Equals(teamName.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.GetSortOrder())
            .ToList();
        return Task.FromResult<IReadOnlyList<Sprint>>(entities);
    }

    public Task<IReadOnlyList<Sprint>> GetByYearQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default)
    {
        var entities = _context.Sprints.Values
            .Where(x => x.Year == year && x.Quarter == quarter)
            .OrderBy(x => x.SprintNumber)
            .ToList();
        return Task.FromResult<IReadOnlyList<Sprint>>(entities);
    }

    public Task<IReadOnlyList<Sprint>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var entities = _context.Sprints.Values
            .Where(x => x.Year == year)
            .OrderBy(x => x.Quarter)
            .ThenBy(x => x.SprintNumber)
            .ToList();
        return Task.FromResult<IReadOnlyList<Sprint>>(entities);
    }

    public Task<Sprint> AddAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        if (!_context.Sprints.TryAdd(sprint.Id, sprint))
        {
            throw new InvalidOperationException($"Sprint with id '{sprint.Id}' already exists.");
        }
        return Task.FromResult(sprint);
    }

    public Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        if (!_context.Sprints.ContainsKey(sprint.Id))
        {
            throw new InvalidOperationException($"Sprint with id '{sprint.Id}' not found.");
        }
        _context.Sprints[sprint.Id] = sprint;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Sprints.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var exists = _context.Sprints.Values
            .Any(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
