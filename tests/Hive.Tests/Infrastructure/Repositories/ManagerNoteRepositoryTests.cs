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
    public async Task GetPendingAsync_ReturnsOnlyIncompleteNotes()
    {
        // Arrange
        CreateAndAddNote("Pending 1");
        var completed = CreateAndAddNote("Completed");
        completed.MarkComplete();
        _context.ManagerNotes[completed.Id] = completed;
        CreateAndAddNote("Pending 2");

        // Act
        var result = await _repository.GetPendingAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.IsCompleted.Should().BeFalse());
    }

    [Fact]
    public async Task GetPendingAsync_OrdersByPriorityThenDueDateThenCreatedAt()
    {
        // Arrange
        var tomorrow = DateTime.UtcNow.AddDays(1);
        var nextWeek = DateTime.UtcNow.AddDays(7);

        // Urgent with due date
        var note1 = CreateAndAddNote("Urgent soon", priority: NotePriority.Urgent, dueDate: tomorrow);
        await Task.Delay(10);

        // Urgent with later due date
        var note2 = CreateAndAddNote("Urgent later", priority: NotePriority.Urgent, dueDate: nextWeek);
        await Task.Delay(10);

        // High priority
        var note3 = CreateAndAddNote("High priority", priority: NotePriority.High);
        await Task.Delay(10);

        // Normal priority
        var note4 = CreateAndAddNote("Normal", priority: NotePriority.Normal);

        // Act
        var result = await _repository.GetPendingAsync();

        // Assert
        result[0].Id.Should().Be(note1.Id); // Urgent, due tomorrow
        result[1].Id.Should().Be(note2.Id); // Urgent, due next week
        result[2].Id.Should().Be(note3.Id); // High priority
        result[3].Id.Should().Be(note4.Id); // Normal priority
    }

    [Fact]
    public async Task GetCompletedAsync_ReturnsOnlyCompletedNotes()
    {
        // Arrange
        CreateAndAddNote("Pending");
        var completed1 = CreateAndAddNote("Completed 1");
        completed1.MarkComplete();
        _context.ManagerNotes[completed1.Id] = completed1;

        await Task.Delay(10);
        var completed2 = CreateAndAddNote("Completed 2");
        completed2.MarkComplete();
        _context.ManagerNotes[completed2.Id] = completed2;

        // Act
        var result = await _repository.GetCompletedAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.IsCompleted.Should().BeTrue());
    }

    [Fact]
    public async Task GetCompletedAsync_OrdersByCompletedAtDescending()
    {
        // Arrange
        var note1 = CreateAndAddNote("First completed");
        note1.MarkComplete();
        _context.ManagerNotes[note1.Id] = note1;

        await Task.Delay(10);
        var note2 = CreateAndAddNote("Second completed");
        note2.MarkComplete();
        _context.ManagerNotes[note2.Id] = note2;

        // Act
        var result = await _repository.GetCompletedAsync();

        // Assert
        result[0].Id.Should().Be(note2.Id); // Most recently completed first
        result[1].Id.Should().Be(note1.Id);
    }

    [Fact]
    public async Task GetOverdueAsync_ReturnsOnlyOverdueNotes()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        // Overdue note
        CreateAndAddNote("Overdue", dueDate: yesterday);

        // Future note
        CreateAndAddNote("Future", dueDate: tomorrow);

        // Completed overdue note (should not be included)
        var completedOverdue = CreateAndAddNote("Completed overdue", dueDate: yesterday);
        completedOverdue.MarkComplete();
        _context.ManagerNotes[completedOverdue.Id] = completedOverdue;

        // Note without due date
        CreateAndAddNote("No due date");

        // Act
        var result = await _repository.GetOverdueAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Overdue");
    }

    [Fact]
    public async Task GetOverdueAsync_OrdersByDueDateAscending()
    {
        // Arrange
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var note1 = CreateAndAddNote("Older overdue", dueDate: twoDaysAgo);
        var note2 = CreateAndAddNote("Recent overdue", dueDate: yesterday);

        // Act
        var result = await _repository.GetOverdueAsync();

        // Assert
        result[0].Id.Should().Be(note1.Id); // Oldest overdue first
        result[1].Id.Should().Be(note2.Id);
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
    public async Task GetByTagAsync_OrdersByPriorityThenCreatedAt()
    {
        // Arrange
        var note1 = CreateAndAddNote("Normal", tags: "test", priority: NotePriority.Normal);
        await Task.Delay(10);
        var note2 = CreateAndAddNote("High", tags: "test", priority: NotePriority.High);
        await Task.Delay(10);
        var note3 = CreateAndAddNote("Urgent", tags: "test", priority: NotePriority.Urgent);

        // Act
        var result = await _repository.GetByTagAsync("test");

        // Assert
        result[0].Id.Should().Be(note3.Id); // Urgent first
        result[1].Id.Should().Be(note2.Id); // High second
        result[2].Id.Should().Be(note1.Id); // Normal last
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
        note.Update("Updated", "New content", NotePriority.High, null);

        // Act
        await _repository.UpdateAsync(note);

        // Assert
        var stored = _context.ManagerNotes[note.Id];
        stored.Title.Should().Be("Updated");
        stored.Content.Should().Be("New content");
        stored.Priority.Should().Be(NotePriority.High);
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
    public async Task GetPendingAsync_ExcludesNotesThatAreNotTodos()
    {
        // Arrange
        CreateAndAddNote("A plain note", isTodo: false);
        CreateAndAddNote("An open to-do");

        // Act
        var result = await _repository.GetPendingAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("An open to-do");
    }

    [Fact]
    public async Task GetOverdueAsync_ExcludesNotesThatAreNotTodos()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        CreateAndAddNote("Plain note with a date", dueDate: yesterday, isTodo: false);
        CreateAndAddNote("Late to-do", dueDate: yesterday);

        // Act
        var result = await _repository.GetOverdueAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Late to-do");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithFolderId_ReturnsOnlyThatFolder()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        CreateAndAddNote("In folder", folderId: folderId);
        CreateAndAddNote("At root");

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, null, null, null, folderId);

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
    public async Task GetFilteredPagedAsync_SortedByRecent_PutsPinnedNotesFirst()
    {
        // Arrange
        CreateAndAddNote("Ordinary");
        var pinned = CreateAndAddNote("Pinned");
        pinned.Pin();
        _context.ManagerNotes[pinned.Id] = pinned;

        // Act
        var (items, _) = await _repository.GetFilteredPagedAsync(0, 20, null, null, null, null, NoteSortOrder.Recent);

        // Assert
        items[0].Title.Should().Be("Pinned");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_PendingFilter_ExcludesNotesThatAreNotTodos()
    {
        // Arrange
        CreateAndAddNote("Plain note", isTodo: false);
        CreateAndAddNote("Open to-do");

        // Act
        var (items, _) = await _repository.GetFilteredPagedAsync(0, 20, "pending", null, null);

        // Assert
        items.Should().ContainSingle().Which.Title.Should().Be("Open to-do");
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
        NotePriority priority = NotePriority.Normal,
        DateTime? dueDate = null,
        string? tags = null,
        Guid? folderId = null,
        bool isTodo = true)
    {
        var note = new ManagerNote(title, content, priority, dueDate, tags, folderId, isTodo);
        _context.ManagerNotes.TryAdd(note.Id, note);
        return note;
    }
}
