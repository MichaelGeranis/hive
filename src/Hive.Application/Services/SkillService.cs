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

    public SkillService(ISkillRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<SkillDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<SkillDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(includeInactive, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SkillDto>> GetByCategoryAsync(SkillCategory category, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByCategoryAsync(category, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<SkillDto> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken = default)
    {
        if (await _repository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A skill with name '{dto.Name}' already exists.");
        }

        var entity = new Skill(dto.Name, dto.Description, dto.Category);
        var created = await _repository.AddAsync(entity, cancellationToken);

        return MapToDto(created);
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

        entity.Update(dto.Name, dto.Description, dto.Category);
        await _repository.UpdateAsync(entity, cancellationToken);

        return MapToDto(entity);
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

        return MapToDto(entity);
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

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(Skill), id);
        }

        await _repository.DeleteAsync(id, cancellationToken);
    }

    private static SkillDto MapToDto(Skill entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Category = entity.Category,
        CategoryName = GetCategoryName(entity.Category),
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static string GetCategoryName(SkillCategory category) => category switch
    {
        SkillCategory.Technical => "Technical",
        SkillCategory.SoftSkills => "Soft Skills",
        SkillCategory.Leadership => "Leadership",
        SkillCategory.DomainKnowledge => "Domain Knowledge",
        SkillCategory.Tools => "Tools",
        _ => "Unknown"
    };
}
