using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class QuarterRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly QuarterRepository _repository;

    public QuarterRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new QuarterRepository(_context);
    }

    private Quarter AddQuarter(int year = 2024, int quarter = 1, QuarterStatus? status = null)
    {
        var q = new Quarter(year, quarter);
        if (status == QuarterStatus.Active) q.Activate();
        else if (status == QuarterStatus.Completed) { q.Activate(); q.Complete(); }
        _context.Quarters.TryAdd(q.Id, q);
        return q;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new QuarterRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsQuarter()
    {
        var quarter = AddQuarter(2024, 1);
        var result = await _repository.GetByIdAsync(quarter.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(quarter.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByYearQuarterAsync_WhenExists_ReturnsQuarter()
    {
        AddQuarter(2024, 2);
        var result = await _repository.GetByYearQuarterAsync(2024, 2);
        result.Should().NotBeNull();
        result!.Year.Should().Be(2024);
        result.QuarterNumber.Should().Be(2);
    }

    [Fact]
    public async Task GetByYearQuarterAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByYearQuarterAsync(2099, 1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByYearAndQuarterDescending()
    {
        AddQuarter(2024, 1);
        AddQuarter(2024, 3);
        AddQuarter(2023, 4);

        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(3);
        result[0].Year.Should().Be(2024);
        result[0].QuarterNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetByYearAsync_FiltersByYear()
    {
        AddQuarter(2024, 1);
        AddQuarter(2024, 2);
        AddQuarter(2023, 4);

        var result = await _repository.GetByYearAsync(2024);
        result.Should().HaveCount(2);
        result.All(q => q.Year == 2024).Should().BeTrue();
    }

    [Fact]
    public async Task GetByYearAsync_OrdersByQuarterNumberAscending()
    {
        AddQuarter(2024, 3);
        AddQuarter(2024, 1);
        AddQuarter(2024, 2);

        var result = await _repository.GetByYearAsync(2024);
        result[0].QuarterNumber.Should().Be(1);
        result[1].QuarterNumber.Should().Be(2);
        result[2].QuarterNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsActiveQuarter()
    {
        AddQuarter(2024, 1, QuarterStatus.Active);
        AddQuarter(2024, 2, QuarterStatus.Planning);

        var result = await _repository.GetActiveAsync();
        result.Should().NotBeNull();
        result!.Status.Should().Be(QuarterStatus.Active);
    }

    [Fact]
    public async Task GetActiveAsync_WhenNoActive_ReturnsNull()
    {
        AddQuarter(2024, 1);

        var result = await _repository.GetActiveAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AddsQuarter()
    {
        var quarter = new Quarter(2025, 2);
        await _repository.AddAsync(quarter);

        var result = await _repository.GetByIdAsync(quarter.Id);
        result.Should().NotBeNull();
        result!.Year.Should().Be(2025);
        result.QuarterNumber.Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_WhenDuplicate_ThrowsInvalidOperationException()
    {
        var quarter = AddQuarter();
        var act = async () => await _repository.AddAsync(quarter);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesQuarter()
    {
        var quarter = AddQuarter(2024, 1);
        quarter.Activate();

        await _repository.UpdateAsync(quarter);

        var result = await _repository.GetByIdAsync(quarter.Id);
        result!.Status.Should().Be(QuarterStatus.Active);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        var quarter = new Quarter(2025, 3);
        var act = async () => await _repository.UpdateAsync(quarter);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesQuarter()
    {
        var quarter = AddQuarter();
        await _repository.DeleteAsync(quarter.Id);
        _context.Quarters.Should().NotContainKey(quarter.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        AddQuarter(2024, 1);
        var result = await _repository.ExistsAsync(2024, 1);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync(2099, 1);
        result.Should().BeFalse();
    }
}
