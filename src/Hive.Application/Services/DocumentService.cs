using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Document management.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IActivityService _activityService;

    public DocumentService(IDocumentRepository documentRepository, IActivityService activityService)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _documentRepository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetByTagsAsync(string tags, CancellationToken cancellationToken = default)
    {
        var entities = await _documentRepository.GetByTagsAsync(tags, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Document(dto.Title, dto.Content, dto.Url, dto.Tags);
        await _documentRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Document,
            entity.Id,
            $"Document '{entity.Title}'",
            $"Document '{entity.Title}' was created",
            cancellationToken);

        return MapToDto(entity);
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Document", id);
        }

        entity.Update(dto.Title, dto.Content, dto.Url, dto.Tags);
        await _documentRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Document,
            entity.Id,
            $"Document '{entity.Title}'",
            $"Document '{entity.Title}' was updated",
            cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Document", id);
        }

        await _documentRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Document,
            id,
            $"Document '{entity.Title}'",
            $"Document '{entity.Title}' was deleted",
            cancellationToken);
    }

    private static DocumentDto MapToDto(Document entity)
    {
        return new DocumentDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            Url = entity.Url,
            Tags = entity.Tags,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}