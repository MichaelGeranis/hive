using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class InitiativeDependencyTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesDependency()
    {
        // Arrange
        var dependentId = Guid.NewGuid();
        var dependencyId = Guid.NewGuid();
        var type = DependencyType.FinishToStart;
        var notes = "Must complete API before frontend";

        // Act
        var dependency = new InitiativeDependency(dependentId, dependencyId, type, notes);

        // Assert
        dependency.Id.Should().NotBeEmpty();
        dependency.DependentInitiativeId.Should().Be(dependentId);
        dependency.DependencyInitiativeId.Should().Be(dependencyId);
        dependency.Type.Should().Be(type);
        dependency.Notes.Should().Be(notes);
        dependency.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithDefaultType_DefaultsToFinishToStart()
    {
        // Arrange & Act
        var dependency = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        dependency.Type.Should().Be(DependencyType.FinishToStart);
    }

    [Fact]
    public void Constructor_WithNullNotes_DefaultsToEmpty()
    {
        // Act
        var dependency = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        dependency.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var dependency = new InitiativeDependency(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DependencyType.FinishToStart,
            "  Some notes  ");

        // Assert
        dependency.Notes.Should().Be("Some notes");
    }

    [Fact]
    public void Constructor_WithSameInitiativeIds_ThrowsArgumentException()
    {
        // Arrange
        var sameId = Guid.NewGuid();

        // Act
        var act = () => new InitiativeDependency(sameId, sameId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("dependentInitiativeId");
    }

    [Theory]
    [InlineData(DependencyType.FinishToStart)]
    [InlineData(DependencyType.StartToStart)]
    [InlineData(DependencyType.FinishToFinish)]
    public void Constructor_WithAllDependencyTypes_CreatesDependency(DependencyType type)
    {
        // Act
        var dependency = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid(), type);

        // Assert
        dependency.Type.Should().Be(type);
    }

    [Fact]
    public void UpdateNotes_UpdatesNotesProperty()
    {
        // Arrange
        var dependency = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());
        var newNotes = "Updated notes";

        // Act
        dependency.UpdateNotes(newNotes);

        // Assert
        dependency.Notes.Should().Be(newNotes);
    }

    [Fact]
    public void UpdateNotes_WithNull_ClearsNotes()
    {
        // Arrange
        var dependency = new InitiativeDependency(
            Guid.NewGuid(), Guid.NewGuid(), DependencyType.FinishToStart, "Original notes");

        // Act
        dependency.UpdateNotes(null);

        // Assert
        dependency.Notes.Should().BeEmpty();
    }

    [Fact]
    public void UpdateNotes_TrimsWhitespace()
    {
        // Arrange
        var dependency = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());

        // Act
        dependency.UpdateNotes("  Trimmed notes  ");

        // Assert
        dependency.Notes.Should().Be("Trimmed notes");
    }

    [Fact]
    public void Constructor_AllowsMultipleDependenciesFromSameInitiative()
    {
        // Arrange
        var dependentId = Guid.NewGuid();
        var dependency1Id = Guid.NewGuid();
        var dependency2Id = Guid.NewGuid();

        // Act
        var dep1 = new InitiativeDependency(dependentId, dependency1Id);
        var dep2 = new InitiativeDependency(dependentId, dependency2Id);

        // Assert
        dep1.DependentInitiativeId.Should().Be(dependentId);
        dep2.DependentInitiativeId.Should().Be(dependentId);
        dep1.DependencyInitiativeId.Should().NotBe(dep2.DependencyInitiativeId);
    }

    [Fact]
    public void Constructor_CreatesUniqueIds()
    {
        // Act
        var dep1 = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());
        var dep2 = new InitiativeDependency(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        dep1.Id.Should().NotBe(dep2.Id);
    }
}
