using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Project management. A project is linked to the
/// products it serves, never to tasks.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProjectProductRepository _projectProductRepository;
    private readonly IActivityService _activityService;

    public ProjectService(
        IProjectRepository projectRepository,
        IProductRepository productRepository,
        IProjectProductRepository projectProductRepository,
        IActivityService activityService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _projectProductRepository = projectProductRepository ?? throw new ArgumentNullException(nameof(projectProductRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _projectRepository.GetAllAsync(cancellationToken);
        var productsById = await GetProductsByIdAsync(cancellationToken);
        var links = await _projectProductRepository.GetAllAsync(cancellationToken);

        return entities.Select(e => MapToDto(e, links, productsById)).ToList();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (await _projectRepository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        var productIds = await GetExistingProductIdsAsync(dto.ProductIds, cancellationToken);
        var entity = new Project(dto.Name, dto.Description, dto.Url);
        var created = await _projectRepository.AddAsync(entity, cancellationToken);
        await SetProductsAsync(created.Id, productIds, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Project,
            created.Id,
            created.Name,
            $"Project '{created.Name}' was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (await _projectRepository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        var productIds = await GetExistingProductIdsAsync(dto.ProductIds, cancellationToken);
        entity.Update(dto.Name, dto.Description, dto.Url);
        await _projectRepository.UpdateAsync(entity, cancellationToken);
        await SetProductsAsync(entity.Id, productIds, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Project,
            entity.Id,
            entity.Name,
            $"Project '{entity.Name}' was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        await _projectProductRepository.DeleteByProjectAsync(id, cancellationToken);
        await _projectRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Project,
            id,
            entity.Name,
            $"Project '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<Project> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }
        return entity;
    }

    /// <summary>
    /// The requested products, de-duplicated. Every one must exist, so a project is never
    /// linked to a product that is not there.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> GetExistingProductIdsAsync(IEnumerable<Guid>? productIds, CancellationToken cancellationToken)
    {
        var ids = (productIds ?? Enumerable.Empty<Guid>()).Distinct().ToList();
        foreach (var productId in ids)
        {
            if (await _productRepository.GetByIdAsync(productId, cancellationToken) is null)
            {
                throw new NotFoundException(nameof(Product), productId);
            }
        }

        return ids;
    }

    private async Task SetProductsAsync(Guid projectId, IReadOnlyList<Guid> productIds, CancellationToken cancellationToken)
    {
        await _projectProductRepository.DeleteByProjectAsync(projectId, cancellationToken);
        foreach (var productId in productIds)
        {
            await _projectProductRepository.AddAsync(new ProjectProduct(projectId, productId), cancellationToken);
        }
    }

    private async Task<Dictionary<Guid, Product>> GetProductsByIdAsync(CancellationToken cancellationToken) =>
        (await _productRepository.GetAllAsync(cancellationToken)).ToDictionary(p => p.Id);

    private async Task<ProjectDto> MapToDtoAsync(Project entity, CancellationToken cancellationToken)
    {
        var links = await _projectProductRepository.GetByProjectAsync(entity.Id, cancellationToken);
        return MapToDto(entity, links, await GetProductsByIdAsync(cancellationToken));
    }

    private static ProjectDto MapToDto(
        Project entity,
        IReadOnlyList<ProjectProduct> links,
        IReadOnlyDictionary<Guid, Product> productsById) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Url = entity.Url,
            Products = links
            .Where(l => l.ProjectId == entity.Id && productsById.ContainsKey(l.ProductId))
            .Select(l => productsById[l.ProductId])
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ProjectProductDto { Id = p.Id, Name = p.Name })
            .ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
}
