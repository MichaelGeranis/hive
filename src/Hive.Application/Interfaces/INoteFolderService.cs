using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for NoteFolder use cases.
/// </summary>
public interface INoteFolderService
{
    Task<IReadOnlyList<NoteFolderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<NoteFolderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NoteFolderDto> CreateAsync(CreateNoteFolderDto dto, CancellationToken cancellationToken = default);
    Task<NoteFolderDto> UpdateAsync(Guid id, UpdateNoteFolderDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
