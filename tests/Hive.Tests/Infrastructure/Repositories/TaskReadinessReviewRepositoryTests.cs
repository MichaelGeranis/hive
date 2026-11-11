using FluentAssertions;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hive.Tests.Infrastructure.Repositories;

/// <summary>
/// The same behaviour from both persistence stacks.
/// </summary>
public abstract class TaskReadinessReviewRepositoryTestsBase
{
    protected abstract ITaskReadinessReviewRepository Repository { get; }

    protected static TaskReadinessReview Create(Guid taskId) =>
        new(taskId, TaskReadinessVerdict.NeedsWork, "Adds draft saving.", "[\"Acceptance criteria\"]", "llama3.1:8b", "ABC");

    [Fact]
    public async Task GetByTaskIdAsync_NoReview_ReturnsNull()
    {
        (await Repository.GetByTaskIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ThenGetByTaskIdAsync_ReturnsIt()
    {
        var taskId = Guid.NewGuid();
        await Repository.AddAsync(Create(taskId));

        var review = await Repository.GetByTaskIdAsync(taskId);

        review.Should().NotBeNull();
        review!.Verdict.Should().Be(TaskReadinessVerdict.NeedsWork);
        review.MissingJson.Should().Be("[\"Acceptance criteria\"]");
    }

    [Fact]
    public async Task GetByTaskIdsAsync_ReturnsOnlyTheGivenTasks()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await Repository.AddAsync(Create(first));
        await Repository.AddAsync(Create(second));
        await Repository.AddAsync(Create(Guid.NewGuid()));

        var reviews = await Repository.GetByTaskIdsAsync([first, second, Guid.NewGuid()]);

        reviews.Select(r => r.TeamTaskId).Should().BeEquivalentTo([first, second]);
        (await Repository.GetByTaskIdsAsync([])).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_SavesTheNewReview()
    {
        var taskId = Guid.NewGuid();
        await Repository.AddAsync(Create(taskId));

        var copy = await Repository.GetByTaskIdAsync(taskId);
        copy!.Update(TaskReadinessVerdict.Ready, "Clear now.", "[]", "Claude", "DEF");
        await Repository.UpdateAsync(copy);

        var review = await Repository.GetByTaskIdAsync(taskId);
        review!.Verdict.Should().Be(TaskReadinessVerdict.Ready);
        review.SourceFingerprint.Should().Be("DEF");
        (await Repository.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteForMissingTasksAsync_KeepsOnlyReviewsOfExistingTasks()
    {
        var kept = Guid.NewGuid();
        await Repository.AddAsync(Create(kept));
        await Repository.AddAsync(Create(Guid.NewGuid()));

        await Repository.DeleteForMissingTasksAsync([kept]);

        (await Repository.GetAllAsync()).Select(r => r.TeamTaskId).Should().Equal(kept);
    }
}

public class TaskReadinessReviewRepositoryTests : TaskReadinessReviewRepositoryTestsBase
{
    private readonly TaskReadinessReviewRepository _repository = new(new InMemoryDbContext());

    protected override ITaskReadinessReviewRepository Repository => _repository;

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new TaskReadinessReviewRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task AddAsync_SecondReviewOfATask_Throws()
    {
        var taskId = Guid.NewGuid();
        await _repository.AddAsync(Create(taskId));

        var act = () => _repository.AddAsync(Create(taskId));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public class SqliteTaskReadinessReviewRepositoryTests : TaskReadinessReviewRepositoryTestsBase, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HiveDbContext _context;
    private readonly SqliteTaskReadinessReviewRepository _repository;

    public SqliteTaskReadinessReviewRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<HiveDbContext>().UseSqlite(_connection).Options;
        _context = new HiveDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new SqliteTaskReadinessReviewRepository(_context);
    }

    protected override ITaskReadinessReviewRepository Repository => _repository;

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
