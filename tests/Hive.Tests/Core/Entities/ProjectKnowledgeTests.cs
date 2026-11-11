using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ProjectKnowledgeTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesProjectKnowledge()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var knowledgeLevel = 3;

        // Act
        var knowledge = new ProjectKnowledge(directReportId, projectId, knowledgeLevel);

        // Assert
        knowledge.Id.Should().NotBeEmpty();
        knowledge.DirectReportId.Should().Be(directReportId);
        knowledge.ProjectId.Should().Be(projectId);
        knowledge.KnowledgeLevel.Should().Be(knowledgeLevel);
        knowledge.UpdatedAt.Should().NotBeNull();
        knowledge.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new ProjectKnowledge(Guid.Empty, Guid.NewGuid(), 3);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Fact]
    public void Constructor_WithEmptyProjectId_ThrowsArgumentException()
    {
        // Act
        var act = () => new ProjectKnowledge(Guid.NewGuid(), Guid.Empty, 3);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("projectId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Constructor_WithInvalidKnowledgeLevel_ThrowsArgumentException(int level)
    {
        // Act
        var act = () => new ProjectKnowledge(Guid.NewGuid(), Guid.NewGuid(), level);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("knowledgeLevel");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Constructor_WithValidKnowledgeLevels_CreatesProjectKnowledge(int level)
    {
        // Act
        var knowledge = new ProjectKnowledge(Guid.NewGuid(), Guid.NewGuid(), level);

        // Assert
        knowledge.KnowledgeLevel.Should().Be(level);
    }

    [Fact]
    public void Update_WithValidLevel_UpdatesKnowledgeLevel()
    {
        // Arrange
        var knowledge = new ProjectKnowledge(Guid.NewGuid(), Guid.NewGuid(), 2);
        var originalUpdatedAt = knowledge.UpdatedAt;

        // Small delay to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        knowledge.Update(4);

        // Assert
        knowledge.KnowledgeLevel.Should().Be(4);
        knowledge.UpdatedAt.Should().BeAfter(originalUpdatedAt!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Update_WithInvalidLevel_ThrowsArgumentException(int level)
    {
        // Arrange
        var knowledge = new ProjectKnowledge(Guid.NewGuid(), Guid.NewGuid(), 3);

        // Act
        var act = () => knowledge.Update(level);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("knowledgeLevel");
    }

    [Theory]
    [InlineData(1, "No clue")]
    [InlineData(2, "Limited Knowledge")]
    [InlineData(3, "Moderate Knowledge")]
    [InlineData(4, "Good Knowledge")]
    [InlineData(5, "Confident")]
    public void GetKnowledgeLevelLabel_ReturnsCorrectLabel(int level, string expectedLabel)
    {
        // Arrange
        var knowledge = new ProjectKnowledge(Guid.NewGuid(), Guid.NewGuid(), level);

        // Act
        var label = knowledge.GetKnowledgeLevelLabel();

        // Assert
        label.Should().Be(expectedLabel);
    }
}
