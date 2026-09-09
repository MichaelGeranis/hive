using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite/EF Core implementation of INoteFolderRepository.
/// </summary>
public class SqliteNoteFolderRepository : INoteFolderRepository
{
    private readonly HiveDbContext _context;

    public SqliteNoteFolderRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<NoteFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NoteFolders.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFolder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NoteFolders
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFolder>> GetChildrenAsync(Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        return await _context.NoteFolders
            .Where(f => f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<NoteFolder> AddAsync(NoteFolder folder, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders.Add(folder);
        await _context.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task UpdateAsync(NoteFolder folder, CancellationToken cancellationToken = default)
    {
        _context.NoteFolders.Update(folder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.NoteFolders.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.NoteFolders.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NoteFolders.AnyAsync(f => f.Id == id, cancellationToken);
    }
}
