using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class InitiativeRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly InitiativeRepository _repository;

    public InitiativeRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new InitiativeRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new InitiativeRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var initiative = CreateAndAddInitiative("Alpha Initiative");

        // Act
        var result = await _repository.GetByIdAsync(initiative.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(initiative.Id);
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
    public async Task GetAllAsync_ReturnsAllInitiatives()
    {
        // Arrange
        CreateAndAddInitiative("Alpha Initiative");
        CreateAndAddInitiative("Beta Initiative");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        // Arrange
        var initiative2 = CreateAndAddInitiative("Beta Initiative");
        var initiative1 = CreateAndAddInitiative("Alpha Initiative");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(initiative1.Id);
        result[1].Id.Should().Be(initiative2.Id);
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
    public async Task GetByQuarterIdAsync_ReturnsMatchingInitiatives()
    {
        // Arrange
        var quarterId = Guid.NewGuid();
        CreateAndAddInitiative("Alpha Initiative", quarterId: quarterId);
        CreateAndAddInitiative("Beta Initiative", quarterId: quarterId);
        CreateAndAddInitiative("Gamma Initiative"); // different quarter

        // Act
        var result = await _repository.GetByQuarterAsync(quarterId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.QuarterId.Should().Be(quarterId));
    }

    [Fact]
    public async Task GetByQuarterIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddInitiative("Alpha Initiative");

        // Act
        var result = await _repository.GetByQuarterAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByQuarterIdAsync_OrdersByName()
    {
        // Arrange
        var quarterId = Guid.NewGuid();
        var initiative2 = CreateAndAddInitiative("Beta Initiative", quarterId: quarterId);
        var initiative1 = CreateAndAddInitiative("Alpha Initiative", quarterId: quarterId);

        // Act
        var result = await _repository.GetByQuarterAsync(quarterId);

        // Assert
        result[0].Id.Should().Be(initiative1.Id);
        result[1].Id.Should().Be(initiative2.Id);
    }

    [Fact]
    public async Task AddAsync_AddsInitiativeToContext()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Initiative Name", "#FF0000");

        // Act
        var result = await _repository.AddAsync(initiative);

        // Assert
        result.Should().Be(initiative);
        _context.Initiatives.Should().ContainKey(initiative.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var initiative = CreateAndAddInitiative("Alpha Initiative");

        // Act
        var act = () => _repository.AddAsync(initiative);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesInitiativeInContext()
    {
        // Arrange
        var initiative = CreateAndAddInitiative("Alpha Initiative");
        initiative.Update("Updated Initiative", "New description", "#00FF00", null);

        // Act
        await _repository.UpdateAsync(initiative);

        // Assert
        var stored = _context.Initiatives[initiative.Id];
        stored.Name.Should().Be("Updated Initiative");
        stored.Description.Should().Be("New description");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Initiative Name", "#FF0000");

        // Act
        var act = () => _repository.UpdateAsync(initiative);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesInitiativeFromContext()
    {
        // Arrange
        var initiative = CreateAndAddInitiative("Alpha Initiative");

        // Act
        await _repository.DeleteAsync(initiative.Id);

        // Assert
        _context.Initiatives.Should().NotContainKey(initiative.Id);
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
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var initiative = CreateAndAddInitiative("Alpha Initiative");

        // Act
        var result = await _repository.ExistsAsync(initiative.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    private Initiative CreateAndAddInitiative(string name, Guid? quarterId = null)
    {
        var initiative = new Initiative(quarterId ?? Guid.NewGuid(), name, "#FF0000");
        _context.Initiatives.TryAdd(initiative.Id, initiative);
        return initiative;
    }
}
