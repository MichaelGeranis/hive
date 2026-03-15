using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class ActivityServiceTests
{
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly ActivityService _service;

    public ActivityServiceTests()
    {
        _activityRepositoryMock = new Mock<IActivityRepository>();
        _service = new ActivityService(_activityRepositoryMock.Object);
    }

    // ============== Constructor Tests ==============

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var act = () => new ActivityService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activityRepository");
    }

    // ============== GetByIdAsync Tests ==============

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activity = new Activity(ActivityType.Created, EntityType.Task, entityId, "Task #1", "Task was created");
        _activityRepositoryMock.Setup(r => r.GetByIdAsync(activity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await _service.GetByIdAsync(activity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(activity.Id);
        result.EntityId.Should().Be(entityId);
        result.EntityName.Should().Be("Task #1");
        result.Description.Should().Be("Task was created");
        result.ActivityType.Should().Be("Created");
        result.ActivityTypeName.Should().Be("Created");
        result.EntityType.Should().Be("Task");
        result.EntityTypeName.Should().Be("Task");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _activityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ============== GetAllAsync Tests ==============

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activities = new List<Activity>
        {
            new Activity(ActivityType.Created, EntityType.Task, entityId, "Task A", "Task A was created"),
            new Activity(ActivityType.Updated, EntityType.Project, entityId, "Project X", "Project X was updated"),
        };
        _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(a => a.EntityName).Should().Contain(["Task A", "Project X"]);
    }

    [Fact]
    public async Task GetAllAsync_WithNoActivities_ReturnsEmptyList()
    {
        // Arrange
        _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ============== GetRecentAsync Tests ==============

    [Fact]
    public async Task GetRecentAsync_WithValidDays_ReturnsRecentActivities()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activities = new List<Activity>
        {
            new Activity(ActivityType.Created, EntityType.Leave, entityId, "Leave Request", "Leave was created")
        };
        _activityRepositoryMock.Setup(r => r.GetRecentAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await _service.GetRecentAsync(7);

        // Assert
        result.Should().HaveCount(1);
        _activityRepositoryMock.Verify(r => r.GetRecentAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentAsync_WithDefaultDays_Uses7DaysDefault()
    {
        // Arrange
        _activityRepositoryMock.Setup(r => r.GetRecentAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());

        // Act
        await _service.GetRecentAsync();

        // Assert
        _activityRepositoryMock.Verify(r => r.GetRecentAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetRecentAsync_WithNonPositiveDays_ThrowsArgumentException(int invalidDays)
    {
        // Act
        var act = async () => await _service.GetRecentAsync(invalidDays);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("days")
            .WithMessage("*greater than zero*");
    }

    // ============== GetByEntityTypeAsync Tests ==============

    [Fact]
    public async Task GetByEntityTypeAsync_ReturnsActivitiesForEntityType()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activities = new List<Activity>
        {
            new Activity(ActivityType.Created, EntityType.Sprint, entityId, "Sprint 1", "Sprint created"),
            new Activity(ActivityType.Updated, EntityType.Sprint, entityId, "Sprint 1", "Sprint updated")
        };
        _activityRepositoryMock.Setup(r => r.GetByEntityTypeAsync(EntityType.Sprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);

        // Act
        var result = await _service.GetByEntityTypeAsync(EntityType.Sprint);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.EntityType == "Sprint").Should().BeTrue();
    }

    // ============== LogActivityAsync Tests ==============

    [Fact]
    public async Task LogActivityAsync_CreatesAndReturnsActivity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activity = new Activity(ActivityType.Created, EntityType.Task, entityId, "Task #1", "Task was created");
        _activityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await _service.LogActivityAsync(
            ActivityType.Created,
            EntityType.Task,
            entityId,
            "Task #1",
            "Task was created");

        // Assert
        result.Should().NotBeNull();
        result.ActivityType.Should().Be("Created");
        result.EntityType.Should().Be("Task");
        result.EntityId.Should().Be(entityId);
        result.EntityName.Should().Be("Task #1");
        _activityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== SearchAsync Tests ==============

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ReturnsPagedResult()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activities = new List<Activity>
        {
            new Activity(ActivityType.Created, EntityType.Task, entityId, "Sprint 10", "Sprint 10 was created")
        };
        var pagination = new ActivityPaginationParams { SearchTerm = "Sprint 10" };

        _activityRepositoryMock.Setup(r => r.SearchAsync("Sprint 10", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((activities, 1));

        // Act
        var result = await _service.SearchAsync(pagination);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_WithNoResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var pagination = new ActivityPaginationParams { SearchTerm = "NonExistent" };
        _activityRepositoryMock.Setup(r => r.SearchAsync("NonExistent", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Activity>(), 0));

        // Act
        var result = await _service.SearchAsync(pagination);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithNullSearchTerm_PassesNullToRepository()
    {
        // Arrange
        var pagination = new ActivityPaginationParams();
        _activityRepositoryMock.Setup(r => r.SearchAsync(null, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Activity>(), 0));

        // Act
        await _service.SearchAsync(pagination);

        // Assert
        _activityRepositoryMock.Verify(r => r.SearchAsync(null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== Activity Type Name Mapping Tests ==============

    [Theory]
    [InlineData(ActivityType.Created, "Created")]
    [InlineData(ActivityType.Updated, "Updated")]
    [InlineData(ActivityType.StatusChanged, "Status Changed")]
    [InlineData(ActivityType.Approved, "Approved")]
    [InlineData(ActivityType.Rejected, "Rejected")]
    [InlineData(ActivityType.Completed, "Completed")]
    [InlineData(ActivityType.Deleted, "Deleted")]
    [InlineData(ActivityType.Cancelled, "Cancelled")]
    public async Task LogActivityAsync_MapsActivityTypeNameCorrectly(ActivityType activityType, string expectedName)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activity = new Activity(activityType, EntityType.Task, entityId, "Entity", "Description");
        _activityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await _service.LogActivityAsync(activityType, EntityType.Task, entityId, "Entity", "Description");

        // Assert
        result.ActivityTypeName.Should().Be(expectedName);
    }

    // ============== Entity Type Name Mapping Tests ==============

    [Theory]
    [InlineData(EntityType.Review, "Performance Review")]
    [InlineData(EntityType.Task, "Task")]
    [InlineData(EntityType.Leave, "Leave")]
    [InlineData(EntityType.DirectReport, "Team Member")]
    [InlineData(EntityType.Meeting, "1:1 Meeting")]
    [InlineData(EntityType.MeetingNote, "Meeting Note")]
    [InlineData(EntityType.ManagerNote, "Note/TODO")]
    [InlineData(EntityType.Project, "Project")]
    [InlineData(EntityType.Sprint, "Sprint")]
    [InlineData(EntityType.Document, "Document")]
    [InlineData(EntityType.Skill, "Skill")]
    [InlineData(EntityType.SkillAssessment, "Skill Assessment")]
    [InlineData(EntityType.ChecklistTemplate, "Checklist Template")]
    [InlineData(EntityType.ChecklistInstance, "Checklist")]
    [InlineData(EntityType.Parent, "Reporting Structure")]
    [InlineData(EntityType.ProjectKnowledge, "Knowledge Assessment")]
    public async Task LogActivityAsync_MapsEntityTypeNameCorrectly(EntityType entityType, string expectedName)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var activity = new Activity(ActivityType.Created, entityType, entityId, "Entity", "Description");
        _activityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await _service.LogActivityAsync(ActivityType.Created, entityType, entityId, "Entity", "Description");

        // Assert
        result.EntityTypeName.Should().Be(expectedName);
    }
}
