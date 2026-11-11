using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class NoteFolderRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly NoteFolderRepository _repository;

    public NoteFolderRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new NoteFolderRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new NoteFolderRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_StoresFolder()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        var result = await _repository.AddAsync(folder);

        // Assert
        result.Id.Should().Be(folder.Id);
        _context.NoteFolders.ContainsKey(folder.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsFolder()
    {
        // Arrange
        var folder = await _repository.AddAsync(new NoteFolder("Team"));

        // Act
        var result = await _repository.GetByIdAsync(folder.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Team");
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
    public async Task GetAllAsync_OrdersBySortOrderThenName()
    {
        // Arrange
        await _repository.AddAsync(new NoteFolder("Zebra", null, 0));
        await _repository.AddAsync(new NoteFolder("Alpha", null, 0));
        await _repository.AddAsync(new NoteFolder("Middle", null, 1));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Select(f => f.Name).Should().ContainInOrder("Alpha", "Zebra", "Middle");
    }

    [Fact]
    public async Task GetChildrenAsync_ReturnsOnlyDirectChildren()
    {
        // Arrange
        var parent = await _repository.AddAsync(new NoteFolder("Parent"));
        await _repository.AddAsync(new NoteFolder("Child", parent.Id));
        var grandChildParent = await _repository.AddAsync(new NoteFolder("Other child", parent.Id));
        await _repository.AddAsync(new NoteFolder("Grandchild", grandChildParent.Id));

        // Act
        var result = await _repository.GetChildrenAsync(parent.Id);

        // Assert
        result.Select(f => f.Name).Should().BeEquivalentTo(new[] { "Child", "Other child" });
    }

    [Fact]
    public async Task GetChildrenAsync_WithNullParent_ReturnsRootFolders()
    {
        // Arrange
        var parent = await _repository.AddAsync(new NoteFolder("Root folder"));
        await _repository.AddAsync(new NoteFolder("Nested", parent.Id));

        // Act
        var result = await _repository.GetChildrenAsync(null);

        // Assert
        result.Should().ContainSingle().Which.Name.Should().Be("Root folder");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesStoredFolder()
    {
        // Arrange
        var folder = await _repository.AddAsync(new NoteFolder("Team"));
        folder.Rename("Squad");

        // Act
        await _repository.UpdateAsync(folder);

        // Assert
        var stored = await _repository.GetByIdAsync(folder.Id);
        stored!.Name.Should().Be("Squad");
    }

    [Fact]
    public async Task DeleteAsync_RemovesFolder()
    {
        // Arrange
        var folder = await _repository.AddAsync(new NoteFolder("Team"));

        // Act
        await _repository.DeleteAsync(folder.Id);

        // Assert
        _context.NoteFolders.ContainsKey(folder.Id).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ReflectsWhetherFolderIsStored()
    {
        // Arrange
        var folder = await _repository.AddAsync(new NoteFolder("Team"));

        // Act
        var exists = await _repository.ExistsAsync(folder.Id);
        var missing = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        exists.Should().BeTrue();
        missing.Should().BeFalse();
    }
}
