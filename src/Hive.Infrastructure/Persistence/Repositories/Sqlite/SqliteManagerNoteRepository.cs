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
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes
            .AsNoTracking()
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => EF.Functions.Collate(n.Title, "NOCASE"))
            .ThenByDescending(n => n.CreatedAt);

        var totalCount = await _context.ManagerNotes.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetFilteredPagedAsync(int skip, int take, string? searchTerm, string? tag, Guid? folderId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes.AsNoTracking();

        // Apply folder filter
        if (folderId.HasValue)
        {
            query = query.Where(n => n.FolderId == folderId.Value);
        }

        // Apply tag filter
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagLower = tag.ToLowerInvariant();
            query = query.Where(n => n.Tags.ToLower().Contains(tagLower));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) ||
                n.Content.ToLower().Contains(term) ||
                n.Tags.ToLower().Contains(term));
        }

        // Get total count after filtering
        var totalCount = await query.CountAsync(cancellationToken);

        // Pinned first, then by title, descending (NOCASE matches NoteText.TitleComparer)
        var orderedQuery = query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => EF.Functions.Collate(n.Title, "NOCASE"))
            .ThenByDescending(n => n.CreatedAt);

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ManagerNote>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var tagLower = tag.ToLowerInvariant();
        return await _context.ManagerNotes
            .AsNoTracking()
            .Where(n => n.Tags.Contains(tagLower))
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => EF.Functions.Collate(n.Title, "NOCASE"))
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerNote>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) ||
                n.Content.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagLower = tag.ToLowerInvariant();
            query = query.Where(n => n.Tags.Contains(tagLower));
        }

        return await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => EF.Functions.Collate(n.Title, "NOCASE"))
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        var allNotes = await _context.ManagerNotes
            .AsNoTracking()
            .Where(n => n.Tags != "")
            .Select(n => n.Tags)
            .ToListAsync(cancellationToken);

        var tags = allNotes
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return tags;
    }

    public async Task<IReadOnlyList<(Guid? FolderId, int Count)>> GetCountsByFolderAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _context.ManagerNotes
            .AsNoTracking()
            .GroupBy(n => n.FolderId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.Select(c => (c.Key, c.Count)).ToList();
    }

    public async Task<IReadOnlyList<ManagerNote>> GetByFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        return await _context.ManagerNotes
            .AsNoTracking()
            .Where(n => n.FolderId == folderId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => EF.Functions.Collate(n.Title, "NOCASE"))
            .ThenByDescending(n => n.CreatedAt)
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
        _context.ManagerNotes.UpdateUntracked(note);
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
