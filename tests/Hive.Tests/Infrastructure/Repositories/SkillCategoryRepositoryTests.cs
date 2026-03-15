using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SkillCategoryRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SkillCategoryRepository _repository;

    public SkillCategoryRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SkillCategoryRepository(_context);
    }

    private SkillCategoryEntity AddCategory(string name = "Engineering", bool active = true, int sortOrder = 0)
    {
        var category = new SkillCategoryEntity(name, "Description", sortOrder);
        if (!active) category.Deactivate();
        _context.SkillCategories.TryAdd(category.Id, category);
        return category;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new SkillCategoryRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsCategory()
    {
        var category = AddCategory();
        var result = await _repository.GetByIdAsync(category.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenExists_ReturnsCategory()
    {
        AddCategory("Backend");
        var result = await _repository.GetByNameAsync("Backend");
        result.Should().NotBeNull();
        result!.Name.Should().Be("Backend");
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseInsensitive()
    {
        AddCategory("Frontend");
        var result = await _repository.GetByNameAsync("frontend");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NonExistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenIncludeInactiveFalse_ReturnsOnlyActive()
    {
        AddCategory("Active Cat", active: true);
        AddCategory("Inactive Cat", active: false);

        var result = await _repository.GetAllAsync(includeInactive: false);
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Cat");
    }

    [Fact]
    public async Task GetAllAsync_WhenIncludeInactiveTrue_ReturnsAll()
    {
        AddCategory("Active Cat", active: true);
        AddCategory("Inactive Cat", active: false);

        var result = await _repository.GetAllAsync(includeInactive: true);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrderThenName()
    {
        AddCategory("Zebra", sortOrder: 1);
        AddCategory("Alpha", sortOrder: 0);
        AddCategory("Beta", sortOrder: 0);

        var result = await _repository.GetAllAsync(includeInactive: true);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Beta");
        result[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task AddAsync_AddsCategory()
    {
        var category = new SkillCategoryEntity("DevOps", "DevOps skills");
        await _repository.AddAsync(category);

        var result = await _repository.GetByIdAsync(category.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("DevOps");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicate_ThrowsInvalidOperationException()
    {
        var category = AddCategory();
        var act = async () => await _repository.AddAsync(category);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCategory()
    {
        var category = AddCategory("Old Name");
        category.Update("New Name", "New Desc", 0);

        await _repository.UpdateAsync(category);

        var result = await _repository.GetByIdAsync(category.Id);
        result!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        var category = new SkillCategoryEntity("Unknown", "Desc");
        var act = async () => await _repository.UpdateAsync(category);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = AddCategory();
        await _repository.DeleteAsync(category.Id);
        _context.SkillCategories.Should().NotContainKey(category.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var category = AddCategory();
        var result = await _repository.ExistsAsync(category.Id);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_WhenExists_ReturnsTrue()
    {
        AddCategory("Duplicate");
        var result = await _repository.NameExistsAsync("Duplicate");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_IsCaseInsensitive()
    {
        AddCategory("Testing");
        var result = await _repository.NameExistsAsync("TESTING");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_WhenExcludeIdMatches_ReturnsFalse()
    {
        var category = AddCategory("Same Name");
        var result = await _repository.NameExistsAsync("Same Name", category.Id);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasSkillsAsync_WhenCategoryHasSkills_ReturnsTrue()
    {
        var category = AddCategory("Has Skills");
        var skill = new Skill("C#", "C# language", category.Id);
        _context.Skills.TryAdd(skill.Id, skill);

        var result = await _repository.HasSkillsAsync(category.Id);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSkillsAsync_WhenNoSkills_ReturnsFalse()
    {
        var category = AddCategory("Empty Category");
        var result = await _repository.HasSkillsAsync(category.Id);
        result.Should().BeFalse();
    }
}
