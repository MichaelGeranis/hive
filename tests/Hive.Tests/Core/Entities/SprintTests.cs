using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SprintTests
{
    [Fact]
    public void Constructor_WithValidStandardName_CreatesSprint()
    {
        // Arrange & Act
        var sprint = new Sprint("LP_4Q25_S6");

        // Assert
        sprint.Id.Should().NotBeEmpty();
        sprint.Name.Should().Be("LP_4Q25_S6");
        sprint.TeamName.Should().Be("LP");
        sprint.Quarter.Should().Be(4);
        sprint.Year.Should().Be(2025);
        sprint.SprintNumber.Should().Be(6);
        sprint.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sprint.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("TEAM_1Q24_S1", "TEAM", 1, 2024, 1)]
    [InlineData("ABC_2Q23_S10", "ABC", 2, 2023, 10)]
    [InlineData("XYZ_3Q26_S99", "XYZ", 3, 2026, 99)]
    [InlineData("A_4Q25_S5", "A", 4, 2025, 5)]
    public void Constructor_WithVariousValidNames_ParsesCorrectly(
        string sprintName,
        string expectedTeam,
        int expectedQuarter,
        int expectedYear,
        int expectedSprintNumber)
    {
        // Act
        var sprint = new Sprint(sprintName);

        // Assert
        sprint.TeamName.Should().Be(expectedTeam);
        sprint.Quarter.Should().Be(expectedQuarter);
        sprint.Year.Should().Be(expectedYear);
        sprint.SprintNumber.Should().Be(expectedSprintNumber);
    }

    [Fact]
    public void Constructor_WithNonStandardName_UsesNameAsTeamName()
    {
        // Arrange & Act
        var sprint = new Sprint("Custom Sprint Name");

        // Assert
        sprint.Name.Should().Be("Custom Sprint Name");
        sprint.TeamName.Should().Be("Custom Sprint Name");
        sprint.Quarter.Should().Be(0);
        sprint.Year.Should().Be(0);
        sprint.SprintNumber.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Act
        var act = () => new Sprint(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Sprint name cannot be empty.*");
    }

    [Fact]
    public void Constructor_WithNameExceeding100Characters_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => new Sprint(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Sprint name cannot exceed 100 characters.*");
    }

    [Fact]
    public void Update_WithValidName_UpdatesSprintAndReparsesName()
    {
        // Arrange
        var sprint = new Sprint("LP_1Q25_S1");

        // Act
        sprint.Update("LP_2Q25_S5");

        // Assert
        sprint.Name.Should().Be("LP_2Q25_S5");
        sprint.TeamName.Should().Be("LP");
        sprint.Quarter.Should().Be(2);
        sprint.Year.Should().Be(2025);
        sprint.SprintNumber.Should().Be(5);
        sprint.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetSortOrder_ReturnsCorrectValue()
    {
        // Arrange
        var sprint = new Sprint("LP_4Q25_S6");

        // Act
        var sortOrder = sprint.GetSortOrder();

        // Assert
        // Format: YYYYQSS (2025406 for Q4 2025 Sprint 6)
        sortOrder.Should().Be(2025406);
    }

    [Theory]
    [InlineData("LP_1Q25_S1", 2025101)]
    [InlineData("LP_2Q25_S5", 2025205)]
    [InlineData("LP_3Q24_S10", 2024310)]
    [InlineData("LP_4Q26_S99", 2026499)]
    public void GetSortOrder_WithVariousSprints_ReturnsCorrectValues(string sprintName, int expectedSortOrder)
    {
        // Arrange
        var sprint = new Sprint(sprintName);

        // Act
        var sortOrder = sprint.GetSortOrder();

        // Assert
        sortOrder.Should().Be(expectedSortOrder);
    }

    [Fact]
    public void IsBefore_WhenSprintIsEarlier_ReturnsTrue()
    {
        // Arrange
        var earlierSprint = new Sprint("LP_1Q25_S1");
        var laterSprint = new Sprint("LP_2Q25_S1");

        // Act & Assert
        earlierSprint.IsBefore(laterSprint).Should().BeTrue();
    }

    [Fact]
    public void IsBefore_WhenSprintIsLater_ReturnsFalse()
    {
        // Arrange
        var earlierSprint = new Sprint("LP_1Q25_S1");
        var laterSprint = new Sprint("LP_2Q25_S1");

        // Act & Assert
        laterSprint.IsBefore(earlierSprint).Should().BeFalse();
    }

    [Fact]
    public void IsBefore_WhenSameQuarterDifferentSprintNumber_WorksCorrectly()
    {
        // Arrange
        var sprint1 = new Sprint("LP_1Q25_S1");
        var sprint5 = new Sprint("LP_1Q25_S5");

        // Act & Assert
        sprint1.IsBefore(sprint5).Should().BeTrue();
        sprint5.IsBefore(sprint1).Should().BeFalse();
    }

    [Fact]
    public void IsAfter_WhenSprintIsLater_ReturnsTrue()
    {
        // Arrange
        var earlierSprint = new Sprint("LP_1Q25_S1");
        var laterSprint = new Sprint("LP_2Q25_S1");

        // Act & Assert
        laterSprint.IsAfter(earlierSprint).Should().BeTrue();
    }

    [Fact]
    public void IsAfter_WhenSprintIsEarlier_ReturnsFalse()
    {
        // Arrange
        var earlierSprint = new Sprint("LP_1Q25_S1");
        var laterSprint = new Sprint("LP_2Q25_S1");

        // Act & Assert
        earlierSprint.IsAfter(laterSprint).Should().BeFalse();
    }

    [Fact]
    public void IsAfter_AcrossDifferentYears_WorksCorrectly()
    {
        // Arrange
        var sprint2024 = new Sprint("LP_4Q24_S6");
        var sprint2025 = new Sprint("LP_1Q25_S1");

        // Act & Assert
        sprint2025.IsAfter(sprint2024).Should().BeTrue();
        sprint2024.IsAfter(sprint2025).Should().BeFalse();
    }

    [Fact]
    public void Comparison_WithNonStandardNames_WorksBasedOnParsedValues()
    {
        // Arrange
        var nonStandardSprint = new Sprint("Custom Sprint");
        var standardSprint = new Sprint("LP_1Q25_S1");

        // Act & Assert
        // Non-standard sprint has Year=0, Quarter=0, SprintNumber=0
        // So it should be "before" any standard sprint
        nonStandardSprint.IsBefore(standardSprint).Should().BeTrue();
        standardSprint.IsAfter(nonStandardSprint).Should().BeTrue();
    }
}
