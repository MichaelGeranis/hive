using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ChecklistTemplateTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTemplate()
    {
        // Arrange
        var name = "Interview Template";
        var description = "Standard interview checklist";
        var type = ChecklistType.Interview;

        // Act
        var template = new ChecklistTemplate(name, description, type);

        // Assert
        template.Id.Should().NotBeEmpty();
        template.Name.Should().Be(name);
        template.Description.Should().Be(description);
        template.Type.Should().Be(type);
        template.IsActive.Should().BeTrue();
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        template.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var template = new ChecklistTemplate(
            "  Template Name  ",
            "  Description  ",
            ChecklistType.Onboarding);

        // Assert
        template.Name.Should().Be("Template Name");
        template.Description.Should().Be("Description");
    }

    [Fact]
    public void Constructor_WithNullDescription_DefaultsToEmpty()
    {
        // Act
        var template = new ChecklistTemplate("Name", null!, ChecklistType.Interview);

        // Assert
        template.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new ChecklistTemplate(name!, "Description", ChecklistType.Interview);

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
        var act = () => new ChecklistTemplate(longName, "Description", ChecklistType.Interview);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var template = new ChecklistTemplate("Original", "Original desc", ChecklistType.Interview);
        var newName = "Updated Name";
        var newDescription = "Updated description";

        // Act
        template.Update(newName, newDescription);

        // Assert
        template.Name.Should().Be(newName);
        template.Description.Should().Be(newDescription);
        template.UpdatedAt.Should().NotBeNull();
        template.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var template = new ChecklistTemplate("Original", "Desc", ChecklistType.Interview);

        // Act
        var act = () => template.Update("", "New desc");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var template = new ChecklistTemplate("Name", "Desc", ChecklistType.Interview);
        template.Deactivate();

        // Act
        template.Activate();

        // Assert
        template.IsActive.Should().BeTrue();
        template.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var template = new ChecklistTemplate("Name", "Desc", ChecklistType.Interview);

        // Act
        template.Deactivate();

        // Assert
        template.IsActive.Should().BeFalse();
        template.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ChecklistType.Interview)]
    [InlineData(ChecklistType.Onboarding)]
    public void Constructor_WithAllChecklistTypes_CreatesTemplate(ChecklistType type)
    {
        // Act
        var template = new ChecklistTemplate("Name", "Desc", type);

        // Assert
        template.Type.Should().Be(type);
    }
}
