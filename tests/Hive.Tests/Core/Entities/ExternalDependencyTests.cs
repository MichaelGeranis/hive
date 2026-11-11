using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ExternalDependencyTests
{
    private static readonly Guid InitiativeId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidData_NeedsCoordinationByDefault()
    {
        var dependency = new ExternalDependency(InitiativeId, " CORE ", " Release plan ");

        dependency.Team.Should().Be("CORE");
        dependency.Need.Should().Be("Release plan");
        dependency.Status.Should().Be(ExternalDependencyStatus.NeedsCoordination);
        dependency.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_NoTeam_Throws(string team)
    {
        var act = () => new ExternalDependency(InitiativeId, team);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NeedTooLong_Throws()
    {
        var act = () => new ExternalDependency(InitiativeId, "CS", new string('x', 501));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_UnknownStatus_Throws()
    {
        var act = () => new ExternalDependency(InitiativeId, "CS", null, (ExternalDependencyStatus)7);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(ExternalDependencyStatus.NeedsCoordination, true)]
    [InlineData(ExternalDependencyStatus.AtRisk, true)]
    [InlineData(ExternalDependencyStatus.Agreed, false)]
    [InlineData(ExternalDependencyStatus.Done, false)]
    public void IsOpen_ByStatus(ExternalDependencyStatus status, bool open)
    {
        var dependency = new ExternalDependency(InitiativeId, "CS");

        dependency.Update("CS", null, status);

        dependency.IsOpen.Should().Be(open);
    }
}
