using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ChecklistInstanceItemTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var templateItemId = Guid.NewGuid();
        var sortOrder = 1;
        var content = "Complete this task";
        var itemType = ChecklistItemType.Task;
        var isRequired = true;

        // Act
        var item = new ChecklistInstanceItem(
            instanceId, templateItemId, sortOrder, content, itemType, isRequired);

        // Assert
        item.Id.Should().NotBeEmpty();
        item.InstanceId.Should().Be(instanceId);
        item.TemplateItemId.Should().Be(templateItemId);
        item.SortOrder.Should().Be(sortOrder);
        item.Content.Should().Be(content);
        item.ItemType.Should().Be(itemType);
        item.IsRequired.Should().Be(isRequired);
        item.Status.Should().Be(ChecklistItemStatus.Pending);
        item.Notes.Should().BeEmpty();
        item.Score.Should().BeNull();
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyInstanceId_ThrowsArgumentException()
    {
        // Act
        var act = () => new ChecklistInstanceItem(
            Guid.Empty, Guid.NewGuid(), 0, "Content", ChecklistItemType.Task, true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("instanceId");
    }

    [Fact]
    public void MarkInProgress_FromPending_ChangesToInProgress()
    {
        // Arrange
        var item = CreateItem();

        // Act
        item.MarkInProgress();

        // Assert
        item.Status.Should().Be(ChecklistItemStatus.InProgress);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkInProgress_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var item = CreateItem();
        item.MarkComplete();

        // Act
        var act = () => item.MarkInProgress();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkComplete_FromPending_ChangesToCompleted()
    {
        // Arrange
        var item = CreateItem();

        // Act
        item.MarkComplete();

        // Assert
        item.Status.Should().Be(ChecklistItemStatus.Completed);
        item.CompletedAt.Should().NotBeNull();
        item.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkComplete_WithNotesAndScore_SetsValues()
    {
        // Arrange
        var item = CreateItem();
        var notes = "Completed successfully";
        var score = 4;

        // Act
        item.MarkComplete(notes, score);

        // Assert
        item.Status.Should().Be(ChecklistItemStatus.Completed);
        item.Notes.Should().Be(notes);
        item.Score.Should().Be(score);
    }

    [Fact]
    public void MarkSkipped_ForNonRequiredItem_ChangesToSkipped()
    {
        // Arrange
        var item = new ChecklistInstanceItem(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Content", ChecklistItemType.Task, false);

        // Act
        item.MarkSkipped("Not needed");

        // Assert
        item.Status.Should().Be(ChecklistItemStatus.Skipped);
        item.Notes.Should().Be("Not needed");
    }

    [Fact]
    public void MarkSkipped_ForRequiredItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var item = CreateItem(); // Required by default

        // Act
        var act = () => item.MarkSkipped();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkNotApplicable_ChangesToNotApplicable()
    {
        // Arrange
        var item = CreateItem();

        // Act
        item.MarkNotApplicable("Reason here");

        // Assert
        item.Status.Should().Be(ChecklistItemStatus.NotApplicable);
        item.Notes.Should().Be("Reason here");
    }

    [Fact]
    public void SetAssignee_SetsAssigneeAndDueDate()
    {
        // Arrange
        var item = CreateItem();
        var assignee = "John Doe";
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        item.SetAssignee(assignee, dueDate);

        // Assert
        item.Assignee.Should().Be(assignee);
        item.DueDate.Should().Be(dueDate);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetAssignee_WithNullAssignee_ClearsAssignee()
    {
        // Arrange
        var item = CreateItem();
        item.SetAssignee("John Doe");

        // Act
        item.SetAssignee(null);

        // Assert
        item.Assignee.Should().BeNull();
    }

    [Fact]
    public void UpdateNotes_UpdatesNotesProperty()
    {
        // Arrange
        var item = CreateItem();
        var notes = "Some notes here";

        // Act
        item.UpdateNotes(notes);

        // Assert
        item.Notes.Should().Be(notes);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetScore_WithValidScore_SetsScore()
    {
        // Arrange
        var item = CreateItem();

        // Act
        item.SetScore(4);

        // Assert
        item.Score.Should().Be(4);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void SetScore_WithInvalidScore_ThrowsArgumentException(int score)
    {
        // Arrange
        var item = CreateItem();

        // Act
        var act = () => item.SetScore(score);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("score");
    }

    [Fact]
    public void IsOverdue_WhenPastDueDate_ReturnsTrue()
    {
        // Arrange
        var item = CreateItem();
        item.SetAssignee("John", DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        item.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenBeforeDueDate_ReturnsFalse()
    {
        // Arrange
        var item = CreateItem();
        item.SetAssignee("John", DateTime.UtcNow.AddDays(1));

        // Act & Assert
        item.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ReturnsFalse()
    {
        // Arrange
        var item = CreateItem();
        item.SetAssignee("John", DateTime.UtcNow.AddDays(-1));
        item.MarkComplete();

        // Act & Assert
        item.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenSkipped_ReturnsFalse()
    {
        // Arrange
        var item = new ChecklistInstanceItem(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Content", ChecklistItemType.Task, false);
        item.SetAssignee("John", DateTime.UtcNow.AddDays(-1));
        item.MarkSkipped();

        // Act & Assert
        item.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenNoDueDate_ReturnsFalse()
    {
        // Arrange
        var item = CreateItem();

        // Act & Assert
        item.IsOverdue().Should().BeFalse();
    }

    private static ChecklistInstanceItem CreateItem()
    {
        return new ChecklistInstanceItem(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Content", ChecklistItemType.Task, true);
    }
}
