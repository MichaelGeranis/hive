using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ChecklistInstanceRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ChecklistInstanceRepository _repository;

    public ChecklistInstanceRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ChecklistInstanceRepository(_context);
    }

    private ChecklistInstance AddInterviewInstance(string title = "Interview", ChecklistInstanceStatus? status = null)
    {
        var templateId = Guid.NewGuid();
        var instance = ChecklistInstance.CreateInterview(templateId, title, "Candidate", "Dev", DateTime.UtcNow.AddDays(3));
        if (status == ChecklistInstanceStatus.InProgress) instance.Start();
        else if (status == ChecklistInstanceStatus.Completed) { instance.Start(); instance.Complete(); }
        else if (status == ChecklistInstanceStatus.Cancelled) instance.Cancel();
        _context.ChecklistInstances.TryAdd(instance.Id, instance);
        return instance;
    }

    private ChecklistInstance AddOnboardingInstance(string title = "Onboarding")
    {
        var templateId = Guid.NewGuid();
        var instance = ChecklistInstance.CreateOnboarding(templateId, title, "New Hire", DateTime.UtcNow.AddDays(7));
        _context.ChecklistInstances.TryAdd(instance.Id, instance);
        return instance;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new ChecklistInstanceRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsInstance()
    {
        var instance = AddInterviewInstance();
        var result = await _repository.GetByIdAsync(instance.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(instance.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllInstances()
    {
        AddInterviewInstance("Interview 1");
        AddOnboardingInstance("Onboarding 1");

        var result = await _repository.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTypeAsync_FiltersByType()
    {
        AddInterviewInstance("Interview A");
        AddInterviewInstance("Interview B");
        AddOnboardingInstance("Onboarding A");

        var result = await _repository.GetByTypeAsync(ChecklistType.Interview);
        result.Should().HaveCount(2);
        result.All(i => i.Type == ChecklistType.Interview).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTemplateIdAsync_FiltersCorrectly()
    {
        var templateId = Guid.NewGuid();
        var instance1 = ChecklistInstance.CreateInterview(templateId, "Interview 1", "Cand1", "Dev", DateTime.UtcNow);
        var instance2 = ChecklistInstance.CreateInterview(Guid.NewGuid(), "Interview 2", "Cand2", "Dev", DateTime.UtcNow);

        _context.ChecklistInstances.TryAdd(instance1.Id, instance1);
        _context.ChecklistInstances.TryAdd(instance2.Id, instance2);

        var result = await _repository.GetByTemplateIdAsync(templateId);
        result.Should().HaveCount(1);
        result[0].TemplateId.Should().Be(templateId);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsNotStartedAndInProgress()
    {
        AddInterviewInstance("Not Started", ChecklistInstanceStatus.NotStarted);
        AddInterviewInstance("In Progress", ChecklistInstanceStatus.InProgress);
        AddInterviewInstance("Completed", ChecklistInstanceStatus.Completed);
        AddInterviewInstance("Cancelled", ChecklistInstanceStatus.Cancelled);

        var result = await _repository.GetActiveAsync();
        result.Should().HaveCount(2);
        result.All(i => i.Status == ChecklistInstanceStatus.NotStarted || i.Status == ChecklistInstanceStatus.InProgress)
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAsync_WithTypeFilter_FiltersCorrectly()
    {
        AddInterviewInstance("Interview Active");
        AddOnboardingInstance("Onboarding Active");

        var result = await _repository.GetActiveAsync(ChecklistType.Interview);
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ChecklistType.Interview);
    }

    [Fact]
    public async Task AddAsync_AddsInstance()
    {
        var instance = ChecklistInstance.CreateInterview(Guid.NewGuid(), "New Interview", "Cand", "PM", DateTime.UtcNow);
        await _repository.AddAsync(instance);

        var result = await _repository.GetByIdAsync(instance.Id);
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Interview");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicate_ThrowsInvalidOperationException()
    {
        var instance = AddInterviewInstance();
        var act = async () => await _repository.AddAsync(instance);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesInstance()
    {
        var instance = AddInterviewInstance();
        instance.UpdateNotes("Updated notes");

        await _repository.UpdateAsync(instance);

        var result = await _repository.GetByIdAsync(instance.Id);
        result!.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        var instance = ChecklistInstance.CreateInterview(Guid.NewGuid(), "Interview", "Cand", "Dev", DateTime.UtcNow);
        var act = async () => await _repository.UpdateAsync(instance);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesInstance()
    {
        var instance = AddInterviewInstance();
        await _repository.DeleteAsync(instance.Id);

        _context.ChecklistInstances.Should().NotContainKey(instance.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var instance = AddInterviewInstance();
        var result = await _repository.ExistsAsync(instance.Id);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }
}
