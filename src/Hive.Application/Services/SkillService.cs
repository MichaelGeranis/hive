using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Skill management.
/// </summary>
public class SkillService : ISkillService
{
    private readonly ISkillRepository _repository;
    private readonly ISkillCategoryRepository _categoryRepository;
    private readonly IActivityService _activityService;

    public SkillService(
        ISkillRepository repository,
        ISkillCategoryRepository categoryRepository,
        IActivityService activityService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<SkillDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var categoryName = await GetCategoryNameAsync(entity.SkillCategoryId, cancellationToken);
        return MapToDto(entity, categoryName);
    }

    public async Task<IReadOnlyList<SkillDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(includeInactive, cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(true, cancellationToken);
        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

        return entities.Select(e => MapToDto(e, categoryNames.GetValueOrDefault(e.SkillCategoryId, "Unknown"))).ToList();
    }

    public async Task<IReadOnlyList<SkillDto>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByCategoryIdAsync(categoryId, cancellationToken);
        var categoryName = await GetCategoryNameAsync(categoryId, cancellationToken);

        return entities.Select(e => MapToDto(e, categoryName)).ToList();
    }

    public async Task<SkillDto> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken = default)
    {
        if (await _repository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A skill with name '{dto.Name}' already exists.");
        }

        // Validate category exists
        if (!await _categoryRepository.ExistsAsync(dto.CategoryId, cancellationToken))
        {
            throw new NotFoundException("SkillCategory", dto.CategoryId);
        }

        var entity = new Skill(dto.Name, dto.Description, dto.CategoryId);
        var created = await _repository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Skill,
            created.Id,
            $"Skill '{created.Name}'",
            $"Skill '{created.Name}' was created",
            cancellationToken);

        var categoryName = await GetCategoryNameAsync(dto.CategoryId, cancellationToken);
        return MapToDto(created, categoryName);
    }

    public async Task<SkillDto> UpdateAsync(Guid id, UpdateSkillDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Skill), id);
        }

        if (await _repository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A skill with name '{dto.Name}' already exists.");
        }

        // Validate category exists
        if (!await _categoryRepository.ExistsAsync(dto.CategoryId, cancellationToken))
        {
            throw new NotFoundException("SkillCategory", dto.CategoryId);
        }

        entity.Update(dto.Name, dto.Description, dto.CategoryId);
        await _repository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Skill,
            entity.Id,
            $"Skill '{entity.Name}'",
            $"Skill '{entity.Name}' was updated",
            cancellationToken);

        var categoryName = await GetCategoryNameAsync(dto.CategoryId, cancellationToken);
        return MapToDto(entity, categoryName);
    }

    public async Task<SkillDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Skill), id);
        }

        entity.Activate();
        await _repository.UpdateAsync(entity, cancellationToken);

        var categoryName = await GetCategoryNameAsync(entity.SkillCategoryId, cancellationToken);
        return MapToDto(entity, categoryName);
    }

    public async Task<SkillDto> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Skill), id);
        }

        entity.Deactivate();
        await _repository.UpdateAsync(entity, cancellationToken);

        var categoryName = await GetCategoryNameAsync(entity.SkillCategoryId, cancellationToken);
        return MapToDto(entity, categoryName);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Skill), id);
        }

        await _repository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Skill,
            id,
            $"Skill '{entity.Name}'",
            $"Skill '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<string> GetCategoryNameAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category?.Name ?? "Unknown";
    }

    private static SkillDto MapToDto(Skill entity, string categoryName) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        CategoryId = entity.SkillCategoryId,
        CategoryName = categoryName,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
