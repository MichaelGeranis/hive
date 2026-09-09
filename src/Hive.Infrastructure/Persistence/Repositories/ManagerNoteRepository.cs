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
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

    public Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes.Values
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.DueDate)
            .ThenByDescending(n => n.CreatedAt);

        var totalCount = _context.ManagerNotes.Count;
        var items = query.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<ManagerNote>, int)>((items, totalCount));
    }

    public Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetFilteredPagedAsync(int skip, int take, string? filter, string? searchTerm, string? tag, Guid? folderId = null, NoteSortOrder sort = NoteSortOrder.Priority, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes.Values.AsEnumerable();

        // Apply status filter - only notes tracked as to-dos have a pending or overdue state
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter.ToLowerInvariant() switch
            {
                "pending" => query.Where(n => n.IsTodo && !n.IsCompleted),
                "completed" => query.Where(n => n.IsCompleted),
                "overdue" => query.Where(n => n.IsTodo && n.IsOverdue()),
                _ => query
            };
        }

        // Apply folder filter
        if (folderId.HasValue)
        {
            query = query.Where(n => n.FolderId == folderId.Value);
        }

        // Apply tag filter
        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(n => n.HasTag(tag));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(n =>
                n.Title.ToLowerInvariant().Contains(term) ||
                n.Content.ToLowerInvariant().Contains(term) ||
                n.Tags.ToLowerInvariant().Contains(term));
        }

        // Apply ordering
        var orderedQuery = sort == NoteSortOrder.Recent
            ? query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt ?? n.CreatedAt)
            : query
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.DueDate)
                .ThenByDescending(n => n.CreatedAt);

        var filteredList = orderedQuery.ToList();
        var totalCount = filteredList.Count;
        var items = filteredList.Skip(skip).Take(take).ToList();

        return Task.FromResult<(IReadOnlyList<ManagerNote>, int)>((items, totalCount));
    }

    public Task<IReadOnlyList<ManagerNote>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => n.IsTodo && !n.IsCompleted)
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
            .Where(n => n.IsTodo && n.IsOverdue())
            .OrderBy(n => n.DueDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<ManagerNote>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => n.HasTag(tag))
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<ManagerNote>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default)
    {
        var query = _context.ManagerNotes.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(n =>
                n.Title.ToLowerInvariant().Contains(term) ||
                n.Content.ToLowerInvariant().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(n => n.HasTag(tag));
        }

        var notes = query
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ManagerNote>>(notes);
    }

    public Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = _context.ManagerNotes.Values
            .SelectMany(n => n.GetTagsList())
            .Distinct()
            .OrderBy(t => t)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(tags);
    }

    public Task<IReadOnlyList<(Guid? FolderId, int Count)>> GetCountsByFolderAsync(CancellationToken cancellationToken = default)
    {
        var counts = _context.ManagerNotes.Values
            .GroupBy(n => n.FolderId)
            .Select(g => (FolderId: g.Key, Count: g.Count()))
            .ToList();
        return Task.FromResult<IReadOnlyList<(Guid?, int)>>(counts);
    }

    public Task<IReadOnlyList<ManagerNote>> GetByFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        var notes = _context.ManagerNotes.Values
            .Where(n => n.FolderId == folderId)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt ?? n.CreatedAt)
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
