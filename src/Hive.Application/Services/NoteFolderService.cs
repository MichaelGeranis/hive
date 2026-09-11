using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for organising notes into folders.
/// </summary>
public class NoteFolderService : INoteFolderService
{
    private readonly INoteFolderRepository _folderRepository;
    private readonly IManagerNoteRepository _noteRepository;
    private readonly IActivityService _activityService;

    public NoteFolderService(
        INoteFolderRepository folderRepository,
        IManagerNoteRepository noteRepository,
        IActivityService activityService)
    {
        _folderRepository = folderRepository ?? throw new ArgumentNullException(nameof(folderRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<IReadOnlyList<NoteFolderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _folderRepository.GetAllAsync(cancellationToken);
        var counts = await _noteRepository.GetCountsByFolderAsync(cancellationToken);

        var countsByFolder = counts
            .Where(c => c.FolderId.HasValue)
            .ToDictionary(c => c.FolderId!.Value, c => c.Count);

        return folders
            .Select(folder => MapToDto(folder, countsByFolder.GetValueOrDefault(folder.Id)))
            .ToList();
    }

    public async Task<NoteFolderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(id, cancellationToken);
        if (folder is null)
        {
            return null;
        }

        var notes = await _noteRepository.GetByFolderAsync(id, cancellationToken);
        return MapToDto(folder, notes.Count);
    }

    public async Task<NoteFolderDto> CreateAsync(CreateNoteFolderDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureParentExistsAsync(dto.ParentFolderId, cancellationToken);

        var siblings = await _folderRepository.GetChildrenAsync(dto.ParentFolderId, cancellationToken);
        var folder = new NoteFolder(dto.Name, dto.ParentFolderId, siblings.Count);

        var created = await _folderRepository.AddAsync(folder, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.NoteFolder,
            created.Id,
            $"Folder '{created.Name}'",
            $"Note folder '{created.Name}' was created",
            cancellationToken);

        return MapToDto(created, 0);
    }

    public async Task<NoteFolderDto> UpdateAsync(Guid id, UpdateNoteFolderDto dto, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(NoteFolder), id);

        if (folder.ParentFolderId != dto.ParentFolderId)
        {
            await EnsureParentExistsAsync(dto.ParentFolderId, cancellationToken);
            await EnsureNoCycleAsync(id, dto.ParentFolderId, cancellationToken);
            folder.MoveTo(dto.ParentFolderId);
        }

        folder.Rename(dto.Name);
        folder.Reorder(dto.SortOrder);

        await _folderRepository.UpdateAsync(folder, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.NoteFolder,
            folder.Id,
            $"Folder '{folder.Name}'",
            $"Note folder '{folder.Name}' was updated",
            cancellationToken);

        var notes = await _noteRepository.GetByFolderAsync(folder.Id, cancellationToken);
        return MapToDto(folder, notes.Count);
    }

    /// <summary>
    /// Deletes a folder without deleting anything written in it: its notes and its
    /// sub-folders move up to the folder that contained it.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await _folderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(NoteFolder), id);

        var children = await _folderRepository.GetChildrenAsync(id, cancellationToken);
        foreach (var child in children)
        {
            child.MoveTo(folder.ParentFolderId);
            await _folderRepository.UpdateAsync(child, cancellationToken);
        }

        var notes = await _noteRepository.GetByFolderAsync(id, cancellationToken);
        foreach (var note in notes)
        {
            note.MoveToFolder(folder.ParentFolderId);
            await _noteRepository.UpdateAsync(note, cancellationToken);
        }

        await _folderRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.NoteFolder,
            id,
            $"Folder '{folder.Name}'",
            $"Note folder '{folder.Name}' was deleted",
            cancellationToken);
    }

    private async Task EnsureParentExistsAsync(Guid? parentFolderId, CancellationToken cancellationToken)
    {
        if (!parentFolderId.HasValue)
        {
            return;
        }

        var exists = await _folderRepository.ExistsAsync(parentFolderId.Value, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(NoteFolder), parentFolderId.Value);
        }
    }

    /// <summary>
    /// A folder cannot be moved inside itself or inside one of its own descendants.
    /// </summary>
    private async Task EnsureNoCycleAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken)
    {
        if (!newParentId.HasValue)
        {
            return;
        }

        if (newParentId.Value == folderId)
        {
            throw new DomainException("A folder cannot be moved inside itself.");
        }

        var allFolders = await _folderRepository.GetAllAsync(cancellationToken);
        var byId = allFolders.ToDictionary(f => f.Id);

        var currentId = newParentId;
        while (currentId.HasValue && byId.TryGetValue(currentId.Value, out var current))
        {
            if (current.ParentFolderId == folderId)
            {
                throw new DomainException("A folder cannot be moved inside one of its own sub-folders.");
            }

            currentId = current.ParentFolderId;
        }
    }

    private static NoteFolderDto MapToDto(NoteFolder folder, int noteCount)
    {
        return new NoteFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            ParentFolderId = folder.ParentFolderId,
            SortOrder = folder.SortOrder,
            NoteCount = noteCount,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt
        };
    }
}
