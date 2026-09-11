using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IOneOnOneMeetingRepository.
/// </summary>
public class OneOnOneMeetingRepository : IOneOnOneMeetingRepository
{
    private readonly InMemoryDbContext _context;

    public OneOnOneMeetingRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<OneOnOneMeeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.OneOnOneMeetings.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<OneOnOneMeeting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.OneOnOneMeetings.Values
            .OrderByDescending(x => x.MeetingDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<OneOnOneMeeting>>(entities);
    }

    public Task<IReadOnlyList<OneOnOneMeeting>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.OneOnOneMeetings.Values
            .Where(x => x.DirectReportId == directReportId)
            .OrderByDescending(x => x.MeetingDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<OneOnOneMeeting>>(entities);
    }

    public Task<(IReadOnlyList<OneOnOneMeeting> Items, int TotalCount)> GetFilteredPagedAsync(
        int skip,
        int take,
        Guid? directReportId,
        bool unlinkedOnly,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OneOnOneMeetings.Values.AsEnumerable();

        if (unlinkedOnly)
        {
            query = query.Where(m => m.DirectReportId == null);
        }
        else if (directReportId.HasValue)
        {
            query = query.Where(m => m.DirectReportId == directReportId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Title.ToLowerInvariant().Contains(term) ||
                m.Content.ToLowerInvariant().Contains(term) ||
                m.Tags.ToLowerInvariant().Contains(term));
        }

        var ordered = query
            .OrderByDescending(m => m.MeetingDate)
            .ThenByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .ToList();

        var items = ordered.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<OneOnOneMeeting>, int)>((items, ordered.Count));
    }

    public Task<IReadOnlyList<(Guid? DirectReportId, int Count)>> GetCountsByDirectReportAsync(CancellationToken cancellationToken = default)
    {
        var counts = _context.OneOnOneMeetings.Values
            .GroupBy(m => m.DirectReportId)
            .Select(g => (DirectReportId: g.Key, Count: g.Count()))
            .ToList();
        return Task.FromResult<IReadOnlyList<(Guid?, int)>>(counts);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.OneOnOneMeetings.Count);
    }

    public Task<OneOnOneMeeting> AddAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default)
    {
        if (!_context.OneOnOneMeetings.TryAdd(meeting.Id, meeting))
        {
            throw new InvalidOperationException($"OneOnOneMeeting with id '{meeting.Id}' already exists.");
        }
        return Task.FromResult(meeting);
    }

    public Task UpdateAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default)
    {
        if (!_context.OneOnOneMeetings.ContainsKey(meeting.Id))
        {
            throw new InvalidOperationException($"OneOnOneMeeting with id '{meeting.Id}' not found.");
        }
        _context.OneOnOneMeetings[meeting.Id] = meeting;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.OneOnOneMeetings.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.OneOnOneMeetings.ContainsKey(id));
    }
}
