using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class AllocationTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAllocation()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var directReportId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();

        // Act
        var allocation = new Allocation(initiativeId, directReportId, sprintId);

        // Assert
        allocation.Id.Should().NotBeEmpty();
        allocation.InitiativeId.Should().Be(initiativeId);
        allocation.DirectReportId.Should().Be(directReportId);
        allocation.SprintId.Should().Be(sprintId);
        allocation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        allocation.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_NoPercent_TakesTheWholeSprint()
    {
        var allocation = new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        allocation.Percent.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Constructor_PercentOutOfRange_Throws(int percent)
    {
        var act = () => new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), percent);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPercent_Valid_SetsPercentAndUpdatedAt()
    {
        var allocation = new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        allocation.SetPercent(50);

        allocation.Percent.Should().Be(50);
        allocation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ValidateCell_SharesWithinSprint_DoesNotThrow()
    {
        var act = () => Allocation.ValidateCell(new[] { (Guid.NewGuid(), 50), (Guid.NewGuid(), 50) });

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateCell_BeyondWholeSprint_Throws()
    {
        var act = () => Allocation.ValidateCell(new[] { (Guid.NewGuid(), 60), (Guid.NewGuid(), 50) });

        act.Should().Throw<ArgumentException>().WithMessage("*beyond 100%*");
    }

    [Fact]
    public void ValidateCell_SameInitiativeTwice_Throws()
    {
        var id = Guid.NewGuid();
        var act = () => Allocation.ValidateCell(new[] { (id, 20), (id, 30) });

        act.Should().Throw<ArgumentException>().WithMessage("*only once*");
    }

    [Fact]
    public void Constructor_CreatesUniqueIds()
    {
        // Arrange & Act
        var allocation1 = new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var allocation2 = new Allocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        allocation1.Id.Should().NotBe(allocation2.Id);
    }

    [Fact]
    public void Constructor_AllowsSameInitiativeForDifferentSprints()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var directReportId = Guid.NewGuid();
        var sprint1Id = Guid.NewGuid();
        var sprint2Id = Guid.NewGuid();

        // Act
        var allocation1 = new Allocation(initiativeId, directReportId, sprint1Id);
        var allocation2 = new Allocation(initiativeId, directReportId, sprint2Id);

        // Assert
        allocation1.InitiativeId.Should().Be(initiativeId);
        allocation2.InitiativeId.Should().Be(initiativeId);
        allocation1.SprintId.Should().NotBe(allocation2.SprintId);
    }

    [Fact]
    public void Constructor_AllowsSameSprintForDifferentInitiatives()
    {
        // Arrange
        var initiative1Id = Guid.NewGuid();
        var initiative2Id = Guid.NewGuid();
        var directReportId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();

        // Act
        var allocation1 = new Allocation(initiative1Id, directReportId, sprintId);
        var allocation2 = new Allocation(initiative2Id, directReportId, sprintId);

        // Assert
        allocation1.SprintId.Should().Be(sprintId);
        allocation2.SprintId.Should().Be(sprintId);
        allocation1.InitiativeId.Should().NotBe(allocation2.InitiativeId);
    }
}
