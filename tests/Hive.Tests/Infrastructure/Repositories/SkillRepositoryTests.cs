using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SkillRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SkillRepository _repository;

    public SkillRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SkillRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var skill = CreateAndAddSkill("C#");

        // Act
        var result = await _repository.GetByIdAsync(skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(skill.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        CreateAndAddSkill("Python");

        // Act
        var result = await _repository.GetByNameAsync("Python");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Python");
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddSkill("JavaScript");

        // Act
        var result = await _repository.GetByNameAsync("javascript");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("JavaScript");
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveSkills()
    {
        // Arrange
        CreateAndAddSkill("Skill 1");
        CreateAndAddSkill("Skill 2");
        var inactive = CreateAndAddSkill("Inactive");
        inactive.Deactivate();
        _context.Skills[inactive.Id] = inactive;

        // Act
        var result = await _repository.GetAllAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsAll()
    {
        // Arrange
        CreateAndAddSkill("Skill 1");
        var inactive = CreateAndAddSkill("Inactive");
        inactive.Deactivate();
        _context.Skills[inactive.Id] = inactive;

        // Act
        var result = await _repository.GetAllAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCategoryThenName()
    {
        // Arrange
        CreateAndAddSkill("Zebra", category: SkillCategory.Technical);
        CreateAndAddSkill("Alpha", category: SkillCategory.Technical);
        CreateAndAddSkill("Beta", category: SkillCategory.Leadership);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert - Skills ordered by Category (ascending) then Name (ascending)
        // Technical (0) comes before Leadership (2)
        result[0].Name.Should().Be("Alpha"); // Technical (category 0), alphabetically first
        result[1].Name.Should().Be("Zebra"); // Technical (category 0), alphabetically second
        result[2].Name.Should().Be("Beta"); // Leadership (category 2)
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsMatchingActiveSkills()
    {
        // Arrange
        CreateAndAddSkill("C#", category: SkillCategory.Technical);
        CreateAndAddSkill("Python", category: SkillCategory.Technical);
        CreateAndAddSkill("Communication", category: SkillCategory.SoftSkills);

        var inactiveTechnical = CreateAndAddSkill("Inactive Tech", category: SkillCategory.Technical);
        inactiveTechnical.Deactivate();
        _context.Skills[inactiveTechnical.Id] = inactiveTechnical;

        // Act
        var result = await _repository.GetByCategoryAsync(SkillCategory.Technical);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s =>
        {
            s.Category.Should().Be(SkillCategory.Technical);
            s.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetByCategoryAsync_OrdersByName()
    {
        // Arrange
        CreateAndAddSkill("Zebra", category: SkillCategory.Technical);
        CreateAndAddSkill("Alpha", category: SkillCategory.Technical);

        // Act
        var result = await _repository.GetByCategoryAsync(SkillCategory.Technical);

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task AddAsync_AddsSkillToContext()
    {
        // Arrange
        var skill = new Skill("TypeScript", "Programming language", SkillCategory.Technical);

        // Act
        var result = await _repository.AddAsync(skill);

        // Assert
        result.Should().Be(skill);
        _context.Skills.Should().ContainKey(skill.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var skill = CreateAndAddSkill("Test");

        // Act
        var act = () => _repository.AddAsync(skill);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSkillInContext()
    {
        // Arrange
        var skill = CreateAndAddSkill("Original");
        skill.Update("Updated", "New description", SkillCategory.Leadership);

        // Act
        await _repository.UpdateAsync(skill);

        // Assert
        var stored = _context.Skills[skill.Id];
        stored.Name.Should().Be("Updated");
        stored.Description.Should().Be("New description");
        stored.Category.Should().Be(SkillCategory.Leadership);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentSkill_ThrowsInvalidOperationException()
    {
        // Arrange
        var skill = new Skill("Test", "Test description", SkillCategory.Technical);

        // Act
        var act = () => _repository.UpdateAsync(skill);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesSkillFromContext()
    {
        // Arrange
        var skill = CreateAndAddSkill("Test");

        // Act
        await _repository.DeleteAsync(skill.Id);

        // Assert
        _context.Skills.Should().NotContainKey(skill.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var skill = CreateAndAddSkill("Test");

        // Act
        var result = await _repository.ExistsAsync(skill.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        CreateAndAddSkill("Python");

        // Act
        var result = await _repository.NameExistsAsync("Python");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddSkill("Python");

        // Act
        var result = await _repository.NameExistsAsync("python");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.NameExistsAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var skill = CreateAndAddSkill("Python");

        // Act
        var result = await _repository.NameExistsAsync("Python", excludeId: skill.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_FindsDuplicateWhenExcludingDifferentId()
    {
        // Arrange
        CreateAndAddSkill("Python");
        var otherId = Guid.NewGuid();

        // Act
        var result = await _repository.NameExistsAsync("Python", excludeId: otherId);

        // Assert
        result.Should().BeTrue();
    }

    private Skill CreateAndAddSkill(
        string name = "Test Skill",
        SkillCategory category = SkillCategory.Technical)
    {
        var skill = new Skill(name, "Test description", category);
        _context.Skills.TryAdd(skill.Id, skill);
        return skill;
    }
}
