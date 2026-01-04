using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SprintCapacityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesSprintCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        // Act
        var capacity = new SprintCapacity(sprintId, 100, 5);

        // Assert
        capacity.Id.Should().NotBeEmpty();
        capacity.SprintId.Should().Be(sprintId);
        capacity.TotalCapacityPoints.Should().Be(100);
        capacity.AvailableMembers.Should().Be(5);
        capacity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        capacity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithZeroCapacity_CreatesSprintCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        // Act
        var capacity = new SprintCapacity(sprintId, 0, 0);

        // Assert
        capacity.TotalCapacityPoints.Should().Be(0);
        capacity.AvailableMembers.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentException()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        // Act
        var act = () => new SprintCapacity(sprintId, -1, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Capacity cannot be negative.*");
    }

    [Fact]
    public void Constructor_WithNegativeMembers_ThrowsArgumentException()
    {
        // Arrange
        var sprintId = Guid.NewGuid();

        // Act
        var act = () => new SprintCapacity(sprintId, 100, -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Available members cannot be negative.*");
    }

    [Fact]
    public void Update_WithValidData_UpdatesSprintCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, 100, 5);

        // Act
        capacity.Update(120, 6);

        // Assert
        capacity.TotalCapacityPoints.Should().Be(120);
        capacity.AvailableMembers.Should().Be(6);
        capacity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithNegativeCapacity_ThrowsArgumentException()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, 100, 5);

        // Act
        var act = () => capacity.Update(-10, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Capacity cannot be negative.*");
    }

    [Fact]
    public void Update_WithNegativeMembers_ThrowsArgumentException()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, 100, 5);

        // Act
        var act = () => capacity.Update(100, -2);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Available members cannot be negative.*");
    }

    [Fact]
    public void GetCapacityPerMember_WithValidData_ReturnsCorrectValue()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, 100, 5);

        // Act
        var capacityPerMember = capacity.GetCapacityPerMember();

        // Assert
        capacityPerMember.Should().Be(20.0);
    }

    [Fact]
    public void GetCapacityPerMember_WithZeroMembers_ReturnsZero()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, 100, 0);

        // Act
        var capacityPerMember = capacity.GetCapacityPerMember();

        // Assert
        capacityPerMember.Should().Be(0.0);
    }

    [Theory]
    [InlineData(100, 5, 20.0)]
    [InlineData(150, 3, 50.0)]
    [InlineData(75, 6, 12.5)]
    [InlineData(0, 5, 0.0)]
    [InlineData(100, 1, 100.0)]
    public void GetCapacityPerMember_WithVariousInputs_ReturnsCorrectValues(
        int totalCapacity,
        int members,
        double expectedCapacityPerMember)
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var capacity = new SprintCapacity(sprintId, totalCapacity, members);

        // Act
        var capacityPerMember = capacity.GetCapacityPerMember();

        // Assert
        capacityPerMember.Should().BeApproximately(expectedCapacityPerMember, 0.01);
    }
}
