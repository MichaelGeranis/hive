using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ManagerNoteRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ManagerNoteRepository _repository;

    public ManagerNoteRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ManagerNoteRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ManagerNoteRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var note = CreateAndAddNote("Test note");

        // Act
        var result = await _repository.GetByIdAsync(note.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(note.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotes()
    {
        // Arrange
        CreateAndAddNote("Note 1");
        CreateAndAddNote("Note 2");
        CreateAndAddNote("Note 3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var note1 = CreateAndAddNote("First");
        await Task.Delay(10);
        var note2 = CreateAndAddNote("Second");
        await Task.Delay(10);
        var note3 = CreateAndAddNote("Third");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(note3.Id); // Most recent first
        result[1].Id.Should().Be(note2.Id);
        result[2].Id.Should().Be(note1.Id);
    }

    [Fact]
    public async Task GetByTagAsync_ReturnsNotesWithSpecificTag()
    {
        // Arrange
        CreateAndAddNote("Note 1", tags: "important,urgent");
        CreateAndAddNote("Note 2", tags: "important");
        CreateAndAddNote("Note 3", tags: "review");

        // Act
        var result = await _repository.GetByTagAsync("important");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTagAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddNote("Note 1", tags: "IMPORTANT");
        CreateAndAddNote("Note 2", tags: "Important");

        // Act
        var result = await _repository.GetByTagAsync("important");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTagAsync_OrdersPinnedFirstThenTitleDescending()
    {
        // Arrange
        var alpha = CreateAndAddNote("Alpha", tags: "test");
        var charlie = CreateAndAddNote("Charlie", tags: "test");
        var bravo = CreateAndAddNote("Bravo", tags: "test");
        alpha.Pin();

        // Act
        var result = await _repository.GetByTagAsync("test");

        // Assert
        result.Select(n => n.Title).Should().Equal("Alpha", "Charlie", "Bravo");
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_SearchesTitleAndContent()
    {
        // Arrange
        CreateAndAddNote("Important meeting", "Discuss project");
        CreateAndAddNote("Review code", "Important changes");
        CreateAndAddNote("Update docs", "Regular update");

        // Act
        var result = await _repository.SearchAsync("important", null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddNote("IMPORTANT Title", "content");
        CreateAndAddNote("Normal title", "IMPORTANT content");

        // Act
        var result = await _repository.SearchAsync("important", null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WithTag_FiltersToTag()
    {
        // Arrange
        CreateAndAddNote("Note 1", tags: "important");
        CreateAndAddNote("Note 2", tags: "review");
        CreateAndAddNote("Note 3", tags: "important,review");

        // Act
        var result = await _repository.SearchAsync(null, "important");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WithSearchTermAndTag_CombinesBoth()
    {
        // Arrange
        CreateAndAddNote("Meeting notes", "Discuss project", tags: "important");
        CreateAndAddNote("Meeting notes", "Regular update", tags: "review");
        CreateAndAddNote("Other notes", "Discuss project", tags: "important");

        // Act
        var result = await _repository.SearchAsync("meeting", "important");

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Meeting notes");
        result[0].Tags.Should().Contain("important");
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_ReturnsAll()
    {
        // Arrange
        CreateAndAddNote("Note 1");
        CreateAndAddNote("Note 2");

        // Act
        var result = await _repository.SearchAsync(null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTagsAsync_ReturnsDistinctTagsSorted()
    {
        // Arrange
        CreateAndAddNote("Note 1", tags: "zebra,alpha");
        CreateAndAddNote("Note 2", tags: "beta,alpha");
        CreateAndAddNote("Note 3", tags: "gamma");

        // Act
        var result = await _repository.GetAllTagsAsync();

        // Assert
        result.Should().HaveCount(4);
        result.Should().BeInAscendingOrder();
        result.Should().Contain("alpha");
        result.Should().Contain("beta");
        result.Should().Contain("gamma");
        result.Should().Contain("zebra");
    }

    [Fact]
    public async Task GetAllTagsAsync_HandlesNotesWithoutTags()
    {
        // Arrange
        CreateAndAddNote("Note 1", tags: "important");
        CreateAndAddNote("Note 2"); // No tags

        // Act
        var result = await _repository.GetAllTagsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("important");
    }

    [Fact]
    public async Task AddAsync_AddsNoteToContext()
    {
        // Arrange
        var note = new ManagerNote("Test note");

        // Act
        var result = await _repository.AddAsync(note);

        // Assert
        result.Should().Be(note);
        _context.ManagerNotes.Should().ContainKey(note.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNoteInContext()
    {
        // Arrange
        var note = CreateAndAddNote("Original");
        note.Update("Updated", "New content", "retro");

        // Act
        await _repository.UpdateAsync(note);

        // Assert
        var stored = _context.ManagerNotes[note.Id];
        stored.Title.Should().Be("Updated");
        stored.Content.Should().Be("New content");
        stored.Tags.Should().Be("retro");
    }

    [Fact]
    public async Task DeleteAsync_RemovesNoteFromContext()
    {
        // Arrange
        var note = CreateAndAddNote("Test");

        // Act
        await _repository.DeleteAsync(note.Id);

        // Assert
        _context.ManagerNotes.Should().NotContainKey(note.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var note = CreateAndAddNote("Test");

        // Act
        var result = await _repository.ExistsAsync(note.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithFolderId_ReturnsOnlyThatFolder()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        CreateAndAddNote("In folder", folderId: folderId);
        CreateAndAddNote("At root");

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, null, null, folderId);

        // Assert
        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Title.Should().Be("In folder");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithoutFolderId_ReturnsNotesFromEveryFolder()
    {
        // Arrange
        CreateAndAddNote("In folder", folderId: Guid.NewGuid());
        CreateAndAddNote("At root");

        // Act
        var (_, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, null, null, null);

        // Assert
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_PutsPinnedNotesFirst()
    {
        // Arrange
        CreateAndAddNote("Ordinary");
        var pinned = CreateAndAddNote("Pinned");
        pinned.Pin();
        _context.ManagerNotes[pinned.Id] = pinned;

        // Act
        var (items, _) = await _repository.GetFilteredPagedAsync(0, 20, null, null, null);

        // Assert
        items[0].Title.Should().Be("Pinned");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_OrdersByTitleDescendingIgnoringCase()
    {
        // Arrange
        CreateAndAddNote("2026-09-09");
        CreateAndAddNote("2026-09-28");
        CreateAndAddNote("2026-09-24");
        CreateAndAddNote("beta");
        CreateAndAddNote("Alpha");

        // Act
        var (items, _) = await _repository.GetFilteredPagedAsync(0, 20, null, null, null);

        // Assert
        items.Select(n => n.Title).Should().Equal("beta", "Alpha", "2026-09-28", "2026-09-24", "2026-09-09");
    }

    [Fact]
    public async Task GetCountsByFolderAsync_CountsNotesPerFolder()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        CreateAndAddNote("One", folderId: folderId);
        CreateAndAddNote("Two", folderId: folderId);
        CreateAndAddNote("Root note");

        // Act
        var result = await _repository.GetCountsByFolderAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Single(c => c.FolderId == folderId).Count.Should().Be(2);
        result.Single(c => c.FolderId == null).Count.Should().Be(1);
    }

    [Fact]
    public async Task GetByFolderAsync_ReturnsNotesInThatFolderOnly()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        CreateAndAddNote("Inside", folderId: folderId);
        CreateAndAddNote("Outside");

        // Act
        var result = await _repository.GetByFolderAsync(folderId);

        // Assert
        result.Should().ContainSingle().Which.Title.Should().Be("Inside");
    }

    private ManagerNote CreateAndAddNote(
        string title = "Test note",
        string content = "",
        string? tags = null,
        Guid? folderId = null)
    {
        var note = new ManagerNote(title, content, tags, folderId);
        _context.ManagerNotes.TryAdd(note.Id, note);
        return note;
    }
}
