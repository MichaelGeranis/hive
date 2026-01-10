using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

public class ChecklistTemplateRepository : IChecklistTemplateRepository
{
    private readonly InMemoryDbContext _context;

    public ChecklistTemplateRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ChecklistTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplates.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<ChecklistTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChecklistTemplates.Values
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistTemplate>>(entities);
    }

    public Task<IReadOnlyList<ChecklistTemplate>> GetByTypeAsync(ChecklistType type, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistTemplates.Values
            .Where(x => x.Type == type);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = query
            .OrderBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistTemplate>>(entities);
    }

    public Task<IReadOnlyList<ChecklistTemplate>> GetActiveAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChecklistTemplates.Values
            .Where(x => x.IsActive);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        var entities = query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChecklistTemplate>>(entities);
    }

    public Task<ChecklistTemplate> AddAsync(ChecklistTemplate template, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistTemplates.TryAdd(template.Id, template))
        {
            throw new InvalidOperationException($"ChecklistTemplate with id '{template.Id}' already exists.");
        }
        return Task.FromResult(template);
    }

    public Task UpdateAsync(ChecklistTemplate template, CancellationToken cancellationToken = default)
    {
        if (!_context.ChecklistTemplates.ContainsKey(template.Id))
        {
            throw new InvalidOperationException($"ChecklistTemplate with id '{template.Id}' not found.");
        }
        _context.ChecklistTemplates[template.Id] = template;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ChecklistTemplates.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ChecklistTemplates.ContainsKey(id));
    }

    public Task<bool> NameExistsAsync(string name, ChecklistType type, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.ChecklistTemplates.Values
            .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                      && x.Type == type
                      && (!excludeId.HasValue || x.Id != excludeId.Value));

        return Task.FromResult(exists);
    }
}
