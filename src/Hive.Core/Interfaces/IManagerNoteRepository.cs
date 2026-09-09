using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for ManagerNote entities.
/// </summary>
public interface IManagerNoteRepository
{
    Task<ManagerNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetFilteredPagedAsync(int skip, int take, string? filter, string? searchTerm, string? tag, Guid? folderId = null, NoteSortOrder sort = NoteSortOrder.Priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the notes filed in each folder. Notes at the root are reported under a null folder id.
    /// </summary>
    Task<IReadOnlyList<(Guid? FolderId, int Count)>> GetCountsByFolderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every note filed directly in a folder.
    /// </summary>
    Task<IReadOnlyList<ManagerNote>> GetByFolderAsync(Guid folderId, CancellationToken cancellationToken = default);
    Task<ManagerNote> AddAsync(ManagerNote note, CancellationToken cancellationToken = default);
    Task UpdateAsync(ManagerNote note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
