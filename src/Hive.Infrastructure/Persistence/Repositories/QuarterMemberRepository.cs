using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IQuarterMemberRepository.
/// </summary>
public class QuarterMemberRepository : IQuarterMemberRepository
{
    private readonly InMemoryDbContext _context;

    public QuarterMemberRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<QuarterMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.QuarterMembers.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(QuarterMember entity, CancellationToken cancellationToken = default)
    {
        if (!_context.QuarterMembers.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException($"QuarterMember with id '{entity.Id}' not found.");
        }
        _context.QuarterMembers[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QuarterMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var entities = _context.QuarterMembers.Values
            .Where(x => x.QuarterId == quarterId)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<QuarterMember>>(entities);
    }

    public Task<QuarterMember?> GetByQuarterAndDirectReportAsync(Guid quarterId, Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entity = _context.QuarterMembers.Values
            .FirstOrDefault(x => x.QuarterId == quarterId && x.DirectReportId == directReportId);
        return Task.FromResult(entity);
    }

    public Task DeleteByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        foreach (var id in _context.QuarterMembers.Values.Where(x => x.QuarterId == quarterId).Select(x => x.Id).ToList())
        {
            _context.QuarterMembers.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<QuarterMember> AddAsync(QuarterMember entity, CancellationToken cancellationToken = default)
    {
        if (!_context.QuarterMembers.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"QuarterMember with id '{entity.Id}' already exists.");
        }
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.QuarterMembers.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QuarterMember>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuarterMember> entities = _context.QuarterMembers.Values.OrderBy(x => x.CreatedAt).ToList();
        return Task.FromResult(entities);
    }
}
