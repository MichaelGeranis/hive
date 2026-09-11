using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for NoteFolder entities.
/// </summary>
public interface INoteFolderRepository
{
    Task<NoteFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoteFolder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoteFolder>> GetChildrenAsync(Guid? parentFolderId, CancellationToken cancellationToken = default);
    Task<NoteFolder> AddAsync(NoteFolder folder, CancellationToken cancellationToken = default);
    Task UpdateAsync(NoteFolder folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
