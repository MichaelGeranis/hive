using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for ParentService.
/// </summary>
public class ParentServiceTests
{
    private readonly Mock<IParentRepository> _parentRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly ParentService _service;

    public ParentServiceTests()
    {
        _parentRepositoryMock = new Mock<IParentRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new ParentService(_parentRepositoryMock.Object, _taskRepositoryMock.Object, _activityServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullParentRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ParentService(null!, _taskRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("parentRepository");
    }

    [Fact]
    public void Constructor_WithNullTaskRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ParentService(_parentRepositoryMock.Object, null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("taskRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ParentService(_parentRepositoryMock.Object, _taskRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var parent = new Parent("Epic 1", "label1");
        var parentId = GetId(parent);

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetByIdAsync(parentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(parentId);
        result.Name.Should().Be("Epic 1");
        result.Labels.Should().Be("label1");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _parentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);

        _parentRepositoryMock.Setup(r => r.GetByNameAsync("Epic 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetByNameAsync("Epic 1");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Epic 1");
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _parentRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent?)null);

        // Act
        var result = await _service.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllParents()
    {
        // Arrange
        var parent1 = new Parent("Epic 1");
        var parent2 = new Parent("Epic 2");
        var parents = new List<Parent> { parent1, parent2 };

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(parents);
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Name == "Epic 1");
        result.Should().Contain(p => p.Name == "Epic 2");
    }

    [Fact]
    public async Task GetAllAsync_CalculatesTaskMetrics()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);

        var task1 = new TeamTask("Task 1", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(task1, parentId);
        SetTaskStatus(task1, TaskStatus.Done);
        SetTaskStoryPoints(task1, 5);

        var task2 = new TeamTask("Task 2", "Description", TaskType.Task, TaskPriority.Medium);
        SetTaskParent(task2, parentId);
        SetTaskStatus(task2, TaskStatus.InProgress);
        SetTaskStoryPoints(task2, 3);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2 });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.TotalTasks.Should().Be(2);
        dto.CompletedTasks.Should().Be(1);
        dto.OpenTasks.Should().Be(1);
        dto.TotalStoryPoints.Should().Be(8);
        dto.CompletedStoryPoints.Should().Be(5, "only the done task's points are done");
    }

    [Fact]
    public async Task GetAllAsync_CountsWontDoTasksAsProgress()
    {
        // Arrange
        var parent = new Parent("[MAR_1Q26_S1] Service Maintainer");
        var parentId = GetId(parent);

        var done = new TeamTask("DDEX acknowledgement fails", "Description", TaskType.Support, TaskPriority.High);
        SetTaskParent(done, parentId);
        SetTaskStatus(done, TaskStatus.Done);

        var wontDo = new TeamTask("Investigate Audio Album Profile", "Description", TaskType.Support, TaskPriority.Medium);
        SetTaskParent(wontDo, parentId);
        SetTaskStatus(wontDo, TaskStatus.Cancelled);
        SetTaskStoryPoints(wontDo, 3);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { done, wontDo });

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.TotalTasks.Should().Be(2);
        dto.CompletedTasks.Should().Be(2, "a Won't Do task is resolved, so the parent is complete");
        dto.OpenTasks.Should().Be(0);
        dto.CompletedStoryPoints.Should().Be(3, "a Won't Do task's points are resolved too");
    }

    [Fact]
    public async Task GetAllAsync_AllTasksResolved_IsDone()
    {
        // Arrange
        var parent = new Parent("Payments revamp");
        var parentId = GetId(parent);

        var done = new TeamTask("Ship it", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(done, parentId);
        SetTaskStatus(done, TaskStatus.Done);

        var wontDo = new TeamTask("Drop it", "Description", TaskType.Task, TaskPriority.Low);
        SetTaskParent(wontDo, parentId);
        SetTaskStatus(wontDo, TaskStatus.Cancelled);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { done, wontDo });

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_OpenTaskRemaining_IsNotDone()
    {
        // Arrange
        var parent = new Parent("Payments revamp");
        var parentId = GetId(parent);

        var done = new TeamTask("Ship it", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(done, parentId);
        SetTaskStatus(done, TaskStatus.Done);

        var open = new TeamTask("Still going", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(open, parentId);
        SetTaskStatus(open, TaskStatus.InProgress);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { done, open });

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.IsDone.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_NoTasks_IsNotDone()
    {
        // Arrange
        var parent = new Parent("Not started");
        var parentId = GetId(parent);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.IsDone.Should().BeFalse("a parent with nothing under it has not been finished");
    }

    [Fact]
    public async Task GetAllAsync_ExcludesParentTasks()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);

        // This task has the same title as a parent name, should be excluded
        var parentTask = new TeamTask("Epic 1", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(parentTask, parentId);
        SetTaskStatus(parentTask, TaskStatus.Done);

        var normalTask = new TeamTask("Task 1", "Description", TaskType.Task, TaskPriority.High);
        SetTaskParent(normalTask, parentId);
        SetTaskStatus(normalTask, TaskStatus.InProgress);

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { parentTask, normalTask });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.TotalTasks.Should().Be(1); // Only normalTask counts
        dto.CompletedTasks.Should().Be(0);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesParent()
    {
        // Arrange
        var dto = new CreateParentDto { Name = "Epic 1", Labels = "label1" };

        _parentRepositoryMock.Setup(r => r.ExistsAsync("Epic 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _parentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent p, CancellationToken _) => p);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Epic 1");
        result.Labels.Should().Be("label1");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateParentDto { Name = "Epic 1" };

        _parentRepositoryMock.Setup(r => r.ExistsAsync("Epic 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("A parent with name 'Epic 1' already exists.");
    }

    #endregion

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_WhenExists_ReturnsExisting()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);

        _parentRepositoryMock.Setup(r => r.GetByNameAsync("Epic 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent> { parent });
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetOrCreateAsync("Epic 1");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Epic 1");
        _parentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNotExists_CreatesNew()
    {
        // Arrange
        _parentRepositoryMock.Setup(r => r.GetByNameAsync("Epic 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent?)null);
        _parentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent p, CancellationToken _) => p);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetOrCreateAsync("Epic 1");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Epic 1");
        _parentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.GetOrCreateAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Parent name cannot be empty.*");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesParent()
    {
        // Arrange
        var parent = new Parent("Epic 1", "label1");
        var parentId = GetId(parent);
        var dto = new UpdateParentDto { Name = "Updated Epic", Labels = "label2" };

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.ExistsAsync("Updated Epic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _parentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.UpdateAsync(parentId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Epic");
        result.Labels.Should().Be("label2");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new UpdateParentDto { Name = "Updated" };

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateName_ThrowsConflictException()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);
        var dto = new UpdateParentDto { Name = "Epic 2" };

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.ExistsAsync("Epic 2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.UpdateAsync(parentId, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("A parent with name 'Epic 2' already exists.");
    }

    [Fact]
    public async Task UpdateAsync_WithSameName_DoesNotCheckForDuplicate()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);
        var dto = new UpdateParentDto { Name = "Epic 1", Labels = "new labels" };

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Parent>());
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.UpdateAsync(parentId, dto);

        // Assert
        result.Should().NotBeNull();
        _parentRepositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesParent()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var parentId = GetId(parent);

        _parentRepositoryMock.Setup(r => r.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _parentRepositoryMock.Setup(r => r.DeleteAsync(parentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(parentId);

        // Assert
        _parentRepositoryMock.Verify(r => r.DeleteAsync(parentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _parentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Helper Methods

    private static Guid GetId(Parent parent)
    {
        var idProperty = typeof(Parent).GetProperty("Id");
        return (Guid)idProperty!.GetValue(parent)!;
    }

    private static void SetTaskParent(TeamTask task, Guid parentId)
    {
        var parentIdProperty = typeof(TeamTask).GetProperty("ParentId");
        parentIdProperty!.SetValue(task, parentId);
    }

    private static void SetTaskStatus(TeamTask task, TaskStatus status)
    {
        var statusProperty = typeof(TeamTask).GetProperty("Status");
        statusProperty!.SetValue(task, status);
    }

    private static void SetTaskStoryPoints(TeamTask task, int points)
    {
        var pointsProperty = typeof(TeamTask).GetProperty("StoryPoints");
        pointsProperty!.SetValue(task, points);
    }

    #endregion
}
