using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ProjectRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new ProjectRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProjectRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var project = CreateAndAddProject();

        // Act
        var result = await _repository.GetByIdAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
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
    public async Task GetAllAsync_ReturnsAllProjects()
    {
        // Arrange
        CreateAndAddProject("Project 1");
        CreateAndAddProject("Project 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        // Arrange
        CreateAndAddProject("Zebra Project");
        CreateAndAddProject("Alpha Project");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Name.Should().Be("Alpha Project");
        result[1].Name.Should().Be("Zebra Project");
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsMatchingProjects()
    {
        // Arrange
        var planningProject = CreateAndAddProject("Planning");
        var activeProject = CreateAndAddProject("Active");
        activeProject.Activate();
        _context.Projects[activeProject.Id] = activeProject;

        // Act
        var result = await _repository.GetByStatusAsync(ProjectStatus.Active);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsActiveAndPlanningProjects()
    {
        // Arrange
        var planningProject = CreateAndAddProject("Planning");
        var activeProject = CreateAndAddProject("Active");
        activeProject.Activate();
        _context.Projects[activeProject.Id] = activeProject;
        var completedProject = CreateAndAddProject("Completed");
        completedProject.Complete();
        _context.Projects[completedProject.Id] = completedProject;

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Status == ProjectStatus.Planning);
        result.Should().Contain(p => p.Status == ProjectStatus.Active);
    }

    [Fact]
    public async Task AddAsync_AddsProjectToContext()
    {
        // Arrange
        var project = new Project("New Project");

        // Act
        var result = await _repository.AddAsync(project);

        // Assert
        result.Should().Be(project);
        _context.Projects.Should().ContainKey(project.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = CreateAndAddProject();

        // Act
        var act = () => _repository.AddAsync(project);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProjectInContext()
    {
        // Arrange
        var project = CreateAndAddProject();
        project.Update("Updated Name", "New description", null, null);

        // Act
        await _repository.UpdateAsync(project);

        // Assert
        var stored = _context.Projects[project.Id];
        stored.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Test");

        // Act
        var act = () => _repository.UpdateAsync(project);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesProjectFromContext()
    {
        // Arrange
        var project = CreateAndAddProject();

        // Act
        await _repository.DeleteAsync(project.Id);

        // Assert
        _context.Projects.Should().NotContainKey(project.Id);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var project = CreateAndAddProject();

        // Act
        var result = await _repository.ExistsAsync(project.Id);

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
        CreateAndAddProject("Existing Project");

        // Act
        var result = await _repository.NameExistsAsync("EXISTING PROJECT");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NameExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.NameExistsAsync("Nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NameExistsAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var project = CreateAndAddProject("Test Project");

        // Act
        var result = await _repository.NameExistsAsync("Test Project", project.Id);

        // Assert
        result.Should().BeFalse();
    }

    private Project CreateAndAddProject(string name = "Test Project")
    {
        var project = new Project(name, "Description");
        _context.Projects.TryAdd(project.Id, project);
        return project;
    }
}
