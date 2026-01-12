using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

public class ChecklistService : IChecklistService
{
    private readonly IChecklistTemplateRepository _templateRepository;
    private readonly IChecklistTemplateItemRepository _templateItemRepository;
    private readonly IChecklistInstanceRepository _instanceRepository;
    private readonly IChecklistInstanceItemRepository _instanceItemRepository;
    private readonly IActivityService _activityService;

    public ChecklistService(
        IChecklistTemplateRepository templateRepository,
        IChecklistTemplateItemRepository templateItemRepository,
        IChecklistInstanceRepository instanceRepository,
        IChecklistInstanceItemRepository instanceItemRepository,
        IActivityService activityService)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _templateItemRepository = templateItemRepository ?? throw new ArgumentNullException(nameof(templateItemRepository));
        _instanceRepository = instanceRepository ?? throw new ArgumentNullException(nameof(instanceRepository));
        _instanceItemRepository = instanceItemRepository ?? throw new ArgumentNullException(nameof(instanceItemRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    // ============== Templates ==============

    public async Task<ChecklistTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToTemplateDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistTemplateWithItemsDto?> GetTemplateWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var items = await _templateItemRepository.GetByTemplateIdAsync(id, cancellationToken);

        return new ChecklistTemplateWithItemsDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.Type,
            TypeName = GetTypeName(entity.Type),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Items = items.Select(MapToTemplateItemDto).ToList()
        };
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetAllAsync(cancellationToken);
        return await MapToTemplateDtosAsync(templates, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesByTypeAsync(ChecklistType type, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetByTypeAsync(type, includeInactive, cancellationToken);
        return await MapToTemplateDtosAsync(templates, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetActiveTemplatesAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetActiveAsync(type, cancellationToken);
        return await MapToTemplateDtosAsync(templates, cancellationToken);
    }

    public async Task<ChecklistTemplateDto> CreateTemplateAsync(CreateChecklistTemplateDto dto, CancellationToken cancellationToken = default)
    {
        if (await _templateRepository.NameExistsAsync(dto.Name, dto.Type, null, cancellationToken))
        {
            throw new InvalidOperationException($"A template with name '{dto.Name}' already exists for type '{dto.Type}'.");
        }

        var entity = new ChecklistTemplate(dto.Name, dto.Description, dto.Type);
        var created = await _templateRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ChecklistTemplate,
            created.Id,
            $"Template '{created.Name}'",
            $"Checklist template '{created.Name}' was created",
            cancellationToken);

        return await MapToTemplateDtoAsync(created, cancellationToken);
    }

    public async Task<ChecklistTemplateDto> UpdateTemplateAsync(Guid id, UpdateChecklistTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetTemplateOrThrowAsync(id, cancellationToken);

        if (await _templateRepository.NameExistsAsync(dto.Name, entity.Type, id, cancellationToken))
        {
            throw new InvalidOperationException($"A template with name '{dto.Name}' already exists for type '{entity.Type}'.");
        }

        entity.Update(dto.Name, dto.Description);
        await _templateRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.ChecklistTemplate,
            entity.Id,
            $"Template '{entity.Name}'",
            $"Checklist template '{entity.Name}' was updated",
            cancellationToken);

        return await MapToTemplateDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistTemplate), id);
        }

        // Check if there are instances using this template
        var instances = await _instanceRepository.GetByTemplateIdAsync(id, cancellationToken);
        if (instances.Count > 0)
        {
            throw new InvalidOperationException($"Cannot delete template. It has {instances.Count} instance(s) associated with it.");
        }

        // Delete all template items
        await _templateItemRepository.DeleteByTemplateIdAsync(id, cancellationToken);

        // Delete the template
        await _templateRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.ChecklistTemplate,
            id,
            $"Template '{entity.Name}'",
            $"Checklist template '{entity.Name}' was deleted",
            cancellationToken);
    }

    public async Task<ChecklistTemplateDto> ActivateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTemplateOrThrowAsync(id, cancellationToken);
        entity.Activate();
        await _templateRepository.UpdateAsync(entity, cancellationToken);

        return await MapToTemplateDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistTemplateDto> DeactivateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTemplateOrThrowAsync(id, cancellationToken);
        entity.Deactivate();
        await _templateRepository.UpdateAsync(entity, cancellationToken);

        return await MapToTemplateDtoAsync(entity, cancellationToken);
    }

    // ============== Template Items ==============

    public async Task<ChecklistTemplateItemDto> AddTemplateItemAsync(Guid templateId, CreateChecklistTemplateItemDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _templateRepository.ExistsAsync(templateId, cancellationToken))
        {
            throw new NotFoundException(nameof(ChecklistTemplate), templateId);
        }

        var nextSortOrder = await _templateItemRepository.GetNextSortOrderAsync(templateId, cancellationToken);

        var entity = new ChecklistTemplateItem(
            templateId,
            nextSortOrder,
            dto.Content,
            dto.ItemType,
            dto.IsRequired,
            dto.HelpText,
            dto.EstimatedMinutes);

        var created = await _templateItemRepository.AddAsync(entity, cancellationToken);
        return MapToTemplateItemDto(created);
    }

    public async Task<ChecklistTemplateItemDto> UpdateTemplateItemAsync(Guid itemId, UpdateChecklistTemplateItemDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetTemplateItemOrThrowAsync(itemId, cancellationToken);

        entity.Update(dto.SortOrder, dto.Content, dto.ItemType, dto.IsRequired, dto.HelpText, dto.EstimatedMinutes);
        await _templateItemRepository.UpdateAsync(entity, cancellationToken);

        return MapToTemplateItemDto(entity);
    }

    public async Task DeleteTemplateItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!await _templateItemRepository.ExistsAsync(itemId, cancellationToken))
        {
            throw new NotFoundException(nameof(ChecklistTemplateItem), itemId);
        }

        await _templateItemRepository.DeleteAsync(itemId, cancellationToken);
    }

    public async Task ReorderTemplateItemsAsync(Guid templateId, IEnumerable<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        if (!await _templateRepository.ExistsAsync(templateId, cancellationToken))
        {
            throw new NotFoundException(nameof(ChecklistTemplate), templateId);
        }

        var items = await _templateItemRepository.GetByTemplateIdAsync(templateId, cancellationToken);
        var itemDict = items.ToDictionary(i => i.Id);

        var sortOrder = 0;
        foreach (var itemId in itemIds)
        {
            if (itemDict.TryGetValue(itemId, out var item))
            {
                item.UpdateSortOrder(sortOrder++);
                await _templateItemRepository.UpdateAsync(item, cancellationToken);
            }
        }
    }

    // ============== Instances ==============

    public async Task<ChecklistInstanceDto?> GetInstanceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _instanceRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToInstanceDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistInstanceWithItemsDto?> GetInstanceWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _instanceRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var template = await _templateRepository.GetByIdAsync(entity.TemplateId, cancellationToken);
        var items = await _instanceItemRepository.GetByInstanceIdAsync(id, cancellationToken);

        var totalItems = items.Count;
        var completedItems = items.Count(i =>
            i.Status == ChecklistItemStatus.Completed ||
            i.Status == ChecklistItemStatus.Skipped ||
            i.Status == ChecklistItemStatus.NotApplicable);
        var progressPercent = totalItems > 0 ? (int)Math.Round((double)completedItems / totalItems * 100) : 0;

        return new ChecklistInstanceWithItemsDto
        {
            Id = entity.Id,
            TemplateId = entity.TemplateId,
            TemplateName = template?.Name ?? "Unknown Template",
            Type = entity.Type,
            TypeName = GetTypeName(entity.Type),
            Title = entity.Title,
            Status = entity.Status,
            StatusName = GetInstanceStatusName(entity.Status),
            CandidateName = entity.CandidateName,
            Position = entity.Position,
            InterviewDate = entity.InterviewDate,
            NewHireName = entity.NewHireName,
            StartDate = entity.StartDate,
            TargetCompletionDate = entity.TargetCompletionDate,
            Notes = entity.Notes,
            TotalItems = totalItems,
            CompletedItems = completedItems,
            ProgressPercent = progressPercent,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt,
            Items = items.Select(MapToInstanceItemDto).ToList()
        };
    }

    public async Task<IReadOnlyList<ChecklistInstanceDto>> GetAllInstancesAsync(CancellationToken cancellationToken = default)
    {
        var instances = await _instanceRepository.GetAllAsync(cancellationToken);
        return await MapToInstanceDtosAsync(instances, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstanceDto>> GetInstancesByTypeAsync(ChecklistType type, CancellationToken cancellationToken = default)
    {
        var instances = await _instanceRepository.GetByTypeAsync(type, cancellationToken);
        return await MapToInstanceDtosAsync(instances, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistInstanceDto>> GetActiveInstancesAsync(ChecklistType? type = null, CancellationToken cancellationToken = default)
    {
        var instances = await _instanceRepository.GetActiveAsync(type, cancellationToken);
        return await MapToInstanceDtosAsync(instances, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> CreateInterviewInstanceAsync(CreateInterviewInstanceDto dto, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(dto.TemplateId, cancellationToken);
        if (template is null)
        {
            throw new NotFoundException(nameof(ChecklistTemplate), dto.TemplateId);
        }

        if (template.Type != ChecklistType.Interview)
        {
            throw new InvalidOperationException("Template is not an Interview type.");
        }

        var instance = ChecklistInstance.CreateInterview(
            dto.TemplateId,
            dto.Title,
            dto.CandidateName,
            dto.Position,
            dto.InterviewDate);

        await _instanceRepository.AddAsync(instance, cancellationToken);

        // Copy template items to instance items
        await CopyTemplateItemsToInstanceAsync(template.Id, instance.Id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ChecklistInstance,
            instance.Id,
            $"Interview '{instance.Title}'",
            $"Interview checklist '{instance.Title}' was created",
            cancellationToken);

        return await MapToInstanceDtoAsync(instance, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> CreateOnboardingInstanceAsync(CreateOnboardingInstanceDto dto, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(dto.TemplateId, cancellationToken);
        if (template is null)
        {
            throw new NotFoundException(nameof(ChecklistTemplate), dto.TemplateId);
        }

        if (template.Type != ChecklistType.Onboarding)
        {
            throw new InvalidOperationException("Template is not an Onboarding type.");
        }

        var instance = ChecklistInstance.CreateOnboarding(
            dto.TemplateId,
            dto.Title,
            dto.NewHireName,
            dto.StartDate,
            dto.TargetCompletionDate);

        await _instanceRepository.AddAsync(instance, cancellationToken);

        // Copy template items to instance items
        await CopyTemplateItemsToInstanceAsync(template.Id, instance.Id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ChecklistInstance,
            instance.Id,
            $"Onboarding '{instance.Title}'",
            $"Onboarding checklist '{instance.Title}' was created",
            cancellationToken);

        return await MapToInstanceDtoAsync(instance, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> StartInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceOrThrowAsync(id, cancellationToken);
        entity.Start();
        await _instanceRepository.UpdateAsync(entity, cancellationToken);

        return await MapToInstanceDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> CompleteInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceOrThrowAsync(id, cancellationToken);
        entity.Complete();
        await _instanceRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Completed,
            EntityType.ChecklistInstance,
            entity.Id,
            $"Checklist '{entity.Title}'",
            $"Checklist '{entity.Title}' was completed",
            cancellationToken);

        return await MapToInstanceDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> CancelInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceOrThrowAsync(id, cancellationToken);
        entity.Cancel();
        await _instanceRepository.UpdateAsync(entity, cancellationToken);

        return await MapToInstanceDtoAsync(entity, cancellationToken);
    }

    public async Task<ChecklistInstanceDto> UpdateInstanceNotesAsync(Guid id, string notes, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceOrThrowAsync(id, cancellationToken);
        entity.UpdateNotes(notes);
        await _instanceRepository.UpdateAsync(entity, cancellationToken);

        return await MapToInstanceDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _instanceRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistInstance), id);
        }

        // Delete all instance items
        await _instanceItemRepository.DeleteByInstanceIdAsync(id, cancellationToken);

        // Delete the instance
        await _instanceRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.ChecklistInstance,
            id,
            $"Checklist '{entity.Title}'",
            $"Checklist '{entity.Title}' was deleted",
            cancellationToken);
    }

    // ============== Instance Items ==============

    public async Task<ChecklistInstanceItemDto> CompleteItemAsync(Guid itemId, CompleteChecklistItemDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceItemOrThrowAsync(itemId, cancellationToken);
        entity.MarkComplete(dto.Notes, dto.Score);
        await _instanceItemRepository.UpdateAsync(entity, cancellationToken);

        // Auto-start instance if not started
        await AutoStartInstanceIfNeededAsync(entity.InstanceId, cancellationToken);

        return MapToInstanceItemDto(entity);
    }

    public async Task<ChecklistInstanceItemDto> SkipItemAsync(Guid itemId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceItemOrThrowAsync(itemId, cancellationToken);
        entity.MarkSkipped(notes);
        await _instanceItemRepository.UpdateAsync(entity, cancellationToken);

        return MapToInstanceItemDto(entity);
    }

    public async Task<ChecklistInstanceItemDto> UpdateItemAsync(Guid itemId, UpdateChecklistItemDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetInstanceItemOrThrowAsync(itemId, cancellationToken);

        if (dto.Notes is not null)
            entity.UpdateNotes(dto.Notes);

        if (dto.Score.HasValue)
            entity.SetScore(dto.Score.Value);

        if (dto.Assignee is not null || dto.DueDate.HasValue)
            entity.SetAssignee(dto.Assignee, dto.DueDate);

        await _instanceItemRepository.UpdateAsync(entity, cancellationToken);

        return MapToInstanceItemDto(entity);
    }

    public async Task<IReadOnlyList<ChecklistInstanceItemDto>> GetOverdueItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _instanceItemRepository.GetOverdueAsync(cancellationToken);
        return items.Select(MapToInstanceItemDto).ToList();
    }

    // ============== Private Helpers ==============

    private async Task CopyTemplateItemsToInstanceAsync(Guid templateId, Guid instanceId, CancellationToken cancellationToken)
    {
        var templateItems = await _templateItemRepository.GetByTemplateIdAsync(templateId, cancellationToken);

        var instanceItems = templateItems.Select(ti => new ChecklistInstanceItem(
            instanceId,
            ti.Id,
            ti.SortOrder,
            ti.Content,
            ti.ItemType,
            ti.IsRequired)).ToList();

        await _instanceItemRepository.AddRangeAsync(instanceItems, cancellationToken);
    }

    private async Task AutoStartInstanceIfNeededAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is not null && instance.Status == ChecklistInstanceStatus.NotStarted)
        {
            instance.Start();
            await _instanceRepository.UpdateAsync(instance, cancellationToken);
        }
    }

    private async Task<ChecklistTemplate> GetTemplateOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistTemplate), id);
        }
        return entity;
    }

    private async Task<ChecklistTemplateItem> GetTemplateItemOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _templateItemRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistTemplateItem), id);
        }
        return entity;
    }

    private async Task<ChecklistInstance> GetInstanceOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _instanceRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistInstance), id);
        }
        return entity;
    }

    private async Task<ChecklistInstanceItem> GetInstanceItemOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _instanceItemRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ChecklistInstanceItem), id);
        }
        return entity;
    }

    private async Task<ChecklistTemplateDto> MapToTemplateDtoAsync(ChecklistTemplate entity, CancellationToken cancellationToken)
    {
        var items = await _templateItemRepository.GetByTemplateIdAsync(entity.Id, cancellationToken);

        return new ChecklistTemplateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.Type,
            TypeName = GetTypeName(entity.Type),
            IsActive = entity.IsActive,
            ItemCount = items.Count,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<ChecklistTemplateDto>> MapToTemplateDtosAsync(IReadOnlyList<ChecklistTemplate> entities, CancellationToken cancellationToken)
    {
        var result = new List<ChecklistTemplateDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToTemplateDtoAsync(entity, cancellationToken));
        }
        return result;
    }

    private static ChecklistTemplateItemDto MapToTemplateItemDto(ChecklistTemplateItem entity)
    {
        return new ChecklistTemplateItemDto
        {
            Id = entity.Id,
            TemplateId = entity.TemplateId,
            SortOrder = entity.SortOrder,
            Content = entity.Content,
            ItemType = entity.ItemType,
            ItemTypeName = GetItemTypeName(entity.ItemType),
            IsRequired = entity.IsRequired,
            HelpText = entity.HelpText,
            EstimatedMinutes = entity.EstimatedMinutes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<ChecklistInstanceDto> MapToInstanceDtoAsync(ChecklistInstance entity, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(entity.TemplateId, cancellationToken);
        var items = await _instanceItemRepository.GetByInstanceIdAsync(entity.Id, cancellationToken);

        var totalItems = items.Count;
        var completedItems = items.Count(i =>
            i.Status == ChecklistItemStatus.Completed ||
            i.Status == ChecklistItemStatus.Skipped ||
            i.Status == ChecklistItemStatus.NotApplicable);
        var progressPercent = totalItems > 0 ? (int)Math.Round((double)completedItems / totalItems * 100) : 0;

        return new ChecklistInstanceDto
        {
            Id = entity.Id,
            TemplateId = entity.TemplateId,
            TemplateName = template?.Name ?? "Unknown Template",
            Type = entity.Type,
            TypeName = GetTypeName(entity.Type),
            Title = entity.Title,
            Status = entity.Status,
            StatusName = GetInstanceStatusName(entity.Status),
            CandidateName = entity.CandidateName,
            Position = entity.Position,
            InterviewDate = entity.InterviewDate,
            NewHireName = entity.NewHireName,
            StartDate = entity.StartDate,
            TargetCompletionDate = entity.TargetCompletionDate,
            Notes = entity.Notes,
            TotalItems = totalItems,
            CompletedItems = completedItems,
            ProgressPercent = progressPercent,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        };
    }

    private async Task<IReadOnlyList<ChecklistInstanceDto>> MapToInstanceDtosAsync(IReadOnlyList<ChecklistInstance> entities, CancellationToken cancellationToken)
    {
        var result = new List<ChecklistInstanceDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToInstanceDtoAsync(entity, cancellationToken));
        }
        return result;
    }

    private static ChecklistInstanceItemDto MapToInstanceItemDto(ChecklistInstanceItem entity)
    {
        return new ChecklistInstanceItemDto
        {
            Id = entity.Id,
            InstanceId = entity.InstanceId,
            TemplateItemId = entity.TemplateItemId,
            SortOrder = entity.SortOrder,
            Content = entity.Content,
            ItemType = entity.ItemType,
            ItemTypeName = GetItemTypeName(entity.ItemType),
            IsRequired = entity.IsRequired,
            Status = entity.Status,
            StatusName = GetItemStatusName(entity.Status),
            Notes = entity.Notes,
            Score = entity.Score,
            Assignee = entity.Assignee,
            DueDate = entity.DueDate,
            IsOverdue = entity.IsOverdue(),
            CompletedAt = entity.CompletedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string GetTypeName(ChecklistType type) => type switch
    {
        ChecklistType.Interview => "Interview",
        ChecklistType.Onboarding => "Onboarding",
        _ => "Unknown"
    };

    private static string GetItemTypeName(ChecklistItemType type) => type switch
    {
        ChecklistItemType.Question => "Question",
        ChecklistItemType.Topic => "Topic",
        ChecklistItemType.Task => "Task",
        ChecklistItemType.Document => "Document",
        ChecklistItemType.Training => "Training",
        _ => "Unknown"
    };

    private static string GetItemStatusName(ChecklistItemStatus status) => status switch
    {
        ChecklistItemStatus.Pending => "Pending",
        ChecklistItemStatus.InProgress => "In Progress",
        ChecklistItemStatus.Completed => "Completed",
        ChecklistItemStatus.Skipped => "Skipped",
        ChecklistItemStatus.NotApplicable => "N/A",
        _ => "Unknown"
    };

    private static string GetInstanceStatusName(ChecklistInstanceStatus status) => status switch
    {
        ChecklistInstanceStatus.NotStarted => "Not Started",
        ChecklistInstanceStatus.InProgress => "In Progress",
        ChecklistInstanceStatus.Completed => "Completed",
        ChecklistInstanceStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
}
