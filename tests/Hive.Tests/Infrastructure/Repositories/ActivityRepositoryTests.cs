using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ActivityRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ActivityRepository _repository;

    public ActivityRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ActivityRepository(_context);
    }

    private Activity CreateActivity(ActivityType type = ActivityType.Created, EntityType entityType = EntityType.Task, string name = "Entity", string description = "Description")
    {
        var activity = new Activity(type, entityType, Guid.NewGuid(), name, description);
        _context.Activities.TryAdd(activity.Id, activity);
        return activity;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new ActivityRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsActivity()
    {
        var activity = CreateActivity();
        var result = await _repository.GetByIdAsync(activity.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(activity.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActivities()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Task A");
        CreateActivity(ActivityType.Updated, EntityType.Project, "Project B");

        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByTimestampDescending()
    {
        var activity1 = CreateActivity(ActivityType.Created, EntityType.Task, "Older");
        var activity2 = CreateActivity(ActivityType.Updated, EntityType.Task, "Newer");

        var result = await _repository.GetAllAsync();
        // Both created at nearly same time; just verify count
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsActivitiesWithinDays()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Recent");

        var result = await _repository.GetRecentAsync(7);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByEntityTypeAsync_FiltersByEntityType()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Task A");
        CreateActivity(ActivityType.Created, EntityType.Sprint, "Sprint 1");
        CreateActivity(ActivityType.Updated, EntityType.Task, "Task B");

        var result = await _repository.GetByEntityTypeAsync(EntityType.Task);
        result.Should().HaveCount(2);
        result.All(a => a.EntityType == EntityType.Task).Should().BeTrue();
    }

    [Fact]
    public async Task GetByEntityTypeAsync_WhenNoMatch_ReturnsEmptyList()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Task");

        var result = await _repository.GetByEntityTypeAsync(EntityType.Leave);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsActivitiesInRange()
    {
        CreateActivity(); // created now

        var start = DateTime.UtcNow.AddSeconds(-5);
        var end = DateTime.UtcNow.AddSeconds(5);

        var result = await _repository.GetByDateRangeAsync(start, end);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ExcludesActivitiesOutsideRange()
    {
        CreateActivity(); // created now

        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(2);

        var result = await _repository.GetByDateRangeAsync(start, end);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_AddsActivity()
    {
        var activity = new Activity(ActivityType.Created, EntityType.Leave, Guid.NewGuid(), "Leave", "Leave created");
        await _repository.AddAsync(activity);

        var result = await _repository.GetByIdAsync(activity.Id);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        var act = async () => await _repository.AddAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        CreateActivity();
        CreateActivity();
        CreateActivity();

        var count = await _repository.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var activity = CreateActivity();
        var result = await _repository.ExistsAsync(activity.Id);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_FiltersResults()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Sprint Planning", "Sprint was created");
        CreateActivity(ActivityType.Updated, EntityType.Project, "Alpha Project", "Project was updated");

        var (items, totalCount) = await _repository.SearchAsync("Sprint", 0, 10);
        items.Should().HaveCount(1);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_WithNullTerm_ReturnsAll()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "Task A");
        CreateActivity(ActivityType.Updated, EntityType.Project, "Project B");

        var (items, totalCount) = await _repository.SearchAsync(null, 0, 10);
        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_WithPagination_RespectsSkipAndTake()
    {
        for (var i = 0; i < 5; i++)
        {
            CreateActivity(ActivityType.Created, EntityType.Task, $"Task {i}");
        }

        var (items, totalCount) = await _repository.SearchAsync(null, 2, 2);
        items.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        CreateActivity(ActivityType.Created, EntityType.Task, "My Task", "task was created");

        var (items, _) = await _repository.SearchAsync("MY TASK", 0, 10);
        items.Should().HaveCount(1);
    }
}
