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
