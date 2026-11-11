using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesProductMadeByHand()
    {
        // Act
        var product = new Product("  Rights Manager  ", "  Owns rights data  ");

        // Assert
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be("Rights Manager");
        product.Description.Should().Be("Owns rights data");
        product.JiraComponentId.Should().BeNull();
        product.IsFromJira.Should().BeFalse();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        // Act
        var act = () => new Product(name!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameOver255Characters_Throws()
    {
        // Act
        var act = () => new Product(new string('a', 256));

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithCommaInName_Throws()
    {
        // Act — tasks list components separated by commas, so the name would split
        var act = () => new Product("Matching, IPI");

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithDescriptionOver4000Characters_Throws()
    {
        // Act
        var act = () => new Product("Matching", new string('a', 4001));

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void FromJira_LinksTheComponent()
    {
        // Act
        var product = Product.FromJira(" 10042 ", "Matching", null);

        // Assert
        product.JiraComponentId.Should().Be("10042");
        product.IsFromJira.Should().BeTrue();
        product.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void FromJira_WithoutComponentId_Throws(string componentId)
    {
        // Act
        var act = () => Product.FromJira(componentId, "Matching", null);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("componentId");
    }

    [Fact]
    public void Update_ProductMadeByHand_ChangesNameAndDescription()
    {
        // Arrange
        var product = new Product("Matching");

        // Act
        product.Update("Matching Engine", "Matches works");

        // Assert
        product.Name.Should().Be("Matching Engine");
        product.Description.Should().Be("Matches works");
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_ProductFromJira_Throws()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", null);

        // Act — Jira owns the name and description
        var act = () => product.Update("Renamed", null);

        // Assert
        act.Should().Throw<InvalidOperationException>();
        product.Name.Should().Be("Matching");
    }

    [Fact]
    public void SyncFromJira_RenamesTheProduct()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", null);

        // Act
        product.SyncFromJira("10042", "Matching Engine", "Matches works");

        // Assert
        product.Name.Should().Be("Matching Engine");
        product.Description.Should().Be("Matches works");
    }

    [Fact]
    public void SyncFromJira_LinksAProductMadeByHand()
    {
        // Arrange
        var product = new Product("Matching");

        // Act
        product.SyncFromJira("10042", "Matching", null);

        // Assert
        product.JiraComponentId.Should().Be("10042");
    }

    [Fact]
    public void SyncFromJira_WithAnotherComponent_Throws()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", null);

        // Act
        var act = () => product.SyncFromJira("10043", "Matching", null);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MatchesJira_ComparesIdNameAndDescription()
    {
        // Arrange
        var product = Product.FromJira("10042", "Matching", "Matches works");

        // Assert
        product.MatchesJira("10042", " Matching ", "Matches works ").Should().BeTrue();
        product.MatchesJira("10042", "Matching Engine", "Matches works").Should().BeFalse();
        product.MatchesJira("10042", "Matching", null).Should().BeFalse();
        new Product("Matching", "Matches works").MatchesJira("10042", "Matching", "Matches works")
            .Should().BeFalse("it is not linked yet");
    }

    [Theory]
    [InlineData("Matching", true)]
    [InlineData("matching", true)]
    [InlineData("IPI, Matching ", true)]
    [InlineData("Matching Engine", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsNamedIn_MatchesAWholeComponentIgnoringCase(string? components, bool expected)
    {
        // Arrange
        var product = new Product("Matching");

        // Act & Assert
        product.IsNamedIn(components).Should().Be(expected);
    }
}
