using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ProjectProductRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ProjectProductRepository _repository;
    private readonly Guid _projectA = Guid.NewGuid();
    private readonly Guid _projectB = Guid.NewGuid();
    private readonly Guid _matching = Guid.NewGuid();
    private readonly Guid _ipi = Guid.NewGuid();

    public ProjectProductRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ProjectProductRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectProductRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByProjectAsync_AndGetByProductAsync_ReturnTheirLinks()
    {
        // Arrange — a project serves several products, a product is served by several projects
        await _repository.AddAsync(new ProjectProduct(_projectA, _matching));
        await _repository.AddAsync(new ProjectProduct(_projectA, _ipi));
        await _repository.AddAsync(new ProjectProduct(_projectB, _matching));

        // Assert
        (await _repository.GetByProjectAsync(_projectA)).Select(l => l.ProductId).Should().BeEquivalentTo([_matching, _ipi]);
        (await _repository.GetByProductAsync(_matching)).Select(l => l.ProjectId).Should().BeEquivalentTo([_projectA, _projectB]);
        (await _repository.GetAllAsync()).Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsync_SameProjectAndProductTwice_Throws()
    {
        // Arrange
        await _repository.AddAsync(new ProjectProduct(_projectA, _matching));

        // Act
        var act = () => _repository.AddAsync(new ProjectProduct(_projectA, _matching));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteByProjectAsync_RemovesOnlyThatProjectsLinks()
    {
        // Arrange
        await _repository.AddAsync(new ProjectProduct(_projectA, _matching));
        await _repository.AddAsync(new ProjectProduct(_projectB, _matching));

        // Act
        await _repository.DeleteByProjectAsync(_projectA);

        // Assert
        (await _repository.GetAllAsync()).Should().ContainSingle(l => l.ProjectId == _projectB);
    }

    [Fact]
    public async Task DeleteByProductAsync_RemovesOnlyThatProductsLinks()
    {
        // Arrange
        await _repository.AddAsync(new ProjectProduct(_projectA, _matching));
        await _repository.AddAsync(new ProjectProduct(_projectA, _ipi));

        // Act
        await _repository.DeleteByProductAsync(_matching);

        // Assert
        (await _repository.GetAllAsync()).Should().ContainSingle(l => l.ProductId == _ipi);
    }
}
