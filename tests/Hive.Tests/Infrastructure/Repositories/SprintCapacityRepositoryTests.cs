using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SprintCapacityRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SprintCapacityRepository _repository;
    private readonly Guid _sprintId;

    public SprintCapacityRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SprintCapacityRepository(_context);
        _sprintId = Guid.NewGuid();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var capacity = CreateAndAddCapacity();

        // Act
        var result = await _repository.GetByIdAsync(capacity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(capacity.Id);
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
    public async Task GetBySprintIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        CreateAndAddCapacity();

        // Act
        var result = await _repository.GetBySprintIdAsync(_sprintId);

        // Assert
        result.Should().NotBeNull();
        result!.SprintId.Should().Be(_sprintId);
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySprintIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCapacities()
    {
        // Arrange
        CreateAndAddCapacity();
        CreateAndAddCapacity(sprintId: Guid.NewGuid());

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBySprintIdsAsync_ReturnsMatchingCapacities()
    {
        // Arrange
        var sprint1 = Guid.NewGuid();
        var sprint2 = Guid.NewGuid();
        var sprint3 = Guid.NewGuid();

        CreateAndAddCapacity(sprintId: sprint1);
        CreateAndAddCapacity(sprintId: sprint2);
        CreateAndAddCapacity(sprintId: sprint3);

        // Act
        var result = await _repository.GetBySprintIdsAsync(new[] { sprint1, sprint3 });

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.SprintId == sprint1);
        result.Should().Contain(c => c.SprintId == sprint3);
    }

    [Fact]
    public async Task AddAsync_AddsCapacityToContext()
    {
        // Arrange
        var capacity = new SprintCapacity(_sprintId, 100, 5);

        // Act
        var result = await _repository.AddAsync(capacity);

        // Assert
        result.Should().Be(capacity);
        _context.SprintCapacities.Should().ContainKey(capacity.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var capacity = CreateAndAddCapacity();

        // Act
        var act = () => _repository.AddAsync(capacity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCapacityInContext()
    {
        // Arrange
        var capacity = CreateAndAddCapacity();
        capacity.Update(150, 7);

        // Act
        await _repository.UpdateAsync(capacity);

        // Assert
        var stored = _context.SprintCapacities[capacity.Id];
        stored.TotalCapacityPoints.Should().Be(150);
        stored.AvailableMembers.Should().Be(7);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentCapacity_ThrowsInvalidOperationException()
    {
        // Arrange
        var capacity = new SprintCapacity(_sprintId, 100, 5);

        // Act
        var act = () => _repository.UpdateAsync(capacity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesCapacityFromContext()
    {
        // Arrange
        var capacity = CreateAndAddCapacity();

        // Act
        await _repository.DeleteAsync(capacity.Id);

        // Assert
        _context.SprintCapacities.Should().NotContainKey(capacity.Id);
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
    public async Task ExistsBySprintIdAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        CreateAndAddCapacity();

        // Act
        var result = await _repository.ExistsBySprintIdAsync(_sprintId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySprintIdAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsBySprintIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    private SprintCapacity CreateAndAddCapacity(
        Guid? sprintId = null,
        int capacityPoints = 100,
        int availableMembers = 5)
    {
        var capacity = new SprintCapacity(sprintId ?? _sprintId, capacityPoints, availableMembers);
        _context.SprintCapacities.TryAdd(capacity.Id, capacity);
        return capacity;
    }
}
