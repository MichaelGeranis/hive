using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SkillCategoryEntityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesCategory()
    {
        // Arrange
        var name = "Technical Skills";
        var description = "Programming and technical abilities";
        var sortOrder = 1;

        // Act
        var category = new SkillCategoryEntity(name, description, sortOrder);

        // Assert
        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.SortOrder.Should().Be(sortOrder);
        category.IsActive.Should().BeTrue();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        category.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaultSortOrder_DefaultsToZero()
    {
        // Act
        var category = new SkillCategoryEntity("Name", "Description");

        // Assert
        category.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var category = new SkillCategoryEntity("  Category Name  ", "  Description  ");

        // Assert
        category.Name.Should().Be("Category Name");
        category.Description.Should().Be("Description");
    }

    [Fact]
    public void Constructor_WithNullDescription_DefaultsToEmpty()
    {
        // Act
        var category = new SkillCategoryEntity("Name", null!);

        // Assert
        category.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new SkillCategoryEntity(name!, "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => new SkillCategoryEntity(longName, "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void CreateWithId_CreatesWithSpecificId()
    {
        // Arrange
        var specificId = Guid.NewGuid();

        // Act
        var category = SkillCategoryEntity.CreateWithId(specificId, "Name", "Description", 5);

        // Assert
        category.Id.Should().Be(specificId);
        category.Name.Should().Be("Name");
        category.Description.Should().Be("Description");
        category.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var category = new SkillCategoryEntity("Original", "Original desc");
        var newName = "Updated Name";
        var newDescription = "Updated description";
        var newSortOrder = 10;

        // Act
        category.Update(newName, newDescription, newSortOrder);

        // Assert
        category.Name.Should().Be(newName);
        category.Description.Should().Be(newDescription);
        category.SortOrder.Should().Be(newSortOrder);
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var category = new SkillCategoryEntity("Original", "Description");

        // Act
        var act = () => category.Update("", "Desc", 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var category = new SkillCategoryEntity("Name", "Description");

        // Act
        category.Deactivate();

        // Assert
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var category = new SkillCategoryEntity("Name", "Description");
        category.Deactivate();

        // Act
        category.Activate();

        // Assert
        category.IsActive.Should().BeTrue();
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_StillUpdatesTimestamp()
    {
        // Arrange
        var category = new SkillCategoryEntity("Name", "Description");
        category.IsActive.Should().BeTrue();

        // Act
        category.Activate();

        // Assert
        category.IsActive.Should().BeTrue();
        category.UpdatedAt.Should().NotBeNull();
    }
}
