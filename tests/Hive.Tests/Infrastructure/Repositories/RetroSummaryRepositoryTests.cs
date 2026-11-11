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
public abstract class RetroSummaryRepositoryTestsBase
{
    protected abstract IRetroSummaryRepository Repository { get; }

    protected static RetroSummary Create(string summary = "The team shipped.") =>
        new(summary, "[]", "[]", "[]", "llama3.1:8b", 3, new DateOnly(2026, 9, 28), "ABC");

    [Fact]
    public async Task GetAsync_NothingCached_ReturnsNull()
    {
        (await Repository.GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsIt()
    {
        var summary = Create();

        await Repository.AddAsync(summary);

        var result = await Repository.GetAsync();
        result.Should().NotBeNull();
        result!.Id.Should().Be(summary.Id);
        result.Summary.Should().Be("The team shipped.");
        result.LatestRetroDate.Should().Be(new DateOnly(2026, 9, 28));
    }

    [Fact]
    public async Task UpdateAsync_SavesTheNewAnalysis()
    {
        var summary = Create();
        await Repository.AddAsync(summary);

        var copy = await Repository.GetAsync();
        copy!.Update("New reading.", "[\"w\"]", "[]", "[]", "Claude", 4, new DateOnly(2026, 10, 12), "DEF");
        await Repository.UpdateAsync(copy);

        var result = await Repository.GetAsync();
        result!.Summary.Should().Be("New reading.");
        result.SourceFingerprint.Should().Be("DEF");
        (await Repository.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEverySummary()
    {
        await Repository.AddAsync(Create("one"));
        await Repository.AddAsync(Create("two"));

        (await Repository.GetAllAsync()).Should().HaveCount(2);
    }
}

public class RetroSummaryRepositoryTests : RetroSummaryRepositoryTestsBase
{
    private readonly RetroSummaryRepository _repository = new(new InMemoryDbContext());

    protected override IRetroSummaryRepository Repository => _repository;

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new RetroSummaryRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }
}

public class SqliteRetroSummaryRepositoryTests : RetroSummaryRepositoryTestsBase, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HiveDbContext _context;
    private readonly SqliteRetroSummaryRepository _repository;

    public SqliteRetroSummaryRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<HiveDbContext>().UseSqlite(_connection).Options;
        _context = new HiveDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new SqliteRetroSummaryRepository(_context);
    }

    protected override IRetroSummaryRepository Repository => _repository;

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
