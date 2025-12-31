using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IManagerNoteRepository.
/// </summary>
public class ManagerNoteRepository : IManagerNoteRepository
{
    private readonly InMemoryDbContext _context;

    public ManagerNoteRepository(InMemoryDbContext context)
    {
        _context = context;
    }

    public Task<ManagerNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes.TryGetValue(id, out var note);
        return Task.FromResult(note);
    }

    public Task<IReadOnlyList<ManagerNote>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<ManagerNote>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => !n.IsCompleted)
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.DueDate)
            .ThenByDescending(n => n.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<ManagerNote>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => n.IsCompleted)
            .OrderByDescending(n => n.CompletedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<ManagerNote>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => n.IsOverdue())
            .OrderBy(n => n.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<ManagerNote> AddAsync(ManagerNote note, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes.TryAdd(note.Id, note);
        return Task.FromResult(note);
    }

    public Task UpdateAsync(ManagerNote note, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes[note.Id] = note;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.ManagerNotes.ContainsKey(id));
    }
}
