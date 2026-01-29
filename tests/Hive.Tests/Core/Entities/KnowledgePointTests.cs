using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class KnowledgePointTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesKnowledgePoint()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var manualPoints = 5;
        var notes = "Initial points";

        // Act
        var knowledgePoint = new KnowledgePoint(directReportId, projectId, manualPoints, notes);

        // Assert
        knowledgePoint.Id.Should().NotBeEmpty();
        knowledgePoint.DirectReportId.Should().Be(directReportId);
        knowledgePoint.ProjectId.Should().Be(projectId);
        knowledgePoint.ManualPoints.Should().Be(manualPoints);
        knowledgePoint.Notes.Should().Be(notes);
        knowledgePoint.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        knowledgePoint.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaultManualPoints_CreatesKnowledgePoint()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        // Act
        var knowledgePoint = new KnowledgePoint(directReportId, projectId);

        // Assert
        knowledgePoint.ManualPoints.Should().Be(0);
        knowledgePoint.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new KnowledgePoint(Guid.Empty, Guid.NewGuid(), 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Fact]
    public void Constructor_WithEmptyProjectId_ThrowsArgumentException()
    {
        // Act
        var act = () => new KnowledgePoint(Guid.NewGuid(), Guid.Empty, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("projectId");
    }

    [Fact]
    public void Constructor_WithNegativeManualPoints_ThrowsArgumentException()
    {
        // Act
        var act = () => new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("manualPoints");
    }

    [Fact]
    public void AddPoints_WithPositiveValue_AddsToManualPoints()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        knowledgePoint.AddPoints(5, "Completed code review");

        // Assert
        knowledgePoint.ManualPoints.Should().Be(15);
        knowledgePoint.Notes.Should().Be("Completed code review");
        knowledgePoint.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddPoints_AppendsNotes_WhenExistingNotesPresent()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10, "Initial note");

        // Act
        knowledgePoint.AddPoints(5, "Second contribution");

        // Assert
        knowledgePoint.Notes.Should().Be("Initial note\nSecond contribution");
    }

    [Fact]
    public void AddPoints_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        var act = () => knowledgePoint.AddPoints(-5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("points");
    }

    [Fact]
    public void AddPoints_WithZeroValue_DoesNotChangePoints()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        knowledgePoint.AddPoints(0, "Note");

        // Assert
        knowledgePoint.ManualPoints.Should().Be(10);
        knowledgePoint.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateManualPoints_WithValidValue_UpdatesPoints()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10, "Old notes");

        // Act
        knowledgePoint.UpdateManualPoints(25, "New notes");

        // Assert
        knowledgePoint.ManualPoints.Should().Be(25);
        knowledgePoint.Notes.Should().Be("New notes");
        knowledgePoint.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateManualPoints_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        var act = () => knowledgePoint.UpdateManualPoints(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("manualPoints");
    }

    [Fact]
    public void UpdateManualPoints_WithZero_SetsPointsToZero()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        knowledgePoint.UpdateManualPoints(0);

        // Assert
        knowledgePoint.ManualPoints.Should().Be(0);
    }

    [Fact]
    public void Constructor_TrimsNotes()
    {
        // Arrange & Act
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 5, "  Test note  ");

        // Assert
        knowledgePoint.Notes.Should().Be("Test note");
    }

    [Fact]
    public void AddPoints_TrimsNotes()
    {
        // Arrange
        var knowledgePoint = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 5);

        // Act
        knowledgePoint.AddPoints(1, "  Test note  ");

        // Assert
        knowledgePoint.Notes.Should().Be("Test note");
    }
}
