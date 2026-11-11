using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class AllocationRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly AllocationRepository _repository;

    public AllocationRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new AllocationRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AllocationRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var allocation = CreateAndAddAllocation();

        // Act
        var result = await _repository.GetByIdAsync(allocation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(allocation.Id);
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
    public async Task GetAllAsync_ReturnsAllAllocations()
    {
        // Arrange
        CreateAndAddAllocation();
        CreateAndAddAllocation();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInitiativeIdAsync_ReturnsMatchingAllocations()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        CreateAndAddAllocation(initiativeId: initiativeId);
        CreateAndAddAllocation(initiativeId: initiativeId);
        CreateAndAddAllocation(); // different initiative

        // Act
        var result = await _repository.GetByInitiativeAsync(initiativeId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.InitiativeId.Should().Be(initiativeId));
    }

    [Fact]
    public async Task GetByInitiativeIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddAllocation();

        // Act
        var result = await _repository.GetByInitiativeAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingAllocations()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        CreateAndAddAllocation(directReportId: directReportId);
        CreateAndAddAllocation(directReportId: directReportId);
        CreateAndAddAllocation(); // different direct report

        // Act
        var result = await _repository.GetByDirectReportAsync(directReportId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.DirectReportId.Should().Be(directReportId));
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddAllocation();

        // Act
        var result = await _repository.GetByDirectReportAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySprintIdAsync_ReturnsMatchingAllocations()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        CreateAndAddAllocation(sprintId: sprintId);
        CreateAndAddAllocation(sprintId: sprintId);
        CreateAndAddAllocation(); // different sprint

        // Act
        var result = await _repository.GetBySprintAsync(sprintId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.SprintId.Should().Be(sprintId));
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddAllocation();

        // Act
        var result = await _repository.GetBySprintAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_AddsAllocationToContext()
    {
        // Arrange
        var allocation = new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _repository.AddAsync(allocation);

        // Assert
        result.Should().Be(allocation);
        _context.Allocations.Should().ContainKey(allocation.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var allocation = CreateAndAddAllocation();

        // Act
        var act = () => _repository.AddAsync(allocation);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAllocationFromContext()
    {
        // Arrange
        var allocation = CreateAndAddAllocation();

        // Act
        await _repository.DeleteAsync(allocation.Id);

        // Assert
        _context.Allocations.Should().NotContainKey(allocation.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_OnlyRemovesMatchingAllocation()
    {
        // Arrange
        var allocation1 = CreateAndAddAllocation();
        var allocation2 = CreateAndAddAllocation();

        // Act
        await _repository.DeleteAsync(allocation1.Id);

        // Assert
        _context.Allocations.Should().NotContainKey(allocation1.Id);
        _context.Allocations.Should().ContainKey(allocation2.Id);
    }

    private Allocation CreateAndAddAllocation(
        Guid? initiativeId = null,
        Guid? directReportId = null,
        Guid? sprintId = null)
    {
        var allocation = new Allocation(
            initiativeId ?? Guid.NewGuid(),
            directReportId ?? Guid.NewGuid(),
            sprintId ?? Guid.NewGuid());
        _context.Allocations.TryAdd(allocation.Id, allocation);
        return allocation;
    }
}
