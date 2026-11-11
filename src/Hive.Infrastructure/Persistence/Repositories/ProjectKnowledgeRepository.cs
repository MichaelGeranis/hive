using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IProjectKnowledgeRepository.
/// </summary>
public class ProjectKnowledgeRepository : IProjectKnowledgeRepository
{
    private readonly InMemoryDbContext _context;

    public ProjectKnowledgeRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ProjectKnowledge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ProjectKnowledge.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<ProjectKnowledge?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var entity = _context.ProjectKnowledge.Values
            .FirstOrDefault(x => x.DirectReportId == directReportId && x.ProjectId == projectId);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<ProjectKnowledge>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ProjectKnowledge.Values
            .OrderBy(x => x.ProjectId)
            .ThenBy(x => x.DirectReportId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProjectKnowledge>>(entities);
    }

    public Task<IReadOnlyList<ProjectKnowledge>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ProjectKnowledge.Values
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.ProjectId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProjectKnowledge>>(entities);
    }

    public Task<IReadOnlyList<ProjectKnowledge>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ProjectKnowledge.Values
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DirectReportId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProjectKnowledge>>(entities);
    }

    public Task<ProjectKnowledge> AddAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default)
    {
        if (!_context.ProjectKnowledge.TryAdd(knowledge.Id, knowledge))
        {
            throw new InvalidOperationException($"ProjectKnowledge with id '{knowledge.Id}' already exists.");
        }
        return Task.FromResult(knowledge);
    }

    public Task UpdateAsync(ProjectKnowledge knowledge, CancellationToken cancellationToken = default)
    {
        if (!_context.ProjectKnowledge.ContainsKey(knowledge.Id))
        {
            throw new InvalidOperationException($"ProjectKnowledge with id '{knowledge.Id}' not found.");
        }
        _context.ProjectKnowledge[knowledge.Id] = knowledge;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ProjectKnowledge.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ProjectKnowledge.ContainsKey(id));
    }

    public Task<bool> ExistsForDirectReportAndProjectAsync(Guid directReportId, Guid projectId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.ProjectKnowledge.Values
            .Any(x => x.DirectReportId == directReportId
                      && x.ProjectId == projectId
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
