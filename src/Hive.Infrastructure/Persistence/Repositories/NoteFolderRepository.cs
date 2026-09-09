using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of INoteFolderRepository.
/// </summary>
public class NoteFolderRepository : INoteFolderRepository
{
    private readonly InMemoryDbContext _context;

    public NoteFolderRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<NoteFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders.TryGetValue(id, out var folder);
        return Task.FromResult(folder);
    }

    public Task<IReadOnlyList<NoteFolder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var folders = _context.NoteFolders.Values
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<NoteFolder>>(folders);
    }

    public Task<IReadOnlyList<NoteFolder>> GetChildrenAsync(Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        var folders = _context.NoteFolders.Values
            .Where(f => f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<NoteFolder>>(folders);
    }

    public Task<NoteFolder> AddAsync(NoteFolder folder, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders.TryAdd(folder.Id, folder);
        return Task.FromResult(folder);
    }

    public Task UpdateAsync(NoteFolder folder, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders[folder.Id] = folder;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.NoteFolders.ContainsKey(id));
    }
}
