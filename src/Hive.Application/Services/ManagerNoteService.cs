using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for ManagerNote management.
/// </summary>
public class ManagerNoteService : IManagerNoteService
{
    private readonly IManagerNoteRepository _repository;
    private readonly IActivityService _activityService;

    public ManagerNoteService(IManagerNoteRepository repository, IActivityService activityService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<ManagerNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<ManagerNoteDto>> GetAllPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await _repository.GetAllPagedAsync(pagination.Skip, pagination.PageSize, cancellationToken);
        var dtos = entities.Select(MapToDto).ToList();
        return PagedResult<ManagerNoteDto>.Create(dtos, totalCount, pagination);
    }

    public async Task<PagedResult<ManagerNoteDto>> GetFilteredPagedAsync(NotePaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var filterStr = pagination.Filter switch
        {
            NoteFilter.Pending => "pending",
            NoteFilter.Completed => "completed",
            NoteFilter.Overdue => "overdue",
            _ => null
        };

        var (entities, totalCount) = await _repository.GetFilteredPagedAsync(
            pagination.Skip,
            pagination.PageSize,
            filterStr,
            pagination.SearchTerm,
            pagination.Tag,
            pagination.FolderId,
            pagination.Sort,
            cancellationToken);

        var dtos = entities.Select(MapToDto).ToList();
        return PagedResult<ManagerNoteDto>.Create(dtos, totalCount, pagination);
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetPendingAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetCompletedAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetOverdueAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByTagAsync(tag, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ManagerNoteDto>> SearchAsync(string? searchTerm, string? tag, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.SearchAsync(searchTerm, tag, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllTagsAsync(cancellationToken);
    }

    public async Task<ManagerNoteDto> CreateAsync(CreateManagerNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ManagerNote(
            dto.Title,
            dto.Content,
            dto.Priority,
            dto.DueDate,
            dto.Tags,
            dto.FolderId,
            dto.IsTodo);

        var created = await _repository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ManagerNote,
            created.Id,
            $"Note '{created.Title}'",
            $"Note '{created.Title}' was created",
            cancellationToken);

        return MapToDto(created);
    }

    public async Task<ManagerNoteDto> UpdateAsync(Guid id, UpdateManagerNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Update(dto.Title, dto.Content, dto.Priority, dto.DueDate, dto.Tags);
        entity.MoveToFolder(dto.FolderId);
        entity.SetTodo(dto.IsTodo);
        await _repository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.ManagerNote,
            entity.Id,
            $"Note '{entity.Title}'",
            $"Note '{entity.Title}' was updated",
            cancellationToken);

        return MapToDto(entity);
    }

    /// <summary>
    /// Opens a blank note in a folder. The manager starts typing straight away; the note
    /// exists from the first keystroke, so nothing is lost if the app closes.
    /// </summary>
    public async Task<ManagerNoteDto> CreateBlankAsync(CreateBlankNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = ManagerNote.CreateBlank(dto.FolderId);
        var created = await _repository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ManagerNote,
            created.Id,
            $"Note '{created.Title}'",
            $"Note '{created.Title}' was created",
            cancellationToken);

        return MapToDto(created);
    }

    /// <summary>
    /// Saves the body of a note as it is written and re-derives its title from the first line.
    /// Deliberately not logged to the activity feed: this runs on every autosave, and a feed
    /// of keystrokes is not history.
    /// </summary>
    public async Task<ManagerNoteDto> UpdateContentAsync(Guid id, UpdateNoteContentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateContent(dto.Content);
        await _repository.UpdateAsync(entity, cancellationToken);

        return MapToDto(entity);
    }

    public async Task<ManagerNoteDto> MoveToFolderAsync(Guid id, MoveNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.MoveToFolder(dto.FolderId);
        await _repository.UpdateAsync(entity, cancellationToken);

        return MapToDto(entity);
    }

    public async Task<ManagerNoteDto> TogglePinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.TogglePin();
        await _repository.UpdateAsync(entity, cancellationToken);

        return MapToDto(entity);
    }

    public async Task<ManagerNoteDto> ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.ToggleComplete();
        await _repository.UpdateAsync(entity, cancellationToken);

        var activityType = entity.IsCompleted ? ActivityType.Completed : ActivityType.Updated;
        var description = entity.IsCompleted
            ? $"Note '{entity.Title}' was completed"
            : $"Note '{entity.Title}' was reopened";

        await _activityService.LogActivityAsync(
            activityType,
            EntityType.ManagerNote,
            entity.Id,
            $"Note '{entity.Title}'",
            description,
            cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ManagerNote), id);
        }

        await _repository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.ManagerNote,
            id,
            $"Note '{entity.Title}'",
            $"Note '{entity.Title}' was deleted",
            cancellationToken);
    }

    private async Task<ManagerNote> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ManagerNote), id);
        }
        return entity;
    }

    private static ManagerNoteDto MapToDto(ManagerNote entity)
    {
        return new ManagerNoteDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            Tags = entity.Tags,
            TagsList = entity.GetTagsList(),
            Priority = entity.Priority,
            PriorityName = GetPriorityName(entity.Priority),
            FolderId = entity.FolderId,
            IsPinned = entity.IsPinned,
            IsTodo = entity.IsTodo,
            Snippet = BuildSnippet(entity.Content),
            IsCompleted = entity.IsCompleted,
            DueDate = entity.DueDate,
            IsOverdue = entity.IsOverdue(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        };
    }

    /// <summary>
    /// Builds the one-line preview shown under a note's title in the list, skipping the
    /// line the title itself came from.
    /// </summary>
    private static string BuildSnippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var lines = content.Split('\n');
        var skippedTitleLine = false;
        var parts = new List<string>();

        foreach (var line in lines)
        {
            var text = line.Replace("\r", string.Empty).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (!skippedTitleLine)
            {
                skippedTitleLine = true;
                continue;
            }

            parts.Add(text);

            if (parts.Sum(p => p.Length) > 200)
            {
                break;
            }
        }

        var snippet = string.Join(" ", parts);
        return snippet.Length > 200 ? snippet[..200].TrimEnd() : snippet;
    }

    private static string GetPriorityName(NotePriority priority) => priority switch
    {
        NotePriority.Low => "Low",
        NotePriority.Normal => "Normal",
        NotePriority.High => "High",
        NotePriority.Urgent => "Urgent",
        _ => "Unknown"
    };
}
