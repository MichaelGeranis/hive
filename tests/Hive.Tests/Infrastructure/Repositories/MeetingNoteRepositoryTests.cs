using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class MeetingNoteRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly MeetingNoteRepository _repository;
    private readonly Guid _meetingId;
    private readonly Guid _directReportId;

    public MeetingNoteRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new MeetingNoteRepository(_context);
        _meetingId = Guid.NewGuid();
        _directReportId = Guid.NewGuid();

        // Add a meeting to the context for testing
        var meeting = new OneOnOneMeeting(_directReportId, DateOnly.FromDateTime(DateTime.UtcNow));
        _context.OneOnOneMeetings.TryAdd(meeting.Id, meeting);
        _meetingId = meeting.Id;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MeetingNoteRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var note = CreateAndAddNote();

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
    public async Task GetByMeetingIdAsync_ReturnsMatchingNotes()
    {
        // Arrange
        var otherMeetingId = Guid.NewGuid();
        CreateAndAddNote();
        CreateAndAddNote();
        CreateAndAddNote(meetingId: otherMeetingId);

        // Act
        var result = await _repository.GetByMeetingIdAsync(_meetingId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.MeetingId.Should().Be(_meetingId));
    }

    [Fact]
    public async Task GetByMeetingIdAsync_ReturnsOrderedByCreatedAt()
    {
        // Arrange
        var note1 = CreateAndAddNote("First note");
        await Task.Delay(10); // Ensure different timestamps
        var note2 = CreateAndAddNote("Second note");
        await Task.Delay(10);
        var note3 = CreateAndAddNote("Third note");

        // Act
        var result = await _repository.GetByMeetingIdAsync(_meetingId);

        // Assert
        result[0].Id.Should().Be(note1.Id);
        result[1].Id.Should().Be(note2.Id);
        result[2].Id.Should().Be(note3.Id);
    }

    [Fact]
    public async Task GetByMeetingIdAsync_WithIncludePrivateFalse_ExcludesPrivateNotes()
    {
        // Arrange
        CreateAndAddNote("Public note", isPrivate: false);
        CreateAndAddNote("Private note", isPrivate: true);

        // Act
        var result = await _repository.GetByMeetingIdAsync(_meetingId, includePrivate: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsPrivate.Should().BeFalse();
    }

    [Fact]
    public async Task GetByMeetingIdAsync_WithIncludePrivateTrue_IncludesAllNotes()
    {
        // Arrange
        CreateAndAddNote("Public note", isPrivate: false);
        CreateAndAddNote("Private note", isPrivate: true);

        // Act
        var result = await _repository.GetByMeetingIdAsync(_meetingId, includePrivate: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActionItemsAsync_ReturnsOnlyActionItems()
    {
        // Arrange
        CreateAndAddNote("Discussion", category: NoteCategory.Discussion);
        CreateAndAddNote("Action", category: NoteCategory.ActionItem);
        CreateAndAddNote("Feedback", category: NoteCategory.Feedback);
        CreateAndAddNote("Another Action", category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetActionItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.Category.Should().Be(NoteCategory.ActionItem));
    }

    [Fact]
    public async Task GetActionItemsAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var note1 = CreateAndAddNote("First action", category: NoteCategory.ActionItem);
        await Task.Delay(10);
        var note2 = CreateAndAddNote("Second action", category: NoteCategory.ActionItem);
        await Task.Delay(10);
        var note3 = CreateAndAddNote("Third action", category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetActionItemsAsync();

        // Assert
        result[0].Id.Should().Be(note3.Id); // Most recent first
        result[1].Id.Should().Be(note2.Id);
        result[2].Id.Should().Be(note1.Id);
    }

    [Fact]
    public async Task GetActionItemsAsync_WithDirectReportId_FiltersCorrectly()
    {
        // Arrange
        var directReport1 = Guid.NewGuid();
        var directReport2 = Guid.NewGuid();

        var meeting1 = new OneOnOneMeeting(directReport1, DateOnly.FromDateTime(DateTime.UtcNow));
        var meeting2 = new OneOnOneMeeting(directReport2, DateOnly.FromDateTime(DateTime.UtcNow));
        _context.OneOnOneMeetings.TryAdd(meeting1.Id, meeting1);
        _context.OneOnOneMeetings.TryAdd(meeting2.Id, meeting2);

        CreateAndAddNote("Action 1", meetingId: meeting1.Id, category: NoteCategory.ActionItem);
        CreateAndAddNote("Action 2", meetingId: meeting1.Id, category: NoteCategory.ActionItem);
        CreateAndAddNote("Action 3", meetingId: meeting2.Id, category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetActionItemsAsync(directReport1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOpenActionItemsAsync_ExcludesCompletedAndCancelled()
    {
        // Arrange
        var openAction = CreateAndAddNote("Open", category: NoteCategory.ActionItem);

        var inProgressAction = CreateAndAddNote("In Progress", category: NoteCategory.ActionItem);
        inProgressAction.UpdateActionStatus(ActionItemStatus.InProgress);
        _context.MeetingNotes[inProgressAction.Id] = inProgressAction;

        var completedAction = CreateAndAddNote("Completed", category: NoteCategory.ActionItem);
        completedAction.CompleteAction();
        _context.MeetingNotes[completedAction.Id] = completedAction;

        var cancelledAction = CreateAndAddNote("Cancelled", category: NoteCategory.ActionItem);
        cancelledAction.UpdateActionStatus(ActionItemStatus.Cancelled);
        _context.MeetingNotes[cancelledAction.Id] = cancelledAction;

        // Act
        var result = await _repository.GetOpenActionItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Id == openAction.Id);
        result.Should().Contain(n => n.Id == inProgressAction.Id);
    }

    [Fact]
    public async Task GetOpenActionItemsAsync_OrdersByDueDateThenCreatedAt()
    {
        // Arrange
        var tomorrow = DateTime.UtcNow.AddDays(1);
        var nextWeek = DateTime.UtcNow.AddDays(7);

        var note1 = CreateAndAddNote("Due next week", category: NoteCategory.ActionItem);
        note1.SetActionDetails(nextWeek, null);
        _context.MeetingNotes[note1.Id] = note1;

        await Task.Delay(10);
        var note2 = CreateAndAddNote("Due tomorrow", category: NoteCategory.ActionItem);
        note2.SetActionDetails(tomorrow, null);
        _context.MeetingNotes[note2.Id] = note2;

        await Task.Delay(10);
        var note3 = CreateAndAddNote("No due date", category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetOpenActionItemsAsync();

        // Assert
        result[0].Id.Should().Be(note2.Id); // Due tomorrow first
        result[1].Id.Should().Be(note1.Id); // Due next week second
        result[2].Id.Should().Be(note3.Id); // No due date last
    }

    [Fact]
    public async Task GetOpenActionItemsAsync_WithDirectReportId_FiltersCorrectly()
    {
        // Arrange
        var directReport1 = Guid.NewGuid();
        var directReport2 = Guid.NewGuid();

        var meeting1 = new OneOnOneMeeting(directReport1, DateOnly.FromDateTime(DateTime.UtcNow));
        var meeting2 = new OneOnOneMeeting(directReport2, DateOnly.FromDateTime(DateTime.UtcNow));
        _context.OneOnOneMeetings.TryAdd(meeting1.Id, meeting1);
        _context.OneOnOneMeetings.TryAdd(meeting2.Id, meeting2);

        CreateAndAddNote("Open 1", meetingId: meeting1.Id, category: NoteCategory.ActionItem);
        CreateAndAddNote("Open 2", meetingId: meeting1.Id, category: NoteCategory.ActionItem);
        CreateAndAddNote("Open 3", meetingId: meeting2.Id, category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetOpenActionItemsAsync(directReport1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverdueActionItemsAsync_ReturnsOnlyOverdueItems()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        // Overdue action
        var overdueNote = CreateAndAddNote("Overdue", category: NoteCategory.ActionItem);
        overdueNote.SetActionDetails(yesterday, null);
        _context.MeetingNotes[overdueNote.Id] = overdueNote;

        // Future action
        var futureNote = CreateAndAddNote("Future", category: NoteCategory.ActionItem);
        futureNote.SetActionDetails(tomorrow, null);
        _context.MeetingNotes[futureNote.Id] = futureNote;

        // Completed overdue action (should not be included)
        var completedNote = CreateAndAddNote("Completed", category: NoteCategory.ActionItem);
        completedNote.SetActionDetails(yesterday, null);
        completedNote.CompleteAction();
        _context.MeetingNotes[completedNote.Id] = completedNote;

        // Action without due date
        CreateAndAddNote("No date", category: NoteCategory.ActionItem);

        // Act
        var result = await _repository.GetOverdueActionItemsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(overdueNote.Id);
    }

    [Fact]
    public async Task GetOverdueActionItemsAsync_OrdersByDueDate()
    {
        // Arrange
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var note1 = CreateAndAddNote("Older overdue", category: NoteCategory.ActionItem);
        note1.SetActionDetails(twoDaysAgo, null);
        _context.MeetingNotes[note1.Id] = note1;

        var note2 = CreateAndAddNote("Recent overdue", category: NoteCategory.ActionItem);
        note2.SetActionDetails(yesterday, null);
        _context.MeetingNotes[note2.Id] = note2;

        // Act
        var result = await _repository.GetOverdueActionItemsAsync();

        // Assert
        result[0].Id.Should().Be(note1.Id); // Oldest overdue first
        result[1].Id.Should().Be(note2.Id);
    }

    [Fact]
    public async Task AddAsync_AddsNoteToContext()
    {
        // Arrange
        var note = new MeetingNote(_meetingId, "Test note", NoteCategory.Discussion);

        // Act
        var result = await _repository.AddAsync(note);

        // Assert
        result.Should().Be(note);
        _context.MeetingNotes.Should().ContainKey(note.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var note = CreateAndAddNote();

        // Act
        var act = () => _repository.AddAsync(note);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNoteInContext()
    {
        // Arrange
        var note = CreateAndAddNote();
        note.UpdateContent("Updated content", NoteCategory.Feedback, false);

        // Act
        await _repository.UpdateAsync(note);

        // Assert
        var stored = _context.MeetingNotes[note.Id];
        stored.Content.Should().Be("Updated content");
        stored.Category.Should().Be(NoteCategory.Feedback);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentNote_ThrowsInvalidOperationException()
    {
        // Arrange
        var note = new MeetingNote(_meetingId, "Test", NoteCategory.Discussion);

        // Act
        var act = () => _repository.UpdateAsync(note);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesNoteFromContext()
    {
        // Arrange
        var note = CreateAndAddNote();

        // Act
        await _repository.DeleteAsync(note.Id);

        // Assert
        _context.MeetingNotes.Should().NotContainKey(note.Id);
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
        var note = CreateAndAddNote();

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

    private MeetingNote CreateAndAddNote(
        string content = "Test note",
        Guid? meetingId = null,
        NoteCategory category = NoteCategory.Discussion,
        bool isPrivate = false)
    {
        var note = new MeetingNote(
            meetingId ?? _meetingId,
            content,
            category,
            isPrivate);
        _context.MeetingNotes.TryAdd(note.Id, note);
        return note;
    }
}
