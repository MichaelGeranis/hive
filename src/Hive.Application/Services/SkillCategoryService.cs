using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for SkillCategory management.
/// </summary>
public class SkillCategoryService : ISkillCategoryService
{
    private readonly ISkillCategoryRepository _repository;
    private readonly IActivityService _activityService;

    public SkillCategoryService(ISkillCategoryRepository repository, IActivityService activityService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<SkillCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var skillCount = await GetSkillCountAsync(id, cancellationToken);
        return MapToDto(entity, skillCount);
    }

    public async Task<IReadOnlyList<SkillCategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(includeInactive, cancellationToken);
        var result = new List<SkillCategoryDto>();

        foreach (var entity in entities)
        {
            var skillCount = await GetSkillCountAsync(entity.Id, cancellationToken);
            result.Add(MapToDto(entity, skillCount));
        }

        return result;
    }

    public async Task<SkillCategoryDto> CreateAsync(CreateSkillCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (await _repository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A skill category with name '{dto.Name}' already exists.");
        }

        var entity = new SkillCategoryEntity(dto.Name, dto.Description, dto.SortOrder);
        var created = await _repository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Skill,
            created.Id,
            $"Skill Category '{created.Name}'",
            $"Skill category '{created.Name}' was created",
            cancellationToken);

        return MapToDto(created, 0);
    }

    public async Task<SkillCategoryDto> UpdateAsync(Guid id, UpdateSkillCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillCategoryEntity), id);
        }

        if (await _repository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A skill category with name '{dto.Name}' already exists.");
        }

        entity.Update(dto.Name, dto.Description, dto.SortOrder);
        await _repository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Skill,
            entity.Id,
            $"Skill Category '{entity.Name}'",
            $"Skill category '{entity.Name}' was updated",
            cancellationToken);

        var skillCount = await GetSkillCountAsync(id, cancellationToken);
        return MapToDto(entity, skillCount);
    }

    public async Task<SkillCategoryDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillCategoryEntity), id);
        }

        entity.Activate();
        await _repository.UpdateAsync(entity, cancellationToken);

        var skillCount = await GetSkillCountAsync(id, cancellationToken);
        return MapToDto(entity, skillCount);
    }

    public async Task<SkillCategoryDto> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillCategoryEntity), id);
        }

        entity.Deactivate();
        await _repository.UpdateAsync(entity, cancellationToken);

        var skillCount = await GetSkillCountAsync(id, cancellationToken);
        return MapToDto(entity, skillCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillCategoryEntity), id);
        }

        // Check if category has skills
        if (await _repository.HasSkillsAsync(id, cancellationToken))
        {
            throw new ConflictException($"Cannot delete skill category '{entity.Name}' because it has skills assigned to it. Please reassign or delete those skills first.");
        }

        await _repository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Skill,
            id,
            $"Skill Category '{entity.Name}'",
            $"Skill category '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<int> GetSkillCountAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _repository.HasSkillsAsync(categoryId, cancellationToken) ? 1 : 0;
    }

    private static SkillCategoryDto MapToDto(SkillCategoryEntity entity, int skillCount) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        SkillCount = skillCount,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
