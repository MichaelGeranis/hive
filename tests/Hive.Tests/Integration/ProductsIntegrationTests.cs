using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// Products, projects and tasks through the real DI container and the in-memory database.
/// </summary>
public class ProductsIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AProjectServesProductsAndAProductCountsTheTasksNamingIt()
    {
        // Arrange
        var products = GetService<IProductService>();
        var projects = GetService<IProjectService>();
        var tasks = GetService<ITeamTaskRepository>();

        var matching = await products.CreateAsync(new CreateProductDto { Name = "Matching" });
        var ipi = await products.CreateAsync(new CreateProductDto { Name = "IPI" });
        var project = await projects.CreateAsync(new CreateProjectDto
        {
            Name = "matching-api",
            Url = "https://github.com/acme/matching-api",
            ProductIds = new List<Guid> { matching.Id, ipi.Id }
        });
        await tasks.AddAsync(new TeamTask("Fix matching", components: "Matching"));
        await tasks.AddAsync(new TeamTask("Shared work", components: "matching,IPI"));

        // Act
        var all = await products.GetAllAsync();

        // Assert
        project.Products.Select(p => p.Name).Should().Equal("IPI", "Matching");
        var matchingDto = all.Single(p => p.Id == matching.Id);
        matchingDto.TotalTasks.Should().Be(2);
        matchingDto.Projects.Should().ContainSingle(p => p.Name == "matching-api");
        all.Single(p => p.Id == ipi.Id).TotalTasks.Should().Be(1);
    }

    [Fact]
    public async Task DeletingAProject_UnlinksItFromItsProducts()
    {
        // Arrange
        var products = GetService<IProductService>();
        var projects = GetService<IProjectService>();
        var matching = await products.CreateAsync(new CreateProductDto { Name = "Matching" });
        var project = await projects.CreateAsync(new CreateProjectDto { Name = "matching-api", ProductIds = new List<Guid> { matching.Id } });

        // Act
        await projects.DeleteAsync(project.Id);

        // Assert
        (await products.GetByIdAsync(matching.Id))!.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncProducts_BeforeTheSpaceKeyIsSet_ExplainsWhatToSetUp()
    {
        // Arrange
        var sync = GetService<IJiraSyncService>();

        // Act
        var act = () => sync.SyncProductsAsync();

        // Assert
        await act.Should().ThrowAsync<Hive.Core.Exceptions.ValidationException>()
            .WithMessage("Jira sync is not configured*");
    }
}
