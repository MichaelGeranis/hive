using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class InitiativeMemberTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInitiativeMember()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var directReportId = Guid.NewGuid();

        // Act
        var member = new InitiativeMember(initiativeId, directReportId);

        // Assert
        member.Id.Should().NotBeEmpty();
        member.InitiativeId.Should().Be(initiativeId);
        member.DirectReportId.Should().Be(directReportId);
        member.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        // Act
        var member1 = new InitiativeMember(Guid.NewGuid(), Guid.NewGuid());
        var member2 = new InitiativeMember(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        member1.Id.Should().NotBe(member2.Id);
    }
}
