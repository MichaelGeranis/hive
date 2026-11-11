using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Infrastructure.Repositories;

/// <summary>
/// SQLite reads are untracked; saving must still work whichever instance of a row is handed back.
/// </summary>
public class SqliteTrackingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HiveDbContext> _options;

    public SqliteTrackingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<HiveDbContext>().UseSqlite(_connection).Options;
        using var context = new HiveDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTasks_WithoutTrackingThem()
    {
        await SeedTaskAsync();
        await using var context = new HiveDbContext(_options);

        var tasks = await new SqliteTeamTaskRepository(context).GetAllAsync();

        tasks.Should().ContainSingle();
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_CopyOfATrackedRow_SavesTheCopy()
    {
        var id = await SeedTaskAsync();
        await using (var context = new HiveDbContext(_options))
        {
            var repository = new SqliteTeamTaskRepository(context);
            _ = await repository.GetByIdAsync(id);
            var copy = (await repository.GetAllAsync()).Single();
            copy.Start();

            await repository.UpdateAsync(copy);
        }

        await using var fresh = new HiveDbContext(_options);
        (await fresh.TeamTasks.SingleAsync()).Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task UpdateAsync_TwoCopiesSavedInTurn_KeepsTheLastSaved()
    {
        await SeedTaskAsync();
        await using (var context = new HiveDbContext(_options))
        {
            var repository = new SqliteTeamTaskRepository(context);
            var first = (await repository.GetAllAsync()).Single();
            first.Start();
            await repository.UpdateAsync(first);

            var second = (await repository.GetAllAsync()).Single();
            second.Complete();
            await repository.UpdateAsync(second);
        }

        await using var fresh = new HiveDbContext(_options);
        (await fresh.TeamTasks.SingleAsync()).Status.Should().Be(TaskStatus.Done);
    }

    private async Task<Guid> SeedTaskAsync()
    {
        await using var context = new HiveDbContext(_options);
        var task = new TeamTask("Write the report");
        context.TeamTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
