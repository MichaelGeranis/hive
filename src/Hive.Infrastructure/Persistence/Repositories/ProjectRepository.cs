using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IProjectRepository.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly InMemoryDbContext _context;

    public ProjectRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Projects.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Projects.Values
            .OrderBy(x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Project>>(entities);
    }

    public Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (!_context.Projects.TryAdd(project.Id, project))
        {
            throw new InvalidOperationException($"Project with id '{project.Id}' already exists.");
        }
        return Task.FromResult(project);
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (!_context.Projects.ContainsKey(project.Id))
        {
            throw new InvalidOperationException($"Project with id '{project.Id}' not found.");
        }
        _context.Projects[project.Id] = project;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Projects.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Projects.ContainsKey(id));
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.Projects.Values
            .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
