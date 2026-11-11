using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ProjectProductTests
{
    [Fact]
    public void Constructor_LinksProjectToProduct()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var link = new ProjectProduct(projectId, productId);

        // Assert
        link.Id.Should().NotBeEmpty();
        link.ProjectId.Should().Be(projectId);
        link.ProductId.Should().Be(productId);
    }

    [Fact]
    public void Constructor_WithoutProject_Throws()
    {
        // Act
        var act = () => new ProjectProduct(Guid.Empty, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("projectId");
    }

    [Fact]
    public void Constructor_WithoutProduct_Throws()
    {
        // Act
        var act = () => new ProjectProduct(Guid.NewGuid(), Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }
}
