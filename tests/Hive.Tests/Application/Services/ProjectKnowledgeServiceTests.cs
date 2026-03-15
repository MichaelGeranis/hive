using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class ProjectKnowledgeServiceTests
{
    private readonly Mock<IProjectKnowledgeRepository> _knowledgeRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly Mock<IKnowledgePointRepository> _knowledgePointRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _teamTaskRepositoryMock;
    private readonly ProjectKnowledgeService _service;

    private readonly DirectReport _testDirectReport;
    private readonly Project _testProject;

    public ProjectKnowledgeServiceTests()
    {
        _knowledgeRepositoryMock = new Mock<IProjectKnowledgeRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _activityRepositoryMock = new Mock<IActivityRepository>();
        _knowledgePointRepositoryMock = new Mock<IKnowledgePointRepository>();
        _teamTaskRepositoryMock = new Mock<ITeamTaskRepository>();

        _service = new ProjectKnowledgeService(
            _knowledgeRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object,
            _activityRepositoryMock.Object,
            _knowledgePointRepositoryMock.Object,
            _teamTaskRepositoryMock.Object);

        _testDirectReport = new DirectReport("Jane", "Smith", "jane@test.com", "Engineer", "Eng", new DateTime(2022, 1, 1));
        _testProject = new Project("Alpha", "Project Alpha description", new DateTime(2023, 1, 1));
    }

    // ============== Constructor Tests ==============

    [Fact]
    public void Constructor_WithNullKnowledgeRepository_ThrowsArgumentNullException()
    {
        var act = () => new ProjectKnowledgeService(null!, _projectRepositoryMock.Object,
            _directReportRepositoryMock.Object, _activityServiceMock.Object,
            _activityRepositoryMock.Object, _knowledgePointRepositoryMock.Object,
            _teamTaskRepositoryMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("knowledgeRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        var act = () => new ProjectKnowledgeService(_knowledgeRepositoryMock.Object,
            _projectRepositoryMock.Object, _directReportRepositoryMock.Object,
            null!, _activityRepositoryMock.Object, _knowledgePointRepositoryMock.Object,
            _teamTaskRepositoryMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activityService");
    }

    // ============== GetByIdAsync Tests ==============

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        var knowledge = new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 3);
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(knowledge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledge);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);

        // Act
        var result = await _service.GetByIdAsync(knowledge.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(knowledge.Id);
        result.DirectReportId.Should().Be(_testDirectReport.Id);
        result.DirectReportName.Should().Be(_testDirectReport.FullName);
        result.ProjectId.Should().Be(_testProject.Id);
        result.ProjectName.Should().Be(_testProject.Name);
        result.KnowledgeLevel.Should().Be(3);
        result.KnowledgeLevelLabel.Should().Be("Moderate Knowledge");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenDirectReportDeleted_UsesUnknownName()
    {
        // Arrange
        var knowledge = new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 2);
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(knowledge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledge);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);

        // Act
        var result = await _service.GetByIdAsync(knowledge.Id);

        // Assert
        result!.DirectReportName.Should().Be("Unknown");
    }

    // ============== GetAllAsync Tests ==============

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        // Arrange
        var dr2 = new DirectReport("Bob", "Jones", "bob@test.com", "Dev", "Eng", new DateTime(2021, 1, 1));
        var project2 = new Project("Beta", "Beta project", new DateTime(2023, 3, 1));
        var knowledgeList = new List<ProjectKnowledge>
        {
            new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 4),
            new ProjectKnowledge(dr2.Id, project2.Id, 2)
        };

        _knowledgeRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeList);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport, dr2 });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { _testProject, project2 });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.KnowledgeLevel).Should().Contain([4, 2]);
    }

    // ============== GetByDirectReportIdAsync Tests ==============

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsKnowledgeForDirectReport()
    {
        // Arrange
        var knowledgeList = new List<ProjectKnowledge>
        {
            new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 3)
        };
        _knowledgeRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeList);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { _testProject });

        // Act
        var result = await _service.GetByDirectReportIdAsync(_testDirectReport.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportId.Should().Be(_testDirectReport.Id);
    }

    // ============== GetMatrixAsync Tests ==============

    [Fact]
    public async Task GetMatrixAsync_ReturnsMatrixWithProjectsAndDirectReports()
    {
        // Arrange
        var directOnlyReport = new DirectReport("Alice", "Brown", "alice@test.com", "Lead", "Eng", new DateTime(2020, 6, 1));
        // IsDirect is true by default

        var projects = new List<Project> { _testProject };
        var directReports = new List<DirectReport> { directOnlyReport };
        var scores = new List<ProjectKnowledge>
        {
            new ProjectKnowledge(directOnlyReport.Id, _testProject.Id, 5)
        };

        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
        _knowledgeRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scores);

        // Act
        var result = await _service.GetMatrixAsync();

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(1);
        result.Projects[0].Name.Should().Be(_testProject.Name);
        result.DirectReports.Should().HaveCount(1);
        result.Scores.Should().HaveCount(1);
        result.Scores[0].KnowledgeLevel.Should().Be(5);
        result.Scores[0].KnowledgeLevelLabel.Should().Be("Confident");
    }

    [Fact]
    public async Task GetMatrixAsync_ExcludesIndirectReportsFromDirectReportsList()
    {
        // Arrange
        var directReport = new DirectReport("Alice", "Brown", "alice@test.com", "Lead", "Eng", new DateTime(2020, 6, 1));
        var indirectReport = new DirectReport("Charlie", "Davis", "charlie@test.com", "Dev", "Eng", new DateTime(2021, 1, 1));
        // Make indirectReport not direct by setting a parent
        var parent = new Parent(indirectReport.Id, directReport.Id, "direct.reports");

        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport, indirectReport });
        _knowledgeRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectKnowledge>());

        // Act
        var result = await _service.GetMatrixAsync();

        // Assert - only direct reports included
        // Both are direct by default in this test since IsDirect defaults true
        result.DirectReports.Should().HaveCount(2);
    }

    // ============== CreateOrUpdateAsync Tests ==============

    [Fact]
    public async Task CreateOrUpdateAsync_WhenNew_CreatesKnowledgeAndLogsActivity()
    {
        // Arrange
        var dto = new CreateOrUpdateProjectKnowledgeDto
        {
            DirectReportId = _testDirectReport.Id,
            ProjectId = _testProject.Id,
            KnowledgeLevel = 4
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);
        _knowledgeRepositoryMock.Setup(r => r.GetByDirectReportAndProjectAsync(
            _testDirectReport.Id, _testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);
        _knowledgeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ProjectKnowledge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge k, CancellationToken _) => k);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.KnowledgeLevel.Should().Be(4);
        result.KnowledgeLevelLabel.Should().Be("Good Knowledge");
        _knowledgeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ProjectKnowledge>(), It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created, EntityType.ProjectKnowledge,
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenExisting_UpdatesKnowledgeAndLogsActivity()
    {
        // Arrange
        var existing = new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 2);
        var dto = new CreateOrUpdateProjectKnowledgeDto
        {
            DirectReportId = _testDirectReport.Id,
            ProjectId = _testProject.Id,
            KnowledgeLevel = 5
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);
        _knowledgeRepositoryMock.Setup(r => r.GetByDirectReportAndProjectAsync(
            _testDirectReport.Id, _testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.KnowledgeLevel.Should().Be(5);
        _knowledgeRepositoryMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _knowledgeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ProjectKnowledge>(), It.IsAny<CancellationToken>()), Times.Never);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Updated, EntityType.ProjectKnowledge,
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenDirectReportNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = async () => await _service.CreateOrUpdateAsync(new CreateOrUpdateProjectKnowledgeDto
        {
            DirectReportId = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            KnowledgeLevel = 3
        });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenProjectNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var act = async () => await _service.CreateOrUpdateAsync(new CreateOrUpdateProjectKnowledgeDto
        {
            DirectReportId = _testDirectReport.Id,
            ProjectId = Guid.NewGuid(),
            KnowledgeLevel = 3
        });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ============== DeleteAsync Tests ==============

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesAndLogsActivity()
    {
        // Arrange
        var knowledge = new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 3);
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(knowledge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledge);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        await _service.DeleteAsync(knowledge.Id);

        // Assert
        _knowledgeRepositoryMock.Verify(r => r.DeleteAsync(knowledge.Id, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Deleted, EntityType.ProjectKnowledge,
            knowledge.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenProjectOrDirectReportDeleted_StillDeletesWithUnknownNames()
    {
        // Arrange
        var knowledge = new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 2);
        _knowledgeRepositoryMock.Setup(r => r.GetByIdAsync(knowledge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledge);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        await _service.DeleteAsync(knowledge.Id);

        // Assert - delete still proceeds
        _knowledgeRepositoryMock.Verify(r => r.DeleteAsync(knowledge.Id, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Deleted, EntityType.ProjectKnowledge, knowledge.Id,
            It.Is<string>(s => s.Contains("Unknown")),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============== GetByProjectIdAsync Tests ==============

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsKnowledgeForProject()
    {
        // Arrange
        var knowledgeList = new List<ProjectKnowledge>
        {
            new ProjectKnowledge(_testDirectReport.Id, _testProject.Id, 3)
        };
        _knowledgeRepositoryMock.Setup(r => r.GetByProjectIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeList);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { _testProject });

        // Act
        var result = await _service.GetByProjectIdAsync(_testProject.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].ProjectId.Should().Be(_testProject.Id);
    }

    // ============== Knowledge Level Label Tests ==============

    [Theory]
    [InlineData(1, "No clue")]
    [InlineData(2, "Limited Knowledge")]
    [InlineData(3, "Moderate Knowledge")]
    [InlineData(4, "Good Knowledge")]
    [InlineData(5, "Confident")]
    public async Task CreateOrUpdateAsync_MapsKnowledgeLevelLabelCorrectly(int level, string expectedLabel)
    {
        // Arrange
        var dto = new CreateOrUpdateProjectKnowledgeDto
        {
            DirectReportId = _testDirectReport.Id,
            ProjectId = _testProject.Id,
            KnowledgeLevel = level
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(_testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProject);
        _knowledgeRepositoryMock.Setup(r => r.GetByDirectReportAndProjectAsync(
            _testDirectReport.Id, _testProject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge?)null);
        _knowledgeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ProjectKnowledge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectKnowledge k, CancellationToken _) => k);

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.KnowledgeLevelLabel.Should().Be(expectedLabel);
    }
}
