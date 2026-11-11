using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Infrastructure.Repositories;

public class TeamTaskRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly TeamTaskRepository _repository;

    public TeamTaskRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new TeamTaskRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TeamTaskRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var task = CreateAndAddTask();

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
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
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        // Arrange
        CreateAndAddTask("Task 1");
        CreateAndAddTask("Task 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByAssigneeIdAsync_ReturnsMatchingTasks()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        CreateAndAddTask("Task 1", assigneeId: assigneeId);
        CreateAndAddTask("Task 2", assigneeId: Guid.NewGuid());

        // Act
        var result = await _repository.GetByAssigneeIdAsync(assigneeId);

        // Assert
        result.Should().HaveCount(1);
        result[0].AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsMatchingTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        CreateAndAddTask("Task 1", projectId: projectId);
        CreateAndAddTask("Task 2", projectId: Guid.NewGuid());

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        result.Should().HaveCount(1);
        result[0].ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMatchingTasks()
    {
        // Arrange
        var task1 = CreateAndAddTask("Backlog Task");
        var task2 = CreateAndAddTask("In Progress Task");
        task2.Start();
        _context.TeamTasks[task2.Id] = task2;

        // Act
        var result = await _repository.GetByStatusAsync(TaskStatus.InProgress);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task GetByPriorityAsync_ReturnsMatchingTasks()
    {
        // Arrange
        CreateAndAddTask("Low Priority", priority: TaskPriority.Low);
        CreateAndAddTask("High Priority", priority: TaskPriority.High);

        // Act
        var result = await _repository.GetByPriorityAsync(TaskPriority.High);

        // Assert
        result.Should().HaveCount(1);
        result[0].Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task GetOverdueAsync_ReturnsOverdueTasks()
    {
        // Arrange
        CreateAndAddTask("Overdue Task", dueDate: DateTime.UtcNow.AddDays(-1));
        CreateAndAddTask("Future Task", dueDate: DateTime.UtcNow.AddDays(1));

        // Act
        var result = await _repository.GetOverdueAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].IsOverdue().Should().BeTrue();
    }

    [Fact]
    public async Task GetUnassignedAsync_ReturnsUnassignedOpenTasks()
    {
        // Arrange
        CreateAndAddTask("Unassigned", assigneeId: null);
        CreateAndAddTask("Assigned", assigneeId: Guid.NewGuid());
        var doneTask = CreateAndAddTask("Done Unassigned", assigneeId: null);
        doneTask.Complete();
        _context.TeamTasks[doneTask.Id] = doneTask;

        // Act
        var result = await _repository.GetUnassignedAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].AssigneeId.Should().BeNull();
        result[0].Status.Should().NotBe(TaskStatus.Done);
    }

    [Fact]
    public async Task AddAsync_AddsTaskToContext()
    {
        // Arrange
        var task = new TeamTask("New Task");

        // Act
        var result = await _repository.AddAsync(task);

        // Assert
        result.Should().Be(task);
        _context.TeamTasks.Should().ContainKey(task.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = CreateAndAddTask();

        // Act
        var act = () => _repository.AddAsync(task);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTaskInContext()
    {
        // Arrange
        var task = CreateAndAddTask();
        task.Update("Updated", description: "Desc", type: TaskType.Bug, priority: TaskPriority.Critical, dueDate: null, storyPoints: null, tags: null);

        // Act
        await _repository.UpdateAsync(task);

        // Assert
        var stored = _context.TeamTasks[task.Id];
        stored.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTask_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new TeamTask("Test");

        // Act
        var act = () => _repository.UpdateAsync(task);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskFromContext()
    {
        // Arrange
        var task = CreateAndAddTask();

        // Act
        await _repository.DeleteAsync(task.Id);

        // Assert
        _context.TeamTasks.Should().NotContainKey(task.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var task = CreateAndAddTask();

        // Act
        var result = await _repository.ExistsAsync(task.Id);

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

    private TeamTask CreateAndAddTask(
        string title = "Test Task",
        Guid? assigneeId = null,
        Guid? projectId = null,
        DateTime? dueDate = null,
        TaskPriority priority = TaskPriority.Medium)
    {
        var task = new TeamTask(title, description: null, type: TaskType.Task, priority: priority, assigneeId: assigneeId, projectId: projectId, dueDate: dueDate);
        _context.TeamTasks.TryAdd(task.Id, task);
        return task;
    }

    [Fact]
    public async Task GetFilteredPagedAsync_BySprint_IncludesWorkCarriedOverIntoIt()
    {
        // Arrange
        await _repository.AddAsync(new TeamTask("New", sprint: "MAR_3Q26_S6"));
        await _repository.AddAsync(new TeamTask("Carried", sprint: "MAR_3Q26_S5,MAR_3Q26_S6"));
        await _repository.AddAsync(new TeamTask("Earlier", sprint: "MAR_3Q26_S5"));
        await _repository.AddAsync(new TeamTask("Look-alike", sprint: "MAR_3Q26_S16"));

        // Act
        var (items, total, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter { Sprint = "MAR_3Q26_S6" });

        // Assert
        total.Should().Be(2);
        items.Select(t => t.Title).Should().BeEquivalentTo("New", "Carried");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Stale_KeepsInFlightTasksUnchangedSinceTheCutoff()
    {
        var now = DateTime.UtcNow;
        await _repository.AddAsync(InStatus("Stuck", t => t.Start(), now.AddDays(-8)));
        await _repository.AddAsync(InStatus("Moving", t => t.Start(), now.AddDays(-1)));
        await _repository.AddAsync(InStatus("Parked", t => t.MoveToTodo(), now.AddDays(-30)));

        var (items, _, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter { StaleBefore = now.AddDays(-5) });

        items.Select(t => t.Title).Should().BeEquivalentTo("Stuck");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_ByAssigneeTypeAndComponent_KeepsOnlyMatches()
    {
        var alice = Guid.NewGuid();
        await _repository.AddAsync(new TeamTask("Alice's bug", type: TaskType.Bug, assigneeId: alice, components: "Matching,Rights Manager"));
        await _repository.AddAsync(new TeamTask("Alice's story", type: TaskType.Story, assigneeId: alice, components: "Matching"));
        await _repository.AddAsync(new TeamTask("Bob's bug", type: TaskType.Bug, assigneeId: Guid.NewGuid(), components: "Matching"));

        var (items, _, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter
        {
            AssigneeId = alice,
            Type = TaskType.Bug,
            Component = "rights manager"
        });

        items.Select(t => t.Title).Should().BeEquivalentTo("Alice's bug");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Unassigned_KeepsTasksWithNobody()
    {
        await _repository.AddAsync(new TeamTask("Nobody's"));
        await _repository.AddAsync(new TeamTask("Someone's", assigneeId: Guid.NewGuid()));

        var (items, _, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter { Unassigned = true });

        items.Select(t => t.Title).Should().BeEquivalentTo("Nobody's");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_UnattributedAmong_KeepsTasksNamingNoProduct()
    {
        await _repository.AddAsync(new TeamTask("Attributed", components: " rights manager , Other"));
        await _repository.AddAsync(new TeamTask("Unknown component", components: "Other"));
        await _repository.AddAsync(new TeamTask("No components"));

        var (items, total, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter
        {
            UnattributedAmong = [new Product("Rights Manager"), new Product("Matching")]
        });

        total.Should().Be(2);
        items.Select(t => t.Title).Should().BeEquivalentTo("Unknown component", "No components");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_Ids_KeepsOnlyThoseTasks()
    {
        var wanted = new TeamTask("Wanted");
        await _repository.AddAsync(wanted);
        await _repository.AddAsync(new TeamTask("Other"));

        var (items, total, _) = await _repository.GetFilteredPagedAsync(0, 20, new TeamTaskFilter { Ids = [wanted.Id] });

        total.Should().Be(1);
        items.Select(t => t.Title).Should().Equal("Wanted");
    }

    private static TeamTask InStatus(string title, Action<TeamTask> move, DateTime statusChangedAt)
    {
        var task = new TeamTask(title);
        move(task);
        typeof(TeamTask).GetProperty(nameof(TeamTask.StatusChangedAt))!.SetValue(task, statusChangedAt);
        return task;
    }
}

