using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SkillTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesSkill()
    {
        // Arrange
        var name = "C#";
        var description = "C# programming language";
        var category = SkillCategory.Technical;

        // Act
        var skill = new Skill(name, description, category);

        // Assert
        skill.Id.Should().NotBeEmpty();
        skill.Name.Should().Be(name);
        skill.Description.Should().Be(description);
        skill.Category.Should().Be(category);
        skill.IsActive.Should().BeTrue();
        skill.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        skill.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var skill = new Skill("  C#  ", "  Description  ", SkillCategory.Technical);

        // Assert
        skill.Name.Should().Be("C#");
        skill.Description.Should().Be("Description");
    }

    [Fact]
    public void Constructor_WithNullDescription_DefaultsToEmpty()
    {
        // Act
        var skill = new Skill("C#", null!, SkillCategory.Technical);

        // Assert
        skill.Description.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Skill(name!, "Description", SkillCategory.Technical);

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
        var act = () => new Skill(longName, "Description", SkillCategory.Technical);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var skill = new Skill("C#", "Description", SkillCategory.Technical);

        // Act
        skill.Update("TypeScript", "Frontend language", SkillCategory.Tools);

        // Assert
        skill.Name.Should().Be("TypeScript");
        skill.Description.Should().Be("Frontend language");
        skill.Category.Should().Be(SkillCategory.Tools);
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var skill = new Skill("C#", "Description", SkillCategory.Technical);

        // Act
        var act = () => skill.Update("", "Description", SkillCategory.Technical);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var skill = new Skill("C#", "Description", SkillCategory.Technical);

        // Act
        skill.Deactivate();

        // Assert
        skill.IsActive.Should().BeFalse();
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var skill = new Skill("C#", "Description", SkillCategory.Technical);
        skill.Deactivate();

        // Act
        skill.Activate();

        // Assert
        skill.IsActive.Should().BeTrue();
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(SkillCategory.Technical)]
    [InlineData(SkillCategory.SoftSkills)]
    [InlineData(SkillCategory.Leadership)]
    [InlineData(SkillCategory.DomainKnowledge)]
    [InlineData(SkillCategory.Tools)]
    public void Constructor_AcceptsAllCategories(SkillCategory category)
    {
        // Act
        var skill = new Skill("Test Skill", "Description", category);

        // Assert
        skill.Category.Should().Be(category);
    }
}
