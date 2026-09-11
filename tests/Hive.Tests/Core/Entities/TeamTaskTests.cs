using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Core.Entities;

public class TeamTaskTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTask()
    {
        // Arrange
        var title = "Implement feature";
        var description = "Add new feature to API";
        var assigneeId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(7);

        // Act
        var task = new TeamTask(
            title,
            description,
            TaskType.Story,
            TaskPriority.High,
            assigneeId,
            projectId,
            dueDate,
            8,
            3,
            "api,feature");

        // Assert
        task.Id.Should().NotBeEmpty();
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.Type.Should().Be(TaskType.Story);
        task.Priority.Should().Be(TaskPriority.High);
        task.Status.Should().Be(TaskStatus.Backlog);
        task.AssigneeId.Should().Be(assigneeId);
        task.ProjectId.Should().Be(projectId);
        task.DueDate.Should().Be(dueDate);
        task.EstimatedHours.Should().Be(8);
        task.Tags.Should().Be("api,feature");
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithOnlyTitle_UsesDefaults()
    {
        // Act
        var task = new TeamTask("Simple Task");

        // Assert
        task.Type.Should().Be(TaskType.Task);
        task.Priority.Should().Be(TaskPriority.Medium);
        task.Status.Should().Be(TaskStatus.Backlog);
        task.AssigneeId.Should().BeNull();
        task.ProjectId.Should().BeNull();
        task.Description.Should().BeEmpty();
        task.Tags.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        // Act
        var act = () => new TeamTask(title!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Constructor_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 501);

        // Act
        var act = () => new TeamTask(longTitle);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var task = new TeamTask("Original");
        var dueDate = DateTime.UtcNow.AddDays(14);

        // Act
        task.Update("Updated", "New desc", TaskType.Bug, TaskPriority.Critical, dueDate, 16, 5, "bug,urgent");

        // Assert
        task.Title.Should().Be("Updated");
        task.Description.Should().Be("New desc");
        task.Type.Should().Be(TaskType.Bug);
        task.Priority.Should().Be(TaskPriority.Critical);
        task.DueDate.Should().Be(dueDate);
        task.EstimatedHours.Should().Be(16);
        task.Tags.Should().Be("bug,urgent");
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AssignTo_SetsAssigneeId()
    {
        // Arrange
        var task = new TeamTask("Task");
        var assigneeId = Guid.NewGuid();

        // Act
        task.AssignTo(assigneeId);

        // Assert
        task.AssigneeId.Should().Be(assigneeId);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AssignTo_WithNull_ClearsAssigneeId()
    {
        // Arrange
        var task = new TeamTask("Task", assigneeId: Guid.NewGuid());

        // Act
        task.AssignTo(null);

        // Assert
        task.AssigneeId.Should().BeNull();
    }

    [Fact]
    public void AssignToProject_SetsProjectId()
    {
        // Arrange
        var task = new TeamTask("Task");
        var projectId = Guid.NewGuid();

        // Act
        task.AssignToProject(projectId);

        // Assert
        task.ProjectId.Should().Be(projectId);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToBacklog_SetsStatusToBacklog()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.MoveToTodo();

        // Act
        task.MoveToBacklog();

        // Assert
        task.Status.Should().Be(TaskStatus.Backlog);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToTodo_SetsStatusToTodo()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.MoveToTodo();

        // Assert
        task.Status.Should().Be(TaskStatus.Todo);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_FromBacklog_SetsStatusToInProgress()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.Start();

        // Assert
        task.Status.Should().Be(TaskStatus.InProgress);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_FromTodo_SetsStatusToInProgress()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.MoveToTodo();

        // Act
        task.Start();

        // Assert
        task.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public void Start_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void Start_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToReview_FromInProgress_SetsStatusToInReview()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();

        // Act
        task.MoveToReview();

        // Assert
        task.Status.Should().Be(TaskStatus.InReview);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToReview_WhenNotInProgress_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        var act = () => task.MoveToReview();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*in-progress*");
    }

    [Fact]
    public void MoveToReview_FromBlocked_SetsStatusToInReview()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Block();

        // Act
        task.MoveToReview();

        // Assert
        task.Status.Should().Be(TaskStatus.InReview);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToReview_FromInTest_SetsStatusToInReview()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.MoveToTest();

        // Act
        task.MoveToReview();

        // Assert
        task.Status.Should().Be(TaskStatus.InReview);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Block_FromInProgress_SetsStatusToBlocked()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();

        // Act
        task.Block();

        // Assert
        task.Status.Should().Be(TaskStatus.Blocked);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Block_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.Block();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void Block_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.Block();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToTest_FromInReview_SetsStatusToInTest()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();
        task.MoveToReview();

        // Act
        task.MoveToTest();

        // Assert
        task.Status.Should().Be(TaskStatus.InTest);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToTest_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.MoveToTest();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToTest_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.MoveToTest();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToPOAcceptance_FromInTest_SetsStatusToPOAcceptance()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.MoveToTest();

        // Act
        task.MoveToPOAcceptance();

        // Assert
        task.Status.Should().Be(TaskStatus.POAcceptance);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToPOAcceptance_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.MoveToPOAcceptance();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToPOAcceptance_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.MoveToPOAcceptance();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToReadyToRelease_FromPOAcceptance_SetsStatusToReadyToRelease()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.MoveToPOAcceptance();

        // Act
        task.MoveToReadyToRelease();

        // Assert
        task.Status.Should().Be(TaskStatus.ReadyToRelease);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToReadyToRelease_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.MoveToReadyToRelease();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void MoveToReadyToRelease_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.MoveToReadyToRelease();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void Complete_SetsStatusToDone()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();

        // Act
        task.Complete();

        // Assert
        task.Status.Should().Be(TaskStatus.Done);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromBacklog_Succeeds()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.Complete();

        // Assert
        task.Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        var act = () => task.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();

        // Act
        task.Cancel();

        // Assert
        task.Status.Should().Be(TaskStatus.Cancelled);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenDone_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        var act = () => task.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Reopen_FromDone_SetsStatusToTodo()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Complete();

        // Act
        task.Reopen();

        // Assert
        task.Status.Should().Be(TaskStatus.Todo);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reopen_FromCancelled_SetsStatusToTodo()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Cancel();

        // Act
        task.Reopen();

        // Assert
        task.Status.Should().Be(TaskStatus.Todo);
    }

    [Fact]
    public void Reopen_WhenInProgress_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.Start();

        // Act
        var act = () => task.Reopen();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void IsOverdue_WithNoDueDate_ReturnsFalse()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act & Assert
        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenDone_ReturnsFalse()
    {
        // Arrange
        var task = new TeamTask("Task", dueDate: DateTime.UtcNow.AddDays(-1));
        task.Complete();

        // Act & Assert
        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var task = new TeamTask("Task", dueDate: DateTime.UtcNow.AddDays(-1));
        task.Cancel();

        // Act & Assert
        task.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenPastDueDate_ReturnsTrue()
    {
        // Arrange
        var task = new TeamTask("Task", dueDate: DateTime.UtcNow.AddDays(-1));

        // Act & Assert
        task.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenFutureDueDate_ReturnsFalse()
    {
        // Arrange
        var task = new TeamTask("Task", dueDate: DateTime.UtcNow.AddDays(1));

        // Act & Assert
        task.IsOverdue().Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskType.Task)]
    [InlineData(TaskType.Epic)]
    [InlineData(TaskType.Story)]
    [InlineData(TaskType.SubTask)]
    [InlineData(TaskType.Bug)]
    [InlineData(TaskType.Spike)]
    [InlineData(TaskType.Support)]
    public void Constructor_AcceptsAllTaskTypes(TaskType type)
    {
        // Act
        var task = new TeamTask("Task", type: type);

        // Assert
        task.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Critical)]
    public void Constructor_AcceptsAllPriorities(TaskPriority priority)
    {
        // Act
        var task = new TeamTask("Task", priority: priority);

        // Assert
        task.Priority.Should().Be(priority);
    }

    #region Override Tests

    [Fact]
    public void Constructor_OverriddenFields_DefaultsToEmpty()
    {
        // Act
        var task = new TeamTask("Task");

        // Assert
        task.OverriddenFields.Should().BeEmpty();
    }

    [Fact]
    public void IsFieldOverridden_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act & Assert
        task.IsFieldOverridden("AssigneeId").Should().BeFalse();
    }

    [Fact]
    public void SetOverride_MarksFieldAsOverridden()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.SetOverride("AssigneeId");

        // Assert
        task.IsFieldOverridden("AssigneeId").Should().BeTrue();
        task.OverriddenFields.Should().Be("AssigneeId");
    }

    [Fact]
    public void SetOverride_MultipleFields_CommaSeparated()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.SetOverride("AssigneeId");
        task.SetOverride("StoryPoints");

        // Assert
        task.IsFieldOverridden("AssigneeId").Should().BeTrue();
        task.IsFieldOverridden("StoryPoints").Should().BeTrue();
        task.OverriddenFields.Should().Be("AssigneeId,StoryPoints");
    }

    [Fact]
    public void SetOverride_DuplicateField_DoesNotAddTwice()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.SetOverride("AssigneeId");
        task.SetOverride("AssigneeId");

        // Assert
        task.OverriddenFields.Should().Be("AssigneeId");
    }

    [Fact]
    public void ClearOverride_RemovesField()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.SetOverride("AssigneeId");
        task.SetOverride("StoryPoints");

        // Act
        task.ClearOverride("AssigneeId");

        // Assert
        task.IsFieldOverridden("AssigneeId").Should().BeFalse();
        task.IsFieldOverridden("StoryPoints").Should().BeTrue();
    }

    [Fact]
    public void ClearOverride_NonExistentField_DoesNothing()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.SetOverride("AssigneeId");

        // Act
        task.ClearOverride("StoryPoints");

        // Assert
        task.IsFieldOverridden("AssigneeId").Should().BeTrue();
    }

    [Fact]
    public void ClearAllOverrides_RemovesAllFields()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.SetOverride("AssigneeId");
        task.SetOverride("StoryPoints");
        task.SetOverride("TimeSpentMinutes");

        // Act
        task.ClearAllOverrides();

        // Assert
        task.OverriddenFields.Should().BeEmpty();
        task.IsFieldOverridden("AssigneeId").Should().BeFalse();
        task.IsFieldOverridden("StoryPoints").Should().BeFalse();
        task.IsFieldOverridden("TimeSpentMinutes").Should().BeFalse();
    }

    [Fact]
    public void OverrideAssignee_SetsValueAndMarksField()
    {
        // Arrange
        var task = new TeamTask("Task");
        var assigneeId = Guid.NewGuid();

        // Act
        task.OverrideAssignee(assigneeId);

        // Assert
        task.AssigneeId.Should().Be(assigneeId);
        task.IsFieldOverridden("AssigneeId").Should().BeTrue();
    }

    [Fact]
    public void OverrideAssignee_WithNull_SetsNullAndMarksField()
    {
        // Arrange
        var task = new TeamTask("Task", assigneeId: Guid.NewGuid());

        // Act
        task.OverrideAssignee(null);

        // Assert
        task.AssigneeId.Should().BeNull();
        task.IsFieldOverridden("AssigneeId").Should().BeTrue();
    }

    [Fact]
    public void OverrideEstimation_SetsValuesAndMarksField()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.OverrideEstimation(5, 24);

        // Assert
        task.StoryPoints.Should().Be(5);
        task.EstimatedHours.Should().Be(24);
        task.IsFieldOverridden("StoryPoints").Should().BeTrue();
    }

    [Fact]
    public void OverrideTimeSpent_SetsValueAndMarksField()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.OverrideTimeSpent(120);

        // Assert
        task.TimeSpentMinutes.Should().Be(120);
        task.IsFieldOverridden("TimeSpentMinutes").Should().BeTrue();
    }

    [Fact]
    public void IsFieldOverridden_CaseInsensitive()
    {
        // Arrange
        var task = new TeamTask("Task");
        task.SetOverride("AssigneeId");

        // Act & Assert
        task.IsFieldOverridden("assigneeid").Should().BeTrue();
        task.IsFieldOverridden("ASSIGNEEID").Should().BeTrue();
    }

    #endregion
}
