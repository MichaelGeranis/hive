using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Product management. A product's tasks and parents are
/// the ones whose Jira components name it; its projects are linked by hand on the project.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProjectProductRepository _projectProductRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IActivityService _activityService;

    public ProductService(
        IProductRepository productRepository,
        IProjectProductRepository projectProductRepository,
        IProjectRepository projectRepository,
        ITeamTaskRepository taskRepository,
        IParentRepository parentRepository,
        IActivityService activityService)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _projectProductRepository = projectProductRepository ?? throw new ArgumentNullException(nameof(projectProductRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return (await MapToDtosAsync(new[] { entity }, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _productRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Product(dto.Name, dto.Description);

        if (await _productRepository.NameExistsAsync(entity.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A product with name '{entity.Name}' already exists.");
        }

        var created = await _productRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Product,
            created.Id,
            created.Name,
            $"Product '{created.Name}' was created",
            cancellationToken);

        return (await MapToDtosAsync(new[] { created }, cancellationToken)).Single();
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        if (await _productRepository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A product with name '{dto.Name.Trim()}' already exists.");
        }

        entity.Update(dto.Name, dto.Description);
        await _productRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Product,
            entity.Id,
            entity.Name,
            $"Product '{entity.Name}' was updated",
            cancellationToken);

        return (await MapToDtosAsync(new[] { entity }, cancellationToken)).Single();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        if (entity.IsFromJira)
        {
            throw new DomainException(
                $"Product '{entity.Name}' comes from Jira. Remove the component in Jira and sync instead.");
        }

        await _projectProductRepository.DeleteByProductAsync(id, cancellationToken);
        await _productRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Product,
            id,
            entity.Name,
            $"Product '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<IReadOnlyList<ProductDto>> MapToDtosAsync(
        IReadOnlyList<Product> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return Array.Empty<ProductDto>();
        }

        // Every product needs the same tasks, parents and projects, so each is read once
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var projectsById = (await _projectRepository.GetAllAsync(cancellationToken)).ToDictionary(p => p.Id);
        var links = await _projectProductRepository.GetAllAsync(cancellationToken);

        return products.Select(product =>
        {
            var productTasks = tasks.Where(t => product.IsNamedIn(t.Components)).ToList();
            var completed = productTasks.Count(t => t.IsResolved());

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                JiraComponentId = product.JiraComponentId,
                IsFromJira = product.IsFromJira,
                TotalTasks = productTasks.Count,
                CompletedTasks = completed,
                OpenTasks = productTasks.Count - completed,
                ParentCount = parents.Count(p => product.IsNamedIn(p.Components)),
                Projects = links
                    .Where(l => l.ProductId == product.Id && projectsById.ContainsKey(l.ProjectId))
                    .Select(l => projectsById[l.ProjectId])
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new ProductProjectDto { Id = p.Id, Name = p.Name, Url = p.Url })
                    .ToList(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }).ToList();
    }
}
