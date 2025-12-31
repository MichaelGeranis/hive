using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite/EF Core implementation of IManagerNoteRepository.
/// </summary>
public class SqliteManagerNoteRepository : IManagerNoteRepository
{
    private readonly HiveDbContext _context;

    public SqliteManagerNoteRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ManagerNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerNote>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerNote>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes
            .Where(n => !n.IsCompleted)
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.DueDate)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerNote>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes
            .Where(n => n.IsCompleted)
            .OrderByDescending(n => n.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerNote>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.ManagerNotes
            .Where(n => !n.IsCompleted && n.DueDate.HasValue && n.DueDate.Value < now)
            .OrderBy(n => n.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagerNote> AddAsync(ManagerNote note, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task UpdateAsync(ManagerNote note, CancellationToken cancellationToken = default)
    {
        _context.ManagerNotes.Update(note);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ManagerNotes.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.ManagerNotes.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes.AnyAsync(n => n.Id == id, cancellationToken);
    }
}
