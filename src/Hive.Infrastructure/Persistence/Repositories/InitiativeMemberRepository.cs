using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IInitiativeMemberRepository.
/// </summary>
public class InitiativeMemberRepository : IInitiativeMemberRepository
{
    private readonly InMemoryDbContext _context;

    public InitiativeMemberRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<InitiativeMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.InitiativeMembers.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<InitiativeMember>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeMembers.Values
            .Where(x => x.InitiativeId == initiativeId)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeMember>>(entities);
    }

    public Task<IReadOnlyList<InitiativeMember>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeMembers.Values
            .Where(x => x.DirectReportId == directReportId)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeMember>>(entities);
    }

    public Task<IReadOnlyList<InitiativeMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = _context.Initiatives.Values
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToHashSet();

        var entities = _context.InitiativeMembers.Values
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeMember>>(entities);
    }

    public Task<InitiativeMember> AddAsync(InitiativeMember member, CancellationToken cancellationToken = default)
    {
        if (!_context.InitiativeMembers.TryAdd(member.Id, member))
        {
            throw new InvalidOperationException($"InitiativeMember with id '{member.Id}' already exists.");
        }
        return Task.FromResult(member);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.InitiativeMembers.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var toRemove = _context.InitiativeMembers.Values
            .Where(x => x.InitiativeId == initiativeId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _context.InitiativeMembers.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid initiativeId, Guid directReportId, CancellationToken cancellationToken = default)
    {
        var exists = _context.InitiativeMembers.Values
            .Any(x => x.InitiativeId == initiativeId && x.DirectReportId == directReportId);
        return Task.FromResult(exists);
    }
}
