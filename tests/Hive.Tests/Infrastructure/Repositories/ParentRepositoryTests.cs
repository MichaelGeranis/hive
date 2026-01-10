using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ParentRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ParentRepository _repository;

    public ParentRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ParentRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ParentRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var parent = CreateAndAddParent("Epic 1");

        // Act
        var result = await _repository.GetByIdAsync(parent.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(parent.Id);
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
    public async Task GetByNameAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        CreateAndAddParent("Epic 1");

        // Act
        var result = await _repository.GetByNameAsync("Epic 1");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Epic 1");
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddParent("Epic 1");

        // Act
        var result = await _repository.GetByNameAsync("epic 1");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllParents()
    {
        // Arrange
        CreateAndAddParent("Epic 1");
        CreateAndAddParent("Epic 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        // Arrange
        CreateAndAddParent("Zebra");
        CreateAndAddParent("Alpha");
        CreateAndAddParent("Beta");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Beta");
        result[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetByMatchingLabelsAsync_ReturnsParentsWithMatchingLabels()
    {
        // Arrange
        CreateAndAddParent("Epic 1", labels: "project-a,frontend");
        CreateAndAddParent("Epic 2", labels: "project-a,backend");
        CreateAndAddParent("Epic 3", labels: "project-b");

        // Act
        var result = await _repository.GetByMatchingLabelsAsync(new[] { "project-a" });

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMatchingLabelsAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddParent("Epic 1", labels: "PROJECT-A");

        // Act
        var result = await _repository.GetByMatchingLabelsAsync(new[] { "project-a" });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByMatchingLabelsAsync_WithMultipleLabels_ReturnsAnyMatch()
    {
        // Arrange
        CreateAndAddParent("Epic 1", labels: "frontend");
        CreateAndAddParent("Epic 2", labels: "backend");
        CreateAndAddParent("Epic 3", labels: "mobile");

        // Act
        var result = await _repository.GetByMatchingLabelsAsync(new[] { "frontend", "backend" });

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMatchingLabelsAsync_ExcludesParentsWithoutLabels()
    {
        // Arrange
        CreateAndAddParent("Epic 1", labels: "project-a");
        CreateAndAddParent("Epic 2"); // No labels

        // Act
        var result = await _repository.GetByMatchingLabelsAsync(new[] { "project-a" });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_AddsParentToContext()
    {
        // Arrange
        var parent = new Parent("Epic 1", "project-a");

        // Act
        var result = await _repository.AddAsync(parent);

        // Assert
        result.Should().Be(parent);
        _context.Parents.Should().ContainKey(parent.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var parent = CreateAndAddParent("Epic 1");

        // Act
        var act = () => _repository.AddAsync(parent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesParentInContext()
    {
        // Arrange
        var parent = CreateAndAddParent("Original");
        parent.Update("Updated", "new-label");

        // Act
        await _repository.UpdateAsync(parent);

        // Assert
        var stored = _context.Parents[parent.Id];
        stored.Name.Should().Be("Updated");
        stored.Labels.Should().Be("new-label");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentParent_ThrowsInvalidOperationException()
    {
        // Arrange
        var parent = new Parent("Test");

        // Act
        var act = () => _repository.UpdateAsync(parent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesParentFromContext()
    {
        // Arrange
        var parent = CreateAndAddParent("Test");

        // Act
        await _repository.DeleteAsync(parent.Id);

        // Assert
        _context.Parents.Should().NotContainKey(parent.Id);
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
        CreateAndAddParent("Epic 1");

        // Act
        var result = await _repository.ExistsAsync("Epic 1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddParent("Epic 1");

        // Act
        var result = await _repository.ExistsAsync("epic 1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    private Parent CreateAndAddParent(string name = "Test Parent", string? labels = null)
    {
        var parent = new Parent(name, labels);
        _context.Parents.TryAdd(parent.Id, parent);
        return parent;
    }
}
