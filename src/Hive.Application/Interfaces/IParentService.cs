using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for Parent operations.
/// </summary>
public interface IParentService
{
    Task<ParentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParentDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ParentDto> CreateAsync(CreateParentDto dto, CancellationToken cancellationToken = default);
    Task<ParentDto> GetOrCreateAsync(string name, int? timeSpentMinutes = null, Guid? teamTaskId = null, CancellationToken cancellationToken = default);
    Task<ParentDto> UpdateAsync(Guid id, UpdateParentDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
