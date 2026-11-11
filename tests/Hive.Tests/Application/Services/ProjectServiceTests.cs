using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IProjectProductRepository> _projectProductRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly ProjectService _service;

    // What the link repository holds, so tests read back what the service wrote
    private readonly List<ProjectProduct> _links = new();

    public ProjectServiceTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _projectProductRepositoryMock = new Mock<IProjectProductRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new ProjectService(
            _projectRepositoryMock.Object,
            _productRepositoryMock.Object,
            _projectProductRepositoryMock.Object,
            _activityServiceMock.Object);

        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());
        _projectProductRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _links.ToList());
        _projectProductRepositoryMock.Setup(r => r.GetByProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, CancellationToken _) => _links.Where(l => l.ProjectId == projectId).ToList());
        _projectProductRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ProjectProduct>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectProduct link, CancellationToken _) =>
            {
                _links.Add(link);
                return link;
            });
        _projectProductRepositoryMock.Setup(r => r.DeleteByProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid projectId, CancellationToken _) => _links.RemoveAll(l => l.ProjectId == projectId))
            .Returns(Task.CompletedTask);
    }

    private void SetupProducts(params Product[] products)
    {
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());
        foreach (var product in products)
        {
            _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
        }
    }

    [Fact]
    public void Constructor_WithNullProjectRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(null!, _productRepositoryMock.Object, _projectProductRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectRepository");
    }

    [Fact]
    public void Constructor_WithNullProductRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(_projectRepositoryMock.Object, null!, _projectProductRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productRepository");
    }

    [Fact]
    public void Constructor_WithNullProjectProductRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(_projectRepositoryMock.Object, _productRepositoryMock.Object, null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("projectProductRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectService(_projectRepositoryMock.Object, _productRepositoryMock.Object, _projectProductRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var project = new Project("Test Project", "Description", "https://github.com/org/repo");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.GetByIdAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
        result.Name.Should().Be("Test Project");
        result.Url.Should().Be("https://github.com/org/repo");
        result.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ListsTheProductsEachProjectServes()
    {
        // Arrange
        var matching = new Product("Matching");
        var enrichment = new Product("Enrichment");
        SetupProducts(matching, enrichment);

        var shared = new Project("shared-lib");
        var other = new Project("other");
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { shared, other });
        _links.Add(new ProjectProduct(shared.Id, matching.Id));
        _links.Add(new ProjectProduct(shared.Id, enrichment.Id));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Single(p => p.Id == shared.Id).Products.Select(p => p.Name)
            .Should().Equal("Enrichment", "Matching");
        result.Single(p => p.Id == other.Id).Products.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesProjectServingItsProducts()
    {
        // Arrange
        var matching = new Product("Matching");
        SetupProducts(matching);
        var dto = new CreateProjectDto
        {
            Name = "New Project",
            Description = "Description",
            Url = "https://github.com/org/repo",
            ProductIds = new List<Guid> { matching.Id, matching.Id }
        };

        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Name.Should().Be(dto.Name);
        result.Url.Should().Be(dto.Url);
        result.Products.Should().ContainSingle(p => p.Id == matching.Id, "a repeated product is linked once");
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownProduct_ThrowsNotFoundAndCreatesNothing()
    {
        // Arrange
        var dto = new CreateProjectDto { Name = "New Project", ProductIds = new List<Guid> { Guid.NewGuid() } };
        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateProjectDto { Name = "Existing Project" };
        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesProjectAndReplacesItsProducts()
    {
        // Arrange
        var matching = new Product("Matching");
        var enrichment = new Product("Enrichment");
        SetupProducts(matching, enrichment);
        var project = new Project("Original");
        _links.Add(new ProjectProduct(project.Id, matching.Id));
        var dto = new UpdateProjectDto { Name = "Updated", Description = "New desc", ProductIds = new List<Guid> { enrichment.Id } };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _projectRepositoryMock.Setup(r => r.NameExistsAsync(dto.Name, project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(project.Id, dto);

        // Assert
        result.Name.Should().Be(dto.Name);
        result.Products.Select(p => p.Name).Should().Equal("Enrichment");
        _projectRepositoryMock.Verify(r => r.UpdateAsync(project, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateProjectDto { Name = "Test" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesProjectAndItsProductLinks()
    {
        // Arrange
        var project = new Project("Test Project", "Description");
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        await _service.DeleteAsync(project.Id);

        // Assert
        _projectProductRepositoryMock.Verify(r => r.DeleteByProjectAsync(project.Id, It.IsAny<CancellationToken>()), Times.Once);
        _projectRepositoryMock.Verify(r => r.DeleteAsync(project.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var act = () => _service.DeleteAsync(projectId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
