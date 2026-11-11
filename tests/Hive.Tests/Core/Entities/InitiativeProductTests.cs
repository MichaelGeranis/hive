using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class InitiativeProductTests
{
    [Fact]
    public void Constructor_ValidIds_LinksThem()
    {
        var initiativeId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var link = new InitiativeProduct(initiativeId, productId);

        link.InitiativeId.Should().Be(initiativeId);
        link.ProductId.Should().Be(productId);
    }

    [Fact]
    public void Constructor_EmptyProduct_Throws()
    {
        var act = () => new InitiativeProduct(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyInitiative_Throws()
    {
        var act = () => new InitiativeProduct(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}
