using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ProductRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ProductRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProductRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheProduct()
    {
        // Arrange
        var product = new Product("Matching");

        // Act
        await _repository.AddAsync(product);
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        result.Should().BeSameAs(product);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        // Arrange
        await _repository.AddAsync(new Product("Rights Manager"));
        await _repository.AddAsync(new Product("Enrichment"));
        await _repository.AddAsync(new Product("Matching"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Select(p => p.Name).Should().Equal("Enrichment", "Matching", "Rights Manager");
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_Throws()
    {
        // Act
        var act = () => _repository.UpdateAsync(new Product("Matching"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheProduct()
    {
        // Arrange
        var product = new Product("Matching");
        await _repository.AddAsync(product);

        // Act
        await _repository.DeleteAsync(product.Id);

        // Assert
        (await _repository.GetByIdAsync(product.Id)).Should().BeNull();
    }

    [Fact]
    public async Task NameExistsAsync_IgnoresCaseAndTheExcludedProduct()
    {
        // Arrange
        var product = new Product("Matching");
        await _repository.AddAsync(product);

        // Assert
        (await _repository.NameExistsAsync(" matching ")).Should().BeTrue();
        (await _repository.NameExistsAsync("Matching", product.Id)).Should().BeFalse();
        (await _repository.NameExistsAsync("IPI")).Should().BeFalse();
    }
}
