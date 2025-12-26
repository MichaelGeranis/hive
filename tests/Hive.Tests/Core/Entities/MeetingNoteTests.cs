using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class MeetingNoteTests
{
    private readonly Guid _validMeetingId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesNote()
    {
        // Act
        var note = new MeetingNote(_validMeetingId, "Discussion point", NoteCategory.Discussion);

        // Assert
        note.Id.Should().NotBeEmpty();
        note.MeetingId.Should().Be(_validMeetingId);
        note.Content.Should().Be("Discussion point");
        note.Category.Should().Be(NoteCategory.Discussion);
        note.IsPrivate.Should().BeFalse();
        note.ActionStatus.Should().BeNull();
        note.ActionDueDate.Should().BeNull();
        note.ActionAssignee.Should().BeNull();
        note.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithActionItemCategory_SetsActionStatusToOpen()
    {
        // Act
        var note = new MeetingNote(_validMeetingId, "Follow up on task", NoteCategory.ActionItem);

        // Assert
        note.Category.Should().Be(NoteCategory.ActionItem);
        note.ActionStatus.Should().Be(ActionItemStatus.Open);
    }

    [Fact]
    public void Constructor_WithPrivateFlag_SetsIsPrivate()
    {
        // Act
        var note = new MeetingNote(_validMeetingId, "Private note", NoteCategory.Personal, isPrivate: true);

        // Assert
        note.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyMeetingId_ThrowsArgumentException()
    {
        // Act
        var act = () => new MeetingNote(Guid.Empty, "Content", NoteCategory.Discussion);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("meetingId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyContent_ThrowsArgumentException(string? content)
    {
        // Act
        var act = () => new MeetingNote(_validMeetingId, content!, NoteCategory.Discussion);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void Constructor_WithContentTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longContent = new string('a', 4001);

        // Act
        var act = () => new MeetingNote(_validMeetingId, longContent, NoteCategory.Discussion);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void UpdateContent_WithValidData_UpdatesProperties()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Original", NoteCategory.Discussion);

        // Act
        note.UpdateContent("Updated content", NoteCategory.Feedback, true);

        // Assert
        note.Content.Should().Be("Updated content");
        note.Category.Should().Be(NoteCategory.Feedback);
        note.IsPrivate.Should().BeTrue();
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_ChangingToActionItem_SetsActionStatus()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Original", NoteCategory.Discussion);

        // Act
        note.UpdateContent("New action", NoteCategory.ActionItem, false);

        // Assert
        note.ActionStatus.Should().Be(ActionItemStatus.Open);
    }

    [Fact]
    public void UpdateContent_ChangingFromActionItem_ClearsActionFields()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        note.SetActionDetails(DateTime.UtcNow.AddDays(7), "John");

        // Act
        note.UpdateContent("Discussion now", NoteCategory.Discussion, false);

        // Assert
        note.ActionStatus.Should().BeNull();
        note.ActionDueDate.Should().BeNull();
        note.ActionAssignee.Should().BeNull();
    }

    [Fact]
    public void UpdateContent_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Original", NoteCategory.Discussion);

        // Act
        var act = () => note.UpdateContent("", NoteCategory.Discussion, false);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void SetActionDetails_ForActionItem_SetsDetails()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        note.SetActionDetails(dueDate, "John Doe");

        // Assert
        note.ActionDueDate.Should().Be(dueDate);
        note.ActionAssignee.Should().Be("John Doe");
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetActionDetails_ForNonActionItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Discussion", NoteCategory.Discussion);

        // Act
        var act = () => note.SetActionDetails(DateTime.UtcNow, "John");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*action items*");
    }

    [Fact]
    public void UpdateActionStatus_ForActionItem_UpdatesStatus()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);

        // Act
        note.UpdateActionStatus(ActionItemStatus.InProgress);

        // Assert
        note.ActionStatus.Should().Be(ActionItemStatus.InProgress);
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateActionStatus_ForNonActionItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Discussion", NoteCategory.Discussion);

        // Act
        var act = () => note.UpdateActionStatus(ActionItemStatus.Completed);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*action items*");
    }

    [Fact]
    public void CompleteAction_ForActionItem_SetsStatusToCompleted()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);

        // Act
        note.CompleteAction();

        // Assert
        note.ActionStatus.Should().Be(ActionItemStatus.Completed);
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompleteAction_ForNonActionItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Discussion", NoteCategory.Discussion);

        // Act
        var act = () => note.CompleteAction();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*action items*");
    }

    [Fact]
    public void IsOverdue_ForNonActionItem_ReturnsFalse()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Discussion", NoteCategory.Discussion);

        // Act & Assert
        note.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_ForActionItemWithoutDueDate_ReturnsFalse()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);

        // Act & Assert
        note.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_ForCompletedAction_ReturnsFalse()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        note.SetActionDetails(DateTime.UtcNow.AddDays(-1), "John"); // Past due date
        note.CompleteAction();

        // Act & Assert
        note.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_ForCancelledAction_ReturnsFalse()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        note.SetActionDetails(DateTime.UtcNow.AddDays(-1), "John"); // Past due date
        note.UpdateActionStatus(ActionItemStatus.Cancelled);

        // Act & Assert
        note.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_ForOpenActionPastDueDate_ReturnsTrue()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        note.SetActionDetails(DateTime.UtcNow.AddDays(-1), "John"); // Past due date

        // Act & Assert
        note.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_ForOpenActionFutureDueDate_ReturnsFalse()
    {
        // Arrange
        var note = new MeetingNote(_validMeetingId, "Action", NoteCategory.ActionItem);
        note.SetActionDetails(DateTime.UtcNow.AddDays(1), "John"); // Future due date

        // Act & Assert
        note.IsOverdue().Should().BeFalse();
    }

    [Theory]
    [InlineData(NoteCategory.Discussion)]
    [InlineData(NoteCategory.ActionItem)]
    [InlineData(NoteCategory.Feedback)]
    [InlineData(NoteCategory.CareerDevelopment)]
    [InlineData(NoteCategory.Blocker)]
    [InlineData(NoteCategory.Achievement)]
    [InlineData(NoteCategory.Personal)]
    [InlineData(NoteCategory.FollowUp)]
    public void Constructor_AcceptsAllCategories(NoteCategory category)
    {
        // Act
        var note = new MeetingNote(_validMeetingId, "Content", category);

        // Assert
        note.Category.Should().Be(category);
    }
}
