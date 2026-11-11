using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IProjectProductRepository> _projectProductRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock = new();
    private readonly Mock<IParentRepository> _parentRepositoryMock = new();
    private readonly Mock<IActivityService> _activityServiceMock = new();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(
            _productRepositoryMock.Object,
            _projectProductRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _parentRepositoryMock.Object,
            _activityServiceMock.Object);

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamTask>());
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Parent>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project>());
        _projectProductRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProjectProduct>());
        _productRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);
    }

    [Fact]
    public void Constructor_WithNullProductRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProductService(null!, _projectProductRepositoryMock.Object, _projectRepositoryMock.Object,
            _taskRepositoryMock.Object, _parentRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public async Task GetAllAsync_CountsTheTasksAndParentsWhoseComponentsNameTheProduct()
    {
        // Arrange
        var matching = new Product("Matching");
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { matching });

        var done = Task("Done", "Matching", TaskStatus.Done);
        var wontDo = Task("Won't do", "IPI, matching", TaskStatus.Cancelled);
        var open = Task("Open", "Matching", TaskStatus.InProgress);
        var elsewhere = Task("Elsewhere", "IPI", TaskStatus.Done);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { done, wontDo, open, elsewhere });
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Parent>
        {
            Parent.FromJira("MAR-1", "Epic", "Payments", new[] { "Matching" }),
            Parent.FromJira("MAR-2", "Epic", "Rights", new[] { "IPI" })
        });

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.TotalTasks.Should().Be(3);
        dto.CompletedTasks.Should().Be(2, "a Won't Do task is resolved");
        dto.OpenTasks.Should().Be(1);
        dto.ParentCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ListsTheProjectsServingTheProduct()
    {
        // Arrange
        var matching = new Product("Matching");
        var repo = new Project("matching-api", url: "https://github.com/acme/matching-api");
        var cli = new Project("cli");
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { matching });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project> { repo, cli });
        _projectProductRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectProduct> { new(repo.Id, matching.Id), new(cli.Id, Guid.NewGuid()) });

        // Act
        var dto = (await _service.GetAllAsync()).Single();

        // Assert
        dto.Projects.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ProductProjectDto { Id = repo.Id, Name = "matching-api", Url = "https://github.com/acme/matching-api" });
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAProductMadeByHand()
    {
        // Act
        var dto = await _service.CreateAsync(new CreateProductDto { Name = " Internal tooling ", Description = "Scripts" });

        // Assert
        dto.Name.Should().Be("Internal tooling");
        dto.IsFromJira.Should().BeFalse();
        _activityServiceMock.Verify(a => a.LogActivityAsync(ActivityType.Created, EntityType.Product, dto.Id,
            "Internal tooling", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsConflict()
    {
        // Arrange
        _productRepositoryMock.Setup(r => r.NameExistsAsync("Matching", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(new CreateProductDto { Name = "Matching" });

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_ProductFromJira_Throws()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", null);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var act = () => _service.UpdateAsync(product.Id, new UpdateProductDto { Name = "Renamed" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        // Act
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateProductDto { Name = "Renamed" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ProductMadeByHand_DeletesItAndItsProjectLinks()
    {
        // Arrange
        var product = new Product("Internal tooling");
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        await _service.DeleteAsync(product.Id);

        // Assert
        _projectProductRepositoryMock.Verify(r => r.DeleteByProductAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(r => r.DeleteAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProductFromJira_ThrowsAndKeepsIt()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", null);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        // Act
        var act = () => _service.DeleteAsync(product.Id);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("*comes from Jira*");
        _productRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TeamTask Task(string title, string components, TaskStatus status)
    {
        var task = new TeamTask(title, components: components);
        typeof(TeamTask).GetProperty(nameof(TeamTask.Status))!.SetValue(task, status);
        return task;
    }
}
