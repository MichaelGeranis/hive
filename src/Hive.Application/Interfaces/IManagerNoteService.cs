using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for ManagerNote use cases.
/// </summary>
public interface IManagerNoteService
{
    Task<ManagerNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ManagerNoteDto>> GetAllPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<ManagerNoteDto>> GetFilteredPagedAsync(NotePaginationParams pagination, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerNoteDto>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> CreateAsync(CreateManagerNoteDto dto, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> CreateBlankAsync(CreateBlankNoteDto dto, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> UpdateContentAsync(Guid id, UpdateNoteContentDto dto, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> MoveToFolderAsync(Guid id, MoveNoteDto dto, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> TogglePinAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> UpdateAsync(Guid id, UpdateManagerNoteDto dto, CancellationToken cancellationToken = default);
    Task<ManagerNoteDto> ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
