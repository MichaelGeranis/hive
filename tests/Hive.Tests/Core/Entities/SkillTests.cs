using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SkillTests
{
    private readonly Guid _testCategoryId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesSkill()
    {
        // Arrange
        var name = "C#";
        var description = "C# programming language";
        var categoryId = _testCategoryId;

        // Act
        var skill = new Skill(name, description, categoryId);

        // Assert
        skill.Id.Should().NotBeEmpty();
        skill.Name.Should().Be(name);
        skill.Description.Should().Be(description);
        skill.SkillCategoryId.Should().Be(categoryId);
        skill.IsActive.Should().BeTrue();
        skill.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        skill.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var skill = new Skill("  C#  ", "  Description  ", _testCategoryId);

        // Assert
        skill.Name.Should().Be("C#");
        skill.Description.Should().Be("Description");
    }

    [Fact]
    public void Constructor_WithNullDescription_DefaultsToEmpty()
    {
        // Act
        var skill = new Skill("C#", null!, _testCategoryId);

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
        var act = () => new Skill(name!, "Description", _testCategoryId);

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
        var act = () => new Skill(longName, "Description", _testCategoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var skill = new Skill("C#", "Description", _testCategoryId);
        var newCategoryId = Guid.NewGuid();

        // Act
        skill.Update("TypeScript", "Frontend language", newCategoryId);

        // Assert
        skill.Name.Should().Be("TypeScript");
        skill.Description.Should().Be("Frontend language");
        skill.SkillCategoryId.Should().Be(newCategoryId);
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var skill = new Skill("C#", "Description", _testCategoryId);

        // Act
        var act = () => skill.Update("", "Description", _testCategoryId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var skill = new Skill("C#", "Description", _testCategoryId);

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
        var skill = new Skill("C#", "Description", _testCategoryId);
        skill.Deactivate();

        // Act
        skill.Activate();

        // Assert
        skill.IsActive.Should().BeTrue();
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsDifferentCategoryIds()
    {
        // Arrange
        var categoryIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act & Assert
        foreach (var categoryId in categoryIds)
        {
            var skill = new Skill("Test Skill", "Description", categoryId);
            skill.SkillCategoryId.Should().Be(categoryId);
        }
    }

    [Fact]
    public void SetCategoryId_UpdatesCategoryId()
    {
        // Arrange
        var skill = new Skill("C#", "Description", _testCategoryId);
        var newCategoryId = Guid.NewGuid();

        // Act
        skill.SetCategoryId(newCategoryId);

        // Assert
        skill.SkillCategoryId.Should().Be(newCategoryId);
    }
}
