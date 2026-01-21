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
    Task<(IReadOnlyList<ManagerNote> Items, int TotalCount)> GetFilteredPagedAsync(int skip, int take, string? filter, string? searchTerm, string? tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNote>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default);
    Task<ManagerNote> AddAsync(ManagerNote note, CancellationToken cancellationToken = default);
    Task UpdateAsync(ManagerNote note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
