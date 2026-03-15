using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SprintGoalRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SprintGoalRepository _repository;

    public SprintGoalRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SprintGoalRepository(_context);
    }

    private SprintGoal AddSprintGoal(Guid? quarterId = null, Guid? sprintId = null, string? goal = "Deliver feature X")
    {
        var sg = new SprintGoal(quarterId ?? Guid.NewGuid(), sprintId ?? Guid.NewGuid(), goal);
        _context.SprintGoals.TryAdd(sg.Id, sg);
        return sg;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new SprintGoalRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsSprintGoal()
    {
        var sg = AddSprintGoal();
        var result = await _repository.GetByIdAsync(sg.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(sg.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByQuarterSprintAsync_WhenExists_ReturnsSprintGoal()
    {
        var quarterId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        AddSprintGoal(quarterId, sprintId, "Goal A");

        var result = await _repository.GetByQuarterSprintAsync(quarterId, sprintId);
        result.Should().NotBeNull();
        result!.QuarterId.Should().Be(quarterId);
        result.SprintId.Should().Be(sprintId);
    }

    [Fact]
    public async Task GetByQuarterSprintAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByQuarterSprintAsync(Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByQuarterAsync_ReturnsSprintGoalsForQuarter()
    {
        var quarterId = Guid.NewGuid();
        AddSprintGoal(quarterId, Guid.NewGuid(), "Goal 1");
        AddSprintGoal(quarterId, Guid.NewGuid(), "Goal 2");
        AddSprintGoal(Guid.NewGuid(), Guid.NewGuid(), "Other Quarter Goal");

        var result = await _repository.GetByQuarterAsync(quarterId);
        result.Should().HaveCount(2);
        result.All(sg => sg.QuarterId == quarterId).Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_AddsSprintGoal()
    {
        var quarterId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        var sg = new SprintGoal(quarterId, sprintId, "New Goal");

        await _repository.AddAsync(sg);

        var result = await _repository.GetByIdAsync(sg.Id);
        result.Should().NotBeNull();
        result!.Goal.Should().Be("New Goal");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicate_ThrowsInvalidOperationException()
    {
        var sg = AddSprintGoal();
        var act = async () => await _repository.AddAsync(sg);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSprintGoal()
    {
        var sg = AddSprintGoal(goal: "Original Goal");
        sg.Update("Updated Goal", null);

        await _repository.UpdateAsync(sg);

        var result = await _repository.GetByIdAsync(sg.Id);
        result!.Goal.Should().Be("Updated Goal");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        var sg = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), "Goal");
        var act = async () => await _repository.UpdateAsync(sg);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesSprintGoal()
    {
        var sg = AddSprintGoal();
        await _repository.DeleteAsync(sg.Id);
        _context.SprintGoals.Should().NotContainKey(sg.Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_DoesNotThrow()
    {
        var act = async () => await _repository.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }
}
