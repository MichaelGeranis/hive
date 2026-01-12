using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesProject()
    {
        // Arrange
        var name = "API Redesign";
        var description = "Redesign the REST API";
        var labels = "backend,api";
        var url = "https://github.com/org/api-redesign";

        // Act
        var project = new Project(name, description, labels, url);

        // Assert
        project.Id.Should().NotBeEmpty();
        project.Name.Should().Be(name);
        project.Description.Should().Be(description);
        project.Labels.Should().Be(labels);
        project.Url.Should().Be(url);
        project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithOnlyName_UsesDefaults()
    {
        // Act
        var project = new Project("Simple Project");

        // Assert
        project.Description.Should().BeEmpty();
        project.Labels.Should().BeEmpty();
        project.Url.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var project = new Project("  Project Name  ", "  Description  ");

        // Assert
        project.Name.Should().Be("Project Name");
        project.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Project(name!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act
        var act = () => new Project(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var project = new Project("Original", "Original desc");

        // Act
        project.Update("Updated", "Updated desc", "new-label", "https://github.com/new");

        // Assert
        project.Name.Should().Be("Updated");
        project.Description.Should().Be("Updated desc");
        project.Labels.Should().Be("new-label");
        project.Url.Should().Be("https://github.com/new");
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var project = new Project("Original");

        // Act
        var act = () => project.Update("", null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithNullDescription_SetsEmptyDescription()
    {
        // Arrange
        var project = new Project("Test", "Original desc");

        // Act
        project.Update("Test", null);

        // Assert
        project.Description.Should().BeEmpty();
    }

    [Fact]
    public void Update_TrimsValues()
    {
        // Arrange
        var project = new Project("Test");

        // Act
        project.Update("  Updated  ", "  Desc  ", "  label  ", "  https://test.com  ");

        // Assert
        project.Name.Should().Be("Updated");
        project.Description.Should().Be("Desc");
        project.Labels.Should().Be("label");
        project.Url.Should().Be("https://test.com");
    }
}
