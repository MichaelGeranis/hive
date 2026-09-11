using Hive.Application.DTOs;
using Hive.Application.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// End-to-end coverage of writing a note into a folder through the real services and
/// the in-memory persistence stack.
/// </summary>
public class NotesIntegrationTests : IntegrationTestBase
{
    private readonly INoteFolderService _folderService;
    private readonly IManagerNoteService _noteService;

    public NotesIntegrationTests()
    {
        _folderService = GetService<INoteFolderService>();
        _noteService = GetService<IManagerNoteService>();
    }

    [Fact]
    public async Task WritingANoteInAFolder_SavesItWithATitleTakenFromItsFirstLine()
    {
        // Arrange
        var folder = await _folderService.CreateAsync(new CreateNoteFolderDto { Name = "Team" });

        // Act
        var blank = await _noteService.CreateBlankAsync(new CreateBlankNoteDto { FolderId = folder.Id });
        var written = await _noteService.UpdateContentAsync(blank.Id, new UpdateNoteContentDto
        {
            Content = "# 1:1 prep\n- Ask about the migration"
        });

        // Assert
        written.Title.Should().Be("1:1 prep");
        written.FolderId.Should().Be(folder.Id);

        var folders = await _folderService.GetAllAsync();
        folders.Single(f => f.Id == folder.Id).NoteCount.Should().Be(1);
    }

    [Fact]
    public async Task DeletingAFolder_KeepsItsNotes()
    {
        // Arrange
        var folder = await _folderService.CreateAsync(new CreateNoteFolderDto { Name = "Temporary" });
        var note = await _noteService.CreateBlankAsync(new CreateBlankNoteDto { FolderId = folder.Id });

        // Act
        await _folderService.DeleteAsync(folder.Id);

        // Assert
        var kept = await _noteService.GetByIdAsync(note.Id);
        kept.Should().NotBeNull();
        kept!.FolderId.Should().BeNull();
    }

    [Fact]
    public async Task ListingAFolder_ReturnsOnlyItsNotesWithPinnedOnesFirst()
    {
        // Arrange
        var folder = await _folderService.CreateAsync(new CreateNoteFolderDto { Name = "Team" });
        var first = await _noteService.CreateBlankAsync(new CreateBlankNoteDto { FolderId = folder.Id });
        await _noteService.UpdateContentAsync(first.Id, new UpdateNoteContentDto { Content = "First note" });

        var second = await _noteService.CreateBlankAsync(new CreateBlankNoteDto { FolderId = folder.Id });
        await _noteService.UpdateContentAsync(second.Id, new UpdateNoteContentDto { Content = "Second note" });
        await _noteService.TogglePinAsync(first.Id);

        await _noteService.CreateBlankAsync(new CreateBlankNoteDto());

        // Act
        var result = await _noteService.GetFilteredPagedAsync(new NotePaginationParams
        {
            FolderId = folder.Id,
            Sort = Hive.Core.Entities.NoteSortOrder.Recent
        });

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items[0].Id.Should().Be(first.Id);
        result.Items[0].IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task AFreshNote_DoesNotCountAsAPendingTodo()
    {
        // Arrange
        await _noteService.CreateBlankAsync(new CreateBlankNoteDto());

        // Act
        var pending = await _noteService.GetPendingAsync();

        // Assert
        pending.Should().BeEmpty();
    }
}
