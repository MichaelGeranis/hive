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
            description: description,
            type: TaskType.Story,
            priority: TaskPriority.High,
            assigneeId: assigneeId,
            projectId: projectId,
            dueDate: dueDate,
            storyPoints: 3,
            tags: "api,feature");

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
        task.StoryPoints.Should().Be(3);
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
        task.Update("Updated", description: "New desc", type: TaskType.Bug, priority: TaskPriority.Critical, dueDate: dueDate, storyPoints: 5, tags: "bug,urgent");

        // Assert
        task.Title.Should().Be("Updated");
        task.Description.Should().Be("New desc");
        task.Type.Should().Be(TaskType.Bug);
        task.Priority.Should().Be(TaskPriority.Critical);
        task.DueDate.Should().Be(dueDate);
        task.StoryPoints.Should().Be(5);
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

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void IsEstimated_ByStoryPoints_ReportsWhetherPointsAreSet(int? storyPoints, bool expected)
    {
        // Arrange
        var task = new TeamTask("Task", storyPoints: storyPoints);

        // Act & Assert
        task.IsEstimated().Should().Be(expected);
    }

    [Theory]
    [InlineData(TaskType.Spike, 3, true)]
    [InlineData(TaskType.Support, 1, true)]
    [InlineData(TaskType.Spike, null, false)]
    [InlineData(TaskType.Support, 0, false)]
    [InlineData(TaskType.Story, 5, false)]
    public void HasUnexpectedStoryPoints_ByTypeAndPoints_FlagsEstimatedSpikesAndSupport(
        TaskType type, int? storyPoints, bool expected)
    {
        // Arrange
        var task = new TeamTask("Task", type: type, storyPoints: storyPoints);

        // Act & Assert
        task.HasUnexpectedStoryPoints().Should().Be(expected);
    }

    [Theory]
    [InlineData(TaskType.Task, null, true)]
    [InlineData(TaskType.Story, 0, true)]
    [InlineData(TaskType.SubTask, null, true)]
    [InlineData(TaskType.Bug, null, true)]
    [InlineData(TaskType.Bug, 2, false)]
    [InlineData(TaskType.Spike, null, false)]
    [InlineData(TaskType.Support, null, false)]
    [InlineData(TaskType.Epic, null, false)]
    public void IsMissingStoryPoints_ByTypeAndPoints_FlagsUnestimatedDeliveryWork(
        TaskType type, int? storyPoints, bool expected)
    {
        // Arrange
        var task = new TeamTask("Task", type: type, storyPoints: storyPoints);

        // Act & Assert
        task.IsMissingStoryPoints().Should().Be(expected);
    }

    [Fact]
    public void IsMissingStoryPoints_ParentWhoseChildrenAllHavePoints_IsSizedByThem()
    {
        var story = new TeamTask("Story", type: TaskType.Story);

        story.IsMissingStoryPoints([new TeamTask("A", storyPoints: 2), new TeamTask("B", storyPoints: 1)], null)
            .Should().BeFalse();
    }

    [Fact]
    public void IsMissingStoryPoints_ParentWithAChildWithoutPoints_IsStillMissingThem()
    {
        var story = new TeamTask("Story", type: TaskType.Story);

        story.IsMissingStoryPoints([new TeamTask("A", storyPoints: 2), new TeamTask("B")], null)
            .Should().BeTrue();
    }

    [Fact]
    public void IsMissingStoryPoints_SpikeAndSupportChildren_DoNotHoldTheParentBack()
    {
        var story = new TeamTask("Story", type: TaskType.Story);

        story.IsMissingStoryPoints(
            [new TeamTask("A", storyPoints: 2), new TeamTask("Spike", type: TaskType.Spike), new TeamTask("Support", type: TaskType.Support)],
            null)
            .Should().BeFalse();
    }

    [Fact]
    public void IsMissingStoryPoints_OnlySpikeAndSupportChildren_DoNotSizeTheParent()
    {
        var story = new TeamTask("Story", type: TaskType.Story);

        story.IsMissingStoryPoints([new TeamTask("Spike", type: TaskType.Spike)], null).Should().BeTrue();
    }

    [Theory]
    [InlineData(TaskType.Spike, false)]
    [InlineData(TaskType.Support, false)]
    [InlineData(TaskType.Story, true)]
    public void IsMissingStoryPoints_UnderAParentTask_IsExcusedOnlyUnderSpikeOrSupport(TaskType parentType, bool expected)
    {
        var subTask = new TeamTask("Sub-task", type: TaskType.SubTask);

        subTask.IsMissingStoryPoints([], new TeamTask("Parent", type: parentType)).Should().Be(expected);
    }

    [Fact]
    public void IsMissingStoryPoints_NoChildrenNoParent_FollowsTheTaskItself()
    {
        new TeamTask("Story", type: TaskType.Story).IsMissingStoryPoints([], null).Should().BeTrue();
        new TeamTask("Story", type: TaskType.Story, storyPoints: 3).IsMissingStoryPoints([], null).Should().BeFalse();
    }

    [Fact]
    public void FindMissingStoryPoints_WorksOutChildrenAndParentsFromTheParents()
    {
        // A story linked by its task and sized by its sub-task; a support ticket, linked by
        // Jira key, whose sub-task has no points; a loose task with no points
        var member = new DirectReport("Jane", "Doe", "jane@test.com", "Engineer", "Eng", DateTime.UtcNow);
        var story = new TeamTask("Story", type: TaskType.Story, assigneeId: member.Id);
        var storyParent = new Parent("Story", teamTaskId: story.Id);
        var support = new TeamTask("Incident", type: TaskType.Support, assigneeId: member.Id, tags: "jira:MAR-9");
        var supportParent = Parent.FromJira("MAR-9", "Support", "Incident");
        var loose = new TeamTask("Loose", assigneeId: member.Id);
        var tasks = new[]
        {
            story,
            new TeamTask("Build", type: TaskType.SubTask, assigneeId: member.Id, storyPoints: 2, parentId: storyParent.Id),
            support,
            new TeamTask("Investigate", type: TaskType.SubTask, assigneeId: member.Id, parentId: supportParent.Id),
            loose
        };

        TeamTask.FindMissingStoryPoints(tasks, [storyParent, supportParent], [member]).Should().Equal(loose);
    }

    [Fact]
    public void FindMissingStoryPoints_CountsOnlyTasksAssignedToTheTeam()
    {
        var member = new DirectReport("Jane", "Doe", "jane@test.com", "Engineer", "Eng", DateTime.UtcNow);
        var mine = new TeamTask("Mine", assigneeId: member.Id);
        var tasks = new[] { mine, new TeamTask("Unassigned"), new TeamTask("Someone else's", assigneeId: Guid.NewGuid()) };

        TeamTask.FindMissingStoryPoints(tasks, [], [member]).Should().Equal(mine);
    }

    [Fact]
    public void FindMissingStoryPoints_ChildOutsideTheTeam_StillSizesItsParent()
    {
        var member = new DirectReport("Jane", "Doe", "jane@test.com", "Engineer", "Eng", DateTime.UtcNow);
        var story = new TeamTask("Story", type: TaskType.Story, assigneeId: member.Id);
        var storyParent = new Parent("Story", teamTaskId: story.Id);
        var tasks = new[] { story, new TeamTask("Sub-task", storyPoints: 2, parentId: storyParent.Id) };

        TeamTask.FindMissingStoryPoints(tasks, [storyParent], [member]).Should().BeEmpty();
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
        task.SetOverride("Sprint");

        // Act
        task.ClearAllOverrides();

        // Assert
        task.OverriddenFields.Should().BeEmpty();
        task.IsFieldOverridden("AssigneeId").Should().BeFalse();
        task.IsFieldOverridden("StoryPoints").Should().BeFalse();
        task.IsFieldOverridden("Sprint").Should().BeFalse();
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
    public void OverrideStoryPoints_SetsValueAndMarksField()
    {
        // Arrange
        var task = new TeamTask("Task");

        // Act
        task.OverrideStoryPoints(5);

        // Assert
        task.StoryPoints.Should().Be(5);
        task.IsFieldOverridden("StoryPoints").Should().BeTrue();
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

    [Theory]
    [InlineData(TaskStatus.Done, true)]
    [InlineData(TaskStatus.Cancelled, true)]
    [InlineData(TaskStatus.Todo, false)]
    [InlineData(TaskStatus.InProgress, false)]
    [InlineData(TaskStatus.Blocked, false)]
    [InlineData(TaskStatus.ReadyToRelease, false)]
    public void IsResolved_IsTrueOnlyForDoneAndCancelled(TaskStatus status, bool expected)
    {
        var task = new TeamTask("Task");
        typeof(TeamTask).GetProperty(nameof(TeamTask.Status))!.SetValue(task, status);

        task.IsResolved().Should().Be(expected);
    }

    [Theory]
    [InlineData("jira:MAR-12,imported-from-jira", "MAR-12")]
    [InlineData("urgent, JIRA:ABC-1", "ABC-1")]
    [InlineData("urgent", null)]
    [InlineData("", null)]
    public void GetJiraKey_ReadsTheKeyFromTheJiraTag(string tags, string? expected)
    {
        var task = new TeamTask("Task", tags: tags);

        task.GetJiraKey().Should().Be(expected);
    }

    [Fact]
    public void GetSprintNames_ListsEverySprintOnce()
    {
        var task = new TeamTask("Task", sprint: "MAR_3Q26_S5, MAR_3Q26_S6,mar_3q26_s6");

        task.GetSprintNames().Should().Equal("MAR_3Q26_S5", "MAR_3Q26_S6");
    }

    [Theory]
    [InlineData("MAR_3Q26_S6", "MAR_3Q26_S6", true)]
    [InlineData("MAR_3Q26_S5,MAR_3Q26_S6", "MAR_3Q26_S6", true)]
    [InlineData("MAR_3Q26_S5,MAR_3Q26_S6", "MAR_3Q26_S5", true)]
    [InlineData("MAR_3Q26_S5, MAR_3Q26_S6", " mar_3q26_s6 ", true)]
    [InlineData("MAR_3Q26_S16", "MAR_3Q26_S1", false)]
    [InlineData("", "MAR_3Q26_S6", false)]
    public void IsInSprint_MatchesAnyOfTheTasksSprints(string sprints, string sprint, bool expected)
    {
        var task = new TeamTask("Task", sprint: sprints);

        task.IsInSprint(sprint).Should().Be(expected);
    }

    [Fact]
    public void Constructor_StartsTheStatusClockAtCreation()
    {
        var task = new TeamTask("Task");

        task.StatusChangedAt.Should().Be(task.CreatedAt);
    }

    [Fact]
    public void StatusChange_MovesTheStatusClock()
    {
        var task = new TeamTask("Task");
        SetStatusChangedAt(task, DateTime.UtcNow.AddDays(-10));

        task.Start();

        task.StatusChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SameStatusAgain_KeepsTheStatusClock()
    {
        var task = new TeamTask("Task");
        task.Start();
        var since = DateTime.UtcNow.AddDays(-10);
        SetStatusChangedAt(task, since);

        // A sync that finds the task where it was does not restart the clock
        task.Start();

        task.StatusChangedAt.Should().Be(since);
    }

    [Fact]
    public void Update_WithoutAStatusChange_KeepsTheStatusClock()
    {
        var task = new TeamTask("Task");
        task.Start();
        var since = DateTime.UtcNow.AddDays(-10);
        SetStatusChangedAt(task, since);

        task.Update("Renamed", null, TaskType.Task, TaskPriority.High, null, 3, null);

        task.StatusChangedAt.Should().Be(since);
    }

    [Theory]
    [InlineData(TaskStatus.InProgress, 6, true)]
    [InlineData(TaskStatus.Blocked, 5, true)]
    [InlineData(TaskStatus.InReview, 4, false)]
    [InlineData(TaskStatus.Todo, 30, false)]
    [InlineData(TaskStatus.Backlog, 30, false)]
    [InlineData(TaskStatus.Done, 30, false)]
    public void IsStale_InFlightAndUnchangedForTheThreshold(TaskStatus status, int daysInStatus, bool expected)
    {
        var task = new TeamTask("Task");
        MoveTo(task, status);
        var now = DateTime.UtcNow;
        SetStatusChangedAt(task, now.AddDays(-daysInStatus));

        task.IsStale(now, staleAfterDays: 5).Should().Be(expected);
    }

    [Fact]
    public void DaysInStatus_CountsWholeDaysSinceTheChange()
    {
        var task = new TeamTask("Task");
        var now = DateTime.UtcNow;
        SetStatusChangedAt(task, now.AddDays(-3).AddHours(-5));

        task.DaysInStatus(now).Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void ValidateStaleAfterDays_OutOfRange_Throws(int days)
    {
        var act = () => TeamTask.ValidateStaleAfterDays(days);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Rights Manager,Matching", "matching", true)]
    [InlineData("Rights Manager, Matching ", "Matching", true)]
    [InlineData("Rights Manager", "Rights", false)]
    [InlineData(null, "Matching", false)]
    public void HasComponent_MatchesAWholeComponentIgnoringCase(string? components, string component, bool expected)
    {
        var task = new TeamTask("Task", components: components);

        task.HasComponent(component).Should().Be(expected);
    }

    [Theory]
    [InlineData("support,urgent", "Support", true)]
    [InlineData("support-l2", "support", false)]
    public void HasLabel_MatchesAWholeLabelIgnoringCase(string labels, string label, bool expected)
    {
        var task = new TeamTask("Task", labels: labels);

        task.HasLabel(label).Should().Be(expected);
    }

    private static void MoveTo(TeamTask task, TaskStatus status)
    {
        switch (status)
        {
            case TaskStatus.Todo: task.MoveToTodo(); break;
            case TaskStatus.InProgress: task.Start(); break;
            case TaskStatus.Blocked: task.Block(); break;
            case TaskStatus.InReview: task.Start(); task.MoveToReview(); break;
            case TaskStatus.Done: task.Complete(); break;
        }
    }

    private static void SetStatusChangedAt(TeamTask task, DateTime value) =>
        typeof(TeamTask).GetProperty(nameof(TeamTask.StatusChangedAt))!.SetValue(task, value);

    [Theory]
    [InlineData("## Acceptance criteria\n- Saves the draft")]
    [InlineData("### acceptance criterion")]
    [InlineData("**Acceptance Criteria:**\n1. Works")]
    [InlineData("Context first.\n\nAC:\n- Works")]
    [InlineData("**AC**\n- Works")]
    [InlineData("Given a draft\nWhen I save\nThen it is kept")]
    public void HasAcceptanceCriteria_SectionOrScenario_IsTrue(string description)
    {
        new TeamTask("Story", description, TaskType.Story).HasAcceptanceCriteria().Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("As a user I want to save a draft.")]
    [InlineData("We need access control.")]
    [InlineData("The acceptance of criteria is up to the PO.")]
    [InlineData("Given enough time we will do it.")]
    public void HasAcceptanceCriteria_Without_IsFalse(string description)
    {
        new TeamTask("Story", description, TaskType.Story).HasAcceptanceCriteria().Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskType.Story, true)]
    [InlineData(TaskType.Task, true)]
    [InlineData(TaskType.Bug, false)]
    [InlineData(TaskType.Spike, false)]
    [InlineData(TaskType.Support, false)]
    [InlineData(TaskType.SubTask, false)]
    [InlineData(TaskType.Epic, false)]
    public void IsMissingAcceptanceCriteria_OnlyStoriesAndTasksNeedThem(TaskType type, bool expected)
    {
        new TeamTask("Work", "No criteria here", type).IsMissingAcceptanceCriteria().Should().Be(expected);
    }

    [Fact]
    public void IsNotStarted_BacklogAndToDo_NotInProgress()
    {
        var task = new TeamTask("Work");
        task.IsNotStarted().Should().BeTrue();

        task.MoveToTodo();
        task.IsNotStarted().Should().BeTrue();

        task.Start();
        task.IsNotStarted().Should().BeFalse();
    }
}
