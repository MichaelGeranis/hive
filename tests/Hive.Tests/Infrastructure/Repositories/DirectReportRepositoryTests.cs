using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class DirectReportRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly DirectReportRepository _repository;

    public DirectReportRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new DirectReportRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DirectReportRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var entity = CreateAndAddDirectReport();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
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
    public async Task GetByEmailAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var entity = CreateAndAddDirectReport("test@company.com");

        // Act
        var result = await _repository.GetByEmailAsync("TEST@company.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@company.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        CreateAndAddDirectReport();
        CreateAndAddDirectReport("another@test.com", "Jane", "Smith");

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
    public async Task GetAllAsync_ReturnsOrderedByLastNameThenFirstName()
    {
        // Arrange
        CreateAndAddDirectReport("z@test.com", "Zoe", "Adams");
        CreateAndAddDirectReport("a@test.com", "Alice", "Brown");
        CreateAndAddDirectReport("b@test.com", "Bob", "Adams");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].LastName.Should().Be("Adams");
        result[0].FirstName.Should().Be("Bob");
        result[1].LastName.Should().Be("Adams");
        result[1].FirstName.Should().Be("Zoe");
        result[2].LastName.Should().Be("Brown");
    }

    [Fact]
    public async Task AddAsync_AddsEntityToContext()
    {
        // Arrange
        var entity = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.UtcNow);

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.Should().Be(entity);
        _context.DirectReports.Should().ContainKey(entity.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = CreateAndAddDirectReport();
        var duplicate = entity; // Same ID

        // Act
        var act = () => _repository.AddAsync(duplicate);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntityInContext()
    {
        // Arrange
        var entity = CreateAndAddDirectReport();
        entity.Update("Updated", "Name", "updated@test.com", "New Title", "New Dept", DateTime.UtcNow);

        // Act
        await _repository.UpdateAsync(entity);

        // Assert
        var stored = _context.DirectReports[entity.Id];
        stored.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.UtcNow);

        // Act
        var act = () => _repository.UpdateAsync(entity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntityFromContext()
    {
        // Arrange
        var entity = CreateAndAddDirectReport();

        // Act
        await _repository.DeleteAsync(entity.Id);

        // Assert
        _context.DirectReports.Should().NotContainKey(entity.Id);
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
        var entity = CreateAndAddDirectReport();

        // Act
        var result = await _repository.ExistsAsync(entity.Id);

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

    [Fact]
    public async Task EmailExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        CreateAndAddDirectReport("existing@test.com");

        // Act
        var result = await _repository.EmailExistsAsync("EXISTING@TEST.COM");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var entity = CreateAndAddDirectReport("test@test.com");

        // Act
        var result = await _repository.EmailExistsAsync("test@test.com", entity.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_FindsDuplicateWhenExcludingDifferentId()
    {
        // Arrange
        CreateAndAddDirectReport("test@test.com");
        var otherId = Guid.NewGuid();

        // Act
        var result = await _repository.EmailExistsAsync("test@test.com", otherId);

        // Assert
        result.Should().BeTrue();
    }

    private DirectReport CreateAndAddDirectReport(
        string email = "john@test.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        var entity = new DirectReport(firstName, lastName, email, "Engineer", "Engineering", DateTime.UtcNow);
        _context.DirectReports.TryAdd(entity.Id, entity);
        return entity;
    }
}
