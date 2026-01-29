using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

public class KnowledgePointServiceTests
{
    private readonly Mock<IKnowledgePointRepository> _knowledgePointRepoMock;
    private readonly Mock<IProjectKnowledgeRepository> _projectKnowledgeRepoMock;
    private readonly Mock<ITeamTaskRepository> _teamTaskRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IDirectReportRepository> _directReportRepoMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly KnowledgePointService _service;

    public KnowledgePointServiceTests()
    {
        _knowledgePointRepoMock = new Mock<IKnowledgePointRepository>();
        _projectKnowledgeRepoMock = new Mock<IProjectKnowledgeRepository>();
        _teamTaskRepoMock = new Mock<ITeamTaskRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _directReportRepoMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();

        _service = new KnowledgePointService(
            _knowledgePointRepoMock.Object,
            _projectKnowledgeRepoMock.Object,
            _teamTaskRepoMock.Object,
            _projectRepoMock.Object,
            _directReportRepoMock.Object,
            _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullKnowledgePointRepository_ThrowsArgumentNullException()
    {
        var act = () => new KnowledgePointService(
            null!,
            _projectKnowledgeRepoMock.Object,
            _teamTaskRepoMock.Object,
            _projectRepoMock.Object,
            _directReportRepoMock.Object,
            _activityServiceMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("knowledgePointRepository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var entity = new KnowledgePoint(directReportId, projectId, 10, "Test notes");

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _knowledgePointRepoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectKnowledgeRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.ManualPoints.Should().Be(10);
        result.Notes.Should().Be("Test notes");
        result.DirectReportName.Should().Be("John Doe");
        result.ProjectName.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _knowledgePointRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CalculateAutomaticPointsAsync_WithCompletedTasks_SumsStoryPoints()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project("Test Project", "Description");

        var task1 = new TeamTask("Task 1", storyPoints: 3, projectId: projectId, assigneeId: directReportId);
        task1.Complete();
        var task2 = new TeamTask("Task 2", storyPoints: 5, projectId: projectId, assigneeId: directReportId);
        task2.Complete();
        var task3 = new TeamTask("Task 3 - Not Done", storyPoints: 8, projectId: projectId, assigneeId: directReportId);

        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, task3 });
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.CalculateAutomaticPointsAsync(directReportId, projectId);

        // Assert
        result.Should().Be(8); // 3 + 5 = 8 (only completed tasks)
    }

    [Fact]
    public async Task CalculateAutomaticPointsAsync_WithNoStoryPoints_CountsAs1()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project("Test Project", "Description");

        var task1 = new TeamTask("Task 1", storyPoints: null, projectId: projectId, assigneeId: directReportId);
        task1.Complete();
        var task2 = new TeamTask("Task 2", storyPoints: null, projectId: projectId, assigneeId: directReportId);
        task2.Complete();

        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2 });
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.CalculateAutomaticPointsAsync(directReportId, projectId);

