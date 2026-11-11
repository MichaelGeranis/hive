using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SprintGoalTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesSprintGoal()
    {
        // Arrange
        var quarterId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        var goal = "Complete API implementation";
        var notes = "Focus on performance";

        // Act
        var sprintGoal = new SprintGoal(quarterId, sprintId, goal, notes);

        // Assert
        sprintGoal.Id.Should().NotBeEmpty();
        sprintGoal.QuarterId.Should().Be(quarterId);
        sprintGoal.SprintId.Should().Be(sprintId);
        sprintGoal.Goal.Should().Be(goal);
        sprintGoal.Notes.Should().Be(notes);
        sprintGoal.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sprintGoal.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMinimalData_CreatesSprintGoal()
    {
        // Arrange & Act
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        sprintGoal.Goal.Should().BeEmpty();
        sprintGoal.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var sprintGoal = new SprintGoal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Goal text  ",
            "  Notes text  ");

        // Assert
        sprintGoal.Goal.Should().Be("Goal text");
        sprintGoal.Notes.Should().Be("Notes text");
    }

    [Fact]
    public void Constructor_WithNullGoal_DefaultsToEmpty()
    {
        // Act
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), null);

        // Assert
        sprintGoal.Goal.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithGoalTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longGoal = new string('a', 4001);

        // Act
        var act = () => new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), longGoal);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("goal");
    }

    [Fact]
    public void Constructor_WithGoalAtMaxLength_CreatesSprintGoal()
    {
        // Arrange
        var maxLengthGoal = new string('a', 4000);

        // Act
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), maxLengthGoal);

        // Assert
        sprintGoal.Goal.Should().HaveLength(4000);
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), "Original goal");
        var newGoal = "Updated goal";
        var newNotes = "Updated notes";

        // Act
        sprintGoal.Update(newGoal, newNotes);

        // Assert
        sprintGoal.Goal.Should().Be(newGoal);
        sprintGoal.Notes.Should().Be(newNotes);
        sprintGoal.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithNullGoal_ClearsGoal()
    {
        // Arrange
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), "Some goal");

        // Act
        sprintGoal.Update(null, null);

        // Assert
        sprintGoal.Goal.Should().BeEmpty();
        sprintGoal.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithGoalTooLong_ThrowsArgumentException()
    {
        // Arrange
        var sprintGoal = new SprintGoal(Guid.NewGuid(), Guid.NewGuid(), "Original");
        var longGoal = new string('a', 4001);

        // Act
        var act = () => sprintGoal.Update(longGoal, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("goal");
    }

    [Fact]
    public void Constructor_CreatesUniqueIds()
    {
        // Arrange
        var quarterId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();

        // Act
        var goal1 = new SprintGoal(quarterId, sprintId, "Goal 1");
        var goal2 = new SprintGoal(quarterId, sprintId, "Goal 2");

        // Assert
        goal1.Id.Should().NotBe(goal2.Id);
    }
}
