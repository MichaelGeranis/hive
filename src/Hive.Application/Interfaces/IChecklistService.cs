using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Interfaces;

public interface IChecklistService
{
    // Templates
    Task<ChecklistTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateWithItemsDto?> GetTemplateWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplateDto>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesByTypeAsync(ChecklistType type, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistTemplateDto>> GetActiveTemplatesAsync(ChecklistType? type = null, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateDto> CreateTemplateAsync(CreateChecklistTemplateDto dto, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateDto> UpdateTemplateAsync(Guid id, UpdateChecklistTemplateDto dto, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateDto> ActivateTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateDto> DeactivateTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    // Template Items
    Task<ChecklistTemplateItemDto> AddTemplateItemAsync(Guid templateId, CreateChecklistTemplateItemDto dto, CancellationToken cancellationToken = default);
    Task<ChecklistTemplateItemDto> UpdateTemplateItemAsync(Guid itemId, UpdateChecklistTemplateItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteTemplateItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task ReorderTemplateItemsAsync(Guid templateId, IEnumerable<Guid> itemIds, CancellationToken cancellationToken = default);

    // Instances
    Task<ChecklistInstanceDto?> GetInstanceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceWithItemsDto?> GetInstanceWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceDto>> GetAllInstancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceDto>> GetInstancesByTypeAsync(ChecklistType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceDto>> GetActiveInstancesAsync(ChecklistType? type = null, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> CreateInterviewInstanceAsync(CreateInterviewInstanceDto dto, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> CreateOnboardingInstanceAsync(CreateOnboardingInstanceDto dto, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> StartInstanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> CompleteInstanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> CancelInstanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceDto> UpdateInstanceNotesAsync(Guid id, string notes, CancellationToken cancellationToken = default);
    Task DeleteInstanceAsync(Guid id, CancellationToken cancellationToken = default);

    // Instance Items
    Task<ChecklistInstanceItemDto> CompleteItemAsync(Guid itemId, CompleteChecklistItemDto dto, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceItemDto> SkipItemAsync(Guid itemId, string? notes = null, CancellationToken cancellationToken = default);
    Task<ChecklistInstanceItemDto> UpdateItemAsync(Guid itemId, UpdateChecklistItemDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChecklistInstanceItemDto>> GetOverdueItemsAsync(CancellationToken cancellationToken = default);
}
