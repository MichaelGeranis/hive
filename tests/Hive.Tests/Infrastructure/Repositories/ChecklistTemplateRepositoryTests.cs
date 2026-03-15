using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ChecklistTemplateRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ChecklistTemplateRepository _repository;

    public ChecklistTemplateRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ChecklistTemplateRepository(_context);
    }

    private ChecklistTemplate AddTemplate(string name = "Template", ChecklistType type = ChecklistType.Interview, bool active = true)
    {
        var template = new ChecklistTemplate(name, "Description", type);
        if (!active) template.Deactivate();
        _context.ChecklistTemplates.TryAdd(template.Id, template);
        return template;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new ChecklistTemplateRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsTemplate()
    {
        var template = AddTemplate();
        var result = await _repository.GetByIdAsync(template.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTemplatesOrderedByTypeThenName()
    {
        AddTemplate("Zebra", ChecklistType.Onboarding);
        AddTemplate("Alpha", ChecklistType.Interview);
        AddTemplate("Beta", ChecklistType.Interview);

        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha"); // Interview = 0, alphabetical
        result[1].Name.Should().Be("Beta");
    }

    [Fact]
    public async Task GetByTypeAsync_FiltersByType()
    {
        AddTemplate("Interview T", ChecklistType.Interview);
        AddTemplate("Onboarding T", ChecklistType.Onboarding);

        var result = await _repository.GetByTypeAsync(ChecklistType.Interview);
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ChecklistType.Interview);
    }

    [Fact]
    public async Task GetByTypeAsync_WhenIncludeInactiveFalse_ExcludesInactiveTemplates()
    {
        AddTemplate("Active", ChecklistType.Interview, active: true);
        AddTemplate("Inactive", ChecklistType.Interview, active: false);

        var result = await _repository.GetByTypeAsync(ChecklistType.Interview, includeInactive: false);
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetByTypeAsync_WhenIncludeInactiveTrue_IncludesInactiveTemplates()
    {
        AddTemplate("Active", ChecklistType.Interview, active: true);
        AddTemplate("Inactive", ChecklistType.Interview, active: false);

        var result = await _repository.GetByTypeAsync(ChecklistType.Interview, includeInactive: true);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveTemplates()
    {
        AddTemplate("Active Interview", ChecklistType.Interview, active: true);
        AddTemplate("Inactive Interview", ChecklistType.Interview, active: false);
        AddTemplate("Active Onboarding", ChecklistType.Onboarding, active: true);

        var result = await _repository.GetActiveAsync();
        result.Should().HaveCount(2);
        result.All(t => t.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAsync_WithTypeFilter_ReturnsActiveTemplatesOfType()
    {
        AddTemplate("Active Interview", ChecklistType.Interview, active: true);
        AddTemplate("Active Onboarding", ChecklistType.Onboarding, active: true);

        var result = await _repository.GetActiveAsync(ChecklistType.Interview);
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ChecklistType.Interview);
    }

    [Fact]
    public async Task AddAsync_AddsTemplate()
    {
        var template = new ChecklistTemplate("New Template", "Desc", ChecklistType.Interview);
        await _repository.AddAsync(template);

        var result = await _repository.GetByIdAsync(template.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Template");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicate_ThrowsInvalidOperationException()
    {
        var template = AddTemplate();
        var act = async () => await _repository.AddAsync(template);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTemplate()
    {
        var template = AddTemplate("Original");
        template.Update("Updated", "New Desc");

        await _repository.UpdateAsync(template);

        var result = await _repository.GetByIdAsync(template.Id);
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        var template = new ChecklistTemplate("Template", "Desc", ChecklistType.Interview);
        var act = async () => await _repository.UpdateAsync(template);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTemplate()
    {
        var template = AddTemplate();
        await _repository.DeleteAsync(template.Id);

        _context.ChecklistTemplates.Should().NotContainKey(template.Id);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_DoesNotThrow()
    {
        var act = async () => await _repository.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var template = AddTemplate();
        var result = await _repository.ExistsAsync(template.Id);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_WhenNameExistsForType_ReturnsTrue()
    {
        AddTemplate("Duplicate Name", ChecklistType.Interview);
        var result = await _repository.NameExistsAsync("Duplicate Name", ChecklistType.Interview);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_IsCaseInsensitive()
    {
        AddTemplate("Case Test", ChecklistType.Interview);
        var result = await _repository.NameExistsAsync("case test", ChecklistType.Interview);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_WhenDifferentType_ReturnsFalse()
    {
        AddTemplate("Template", ChecklistType.Interview);
        var result = await _repository.NameExistsAsync("Template", ChecklistType.Onboarding);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_WhenExcludeIdMatches_ReturnsFalse()
    {
        var template = AddTemplate("Template", ChecklistType.Interview);
        var result = await _repository.NameExistsAsync("Template", ChecklistType.Interview, template.Id);
        result.Should().BeFalse();
    }
}