        // Assert
        result.Should().Be(2); // 1 + 1 = 2 (default of 1 per task)
    }

    [Fact]
    public async Task CalculateAutomaticPointsAsync_WithMatchingLabels_IncludesTasksWithoutDirectProjectId()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        // Project has labels
        var project = new Project("Rights Manager", "Description", labels: "rights-manager, backend");

        // Task has matching label but no direct ProjectId
        var task1 = new TeamTask("Task via labels", storyPoints: 5, projectId: null, assigneeId: directReportId, labels: "rights-manager, frontend");
        task1.Complete();
        // Task with direct ProjectId
        var task2 = new TeamTask("Task via ProjectId", storyPoints: 3, projectId: projectId, assigneeId: directReportId);
        task2.Complete();
        // Task with non-matching labels (should not be counted)
        var task3 = new TeamTask("Unrelated task", storyPoints: 10, projectId: null, assigneeId: directReportId, labels: "other-project");
        task3.Complete();

        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1, task2, task3 });
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.CalculateAutomaticPointsAsync(directReportId, projectId);

        // Assert
        result.Should().Be(8); // 5 (label match) + 3 (direct ProjectId) = 8
    }

    [Fact]
    public async Task CalculateAutomaticPointsAsync_WithCaseInsensitiveLabels_MatchesCorrectly()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = new Project("Test Project", "Description", labels: "Backend, API");

        // Task has label with different casing
        var task1 = new TeamTask("Task 1", storyPoints: 7, projectId: null, assigneeId: directReportId, labels: "backend, frontend");
        task1.Complete();

        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task1 });
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.CalculateAutomaticPointsAsync(directReportId, projectId);

        // Assert
        result.Should().Be(7); // Labels match case-insensitively
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenNew_CreatesKnowledgePoint()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var dto = new CreateOrUpdateKnowledgePointDto
        {
            DirectReportId = directReportId,
            ProjectId = projectId,
            ManualPoints = 15,
            Notes = "New points"
        };

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _knowledgePointRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint?)null);
        _knowledgePointRepoMock.Setup(r => r.AddAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint kp, CancellationToken _) => kp);
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectKnowledgeRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ManualPoints.Should().Be(15);
        result.Notes.Should().Be("New points");
        _knowledgePointRepoMock.Verify(r => r.AddAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenExists_UpdatesKnowledgePoint()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var existingEntity = new KnowledgePoint(directReportId, projectId, 5, "Old notes");

        var dto = new CreateOrUpdateKnowledgePointDto
        {
            DirectReportId = directReportId,
            ProjectId = projectId,
            ManualPoints = 20,
            Notes = "Updated notes"
        };

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _knowledgePointRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectKnowledgeRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ManualPoints.Should().Be(20);
        result.Notes.Should().Be("Updated notes");
        _knowledgePointRepoMock.Verify(r => r.UpdateAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithInvalidDirectReport_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateOrUpdateKnowledgePointDto
        {
            DirectReportId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ManualPoints = 10
        };

        _directReportRepoMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = async () => await _service.CreateOrUpdateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*DirectReport*");
    }

    [Fact]
    public async Task AddPointsAsync_AddsToExistingPoints()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var existingEntity = new KnowledgePoint(directReportId, projectId, 10, "Existing");

        var dto = new AddKnowledgePointsDto
        {
            DirectReportId = directReportId,
            ProjectId = projectId,
            PointsToAdd = 5,
            Notes = "Additional contribution"
        };

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _knowledgePointRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectKnowledgeRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.AddPointsAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ManualPoints.Should().Be(15); // 10 + 5
        _knowledgePointRepoMock.Verify(r => r.UpdateAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPointsAsync_CreatesNewIfNotExists()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var dto = new AddKnowledgePointsDto
        {
            DirectReportId = directReportId,
            ProjectId = projectId,
            PointsToAdd = 5,
            Notes = "First contribution"
        };

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _knowledgePointRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint?)null);
        _knowledgePointRepoMock.Setup(r => r.AddAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint kp, CancellationToken _) => kp);
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _projectKnowledgeRepoMock.Setup(r => r.GetByDirectReportAndProjectAsync(directReportId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.AddPointsAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ManualPoints.Should().Be(5);
        _knowledgePointRepoMock.Verify(r => r.AddAsync(It.IsAny<KnowledgePoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesKnowledgePoint()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var entity = new KnowledgePoint(directReportId, projectId, 10);

        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var project = new Project("Test Project", "Description");

        _knowledgePointRepoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _directReportRepoMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _knowledgePointRepoMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _knowledgePointRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgePoint?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*KnowledgePoint*");
    }

    [Fact]
    public async Task GetLevelIncreaseSuggestionsAsync_WithSufficientPoints_ReturnsSuggestions()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        // Use reflection to set private ID for the DirectReport so the mock returns it correctly
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var directReportIdField = typeof(DirectReport).GetProperty("Id")!;
        directReportIdField.SetValue(directReport, directReportId);

        var project = new Project("Test Project", "Description");
        var projectIdField = typeof(Project).GetProperty("Id")!;
        projectIdField.SetValue(project, projectId);

        var knowledgePoint = new KnowledgePoint(directReportId, projectId, 25); // >= 21 threshold

        _directReportRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });
        _projectRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _knowledgePointRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgePoint> { knowledgePoint });
        _projectKnowledgeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectKnowledge>());
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetLevelIncreaseSuggestionsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportName.Should().Be("John Doe");
        result[0].TotalPoints.Should().Be(25);
        result[0].SuggestedLevel.Should().Be(1); // From 0 to 1
    }

    [Fact]
    public async Task GetLevelIncreaseSuggestionsAsync_AtMaxLevel_DoesNotSuggest()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        // Use reflection to set private ID for the DirectReport so the mock returns it correctly
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        var directReportIdField = typeof(DirectReport).GetProperty("Id")!;
        directReportIdField.SetValue(directReport, directReportId);

        var project = new Project("Test Project", "Description");
        var projectIdField = typeof(Project).GetProperty("Id")!;
        projectIdField.SetValue(project, projectId);

        var knowledgePoint = new KnowledgePoint(directReportId, projectId, 30);
        var projectKnowledge = new ProjectKnowledge(directReportId, projectId, 5); // Already at max level

        _directReportRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });
        _projectRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _knowledgePointRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgePoint> { knowledgePoint });
        _projectKnowledgeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectKnowledge> { projectKnowledge });
        _teamTaskRepoMock.Setup(r => r.GetByAssigneeIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());

        // Act
        var result = await _service.GetLevelIncreaseSuggestionsAsync();

        // Assert
        result.Should().BeEmpty();
    }
}
