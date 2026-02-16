using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for JiraImportService.
/// </summary>
public class JiraImportServiceTests
{
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ISprintService> _sprintServiceMock;
    private readonly Mock<IParentService> _parentServiceMock;
    private readonly Mock<IAppSettingsService> _appSettingsServiceMock;
    private readonly JiraImportService _service;

    private readonly DirectReport _testDirectReport;
    private readonly Project _testProject;
    private readonly AppSettingsDto _testAppSettings;

    public JiraImportServiceTests()
    {
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _sprintServiceMock = new Mock<ISprintService>();
        _parentServiceMock = new Mock<IParentService>();
        _appSettingsServiceMock = new Mock<IAppSettingsService>();

        _service = new JiraImportService(
            _taskRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _sprintServiceMock.Object,
            _parentServiceMock.Object,
            _appSettingsServiceMock.Object);

        _testDirectReport = new DirectReport(
            "John",
            "Doe",
            "john.doe@test.com",
            "Software Engineer",
            "Engineering",
            new DateTime(2020, 1, 1));

        _testProject = new Project(
            "Test Project",
            "Test Description",
            "test-label",
            "https://github.com/test/project");

        _testAppSettings = new AppSettingsDto
        {
            Id = Guid.NewGuid(),
            StoryPointMappings = new List<Hive.Application.DTOs.StoryPointMapping>
            {
                new() { Points = 1, Hours = 2, Label = "1 SP" },
                new() { Points = 3, Hours = 8, Label = "3 SP" },
                new() { Points = 5, Hours = 24, Label = "5 SP" }
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTaskRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new JiraImportService(
            null!,
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _sprintServiceMock.Object,
            _parentServiceMock.Object,
            _appSettingsServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("taskRepository");
    }

    #endregion

    #region PreviewImportAsync Tests

    [Fact]
    public async Task PreviewImportAsync_WithEmptyCsv_ReturnsEmptyPreview()
    {
        // Arrange
        var csvContent = "";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(0);
        result.ValidRows.Should().Be(0);
        result.InvalidRows.Should().Be(0);
        result.MappingWarnings.Should().Contain("CSV file is empty");
    }

    [Fact]
    public async Task PreviewImportAsync_WithHeadersOnly_ReturnsZeroRows()
    {
        // Arrange
        var csvContent = "Issue key,Summary,Status";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(0);
        result.ValidRows.Should().Be(0);
        result.InvalidRows.Should().Be(0);
        result.DetectedColumns.Should().HaveCount(3);
        result.DetectedColumns.Should().Contain("Issue key");
        result.DetectedColumns.Should().Contain("Summary");
    }

    [Fact]
    public async Task PreviewImportAsync_WithValidRow_ReturnsValidPreview()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status
PROJ-123,Test Task,Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(1);
        result.InvalidRows.Should().Be(0);
        result.SampleRows.Should().HaveCount(1);
        result.SampleRows[0].IssueKey.Should().Be("PROJ-123");
        result.SampleRows[0].Summary.Should().Be("Test Task");
        result.SampleRows[0].Status.Should().Be("Done");
        result.SampleRows[0].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewImportAsync_WithMissingSummary_ReturnsInvalidRow()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status
PROJ-123,,Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(1);
        result.ValidRows.Should().Be(0);
        result.InvalidRows.Should().Be(1);
        result.SampleRows[0].IsValid.Should().BeFalse();
        result.SampleRows[0].ValidationErrors.Should().Contain("Missing Summary");
    }

    [Fact]
    public async Task PreviewImportAsync_WithMissingBothKeyAndSummary_ReturnsInvalidRow()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status
,,Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.Should().NotBeNull();
        result.InvalidRows.Should().Be(1);
        result.SampleRows[0].ValidationErrors.Should().Contain("Missing both Issue Key and Summary");
    }

    [Fact]
    public async Task PreviewImportAsync_WithNoIssueKeyColumn_AddsWarning()
    {
        // Arrange
        var csvContent = @"Summary,Status
Test Task,Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.MappingWarnings.Should().Contain(w => w.Contains("No 'Issue key' column detected"));
    }

    [Fact]
    public async Task PreviewImportAsync_WithNoSummaryColumn_AddsWarning()
    {
        // Arrange
        var csvContent = @"Issue key,Status
PROJ-123,Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.MappingWarnings.Should().Contain(w => w.Contains("No 'Summary' column detected"));
    }

    [Fact]
    public async Task PreviewImportAsync_WithQuotedFields_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status
""PROJ-123"",""Task with, comma"",""Done""";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.SampleRows[0].IssueKey.Should().Be("PROJ-123");
        result.SampleRows[0].Summary.Should().Be("Task with, comma");
        result.SampleRows[0].Status.Should().Be("Done");
    }

    [Fact]
    public async Task PreviewImportAsync_WithEscapedQuotes_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status
PROJ-123,""Task with """"quotes"""""",Done";

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.SampleRows[0].Summary.Should().Be(@"Task with ""quotes""");
    }

    [Fact]
    public async Task PreviewImportAsync_ShowsOnlyFirst10Rows_ButCountsAll()
    {
        // Arrange
        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Issue key,Summary,Status");

        for (int i = 1; i <= 20; i++)
        {
            csvBuilder.AppendLine($"PROJ-{i},Task {i},Done");
        }
        var csvContent = csvBuilder.ToString();

        // Act
        var result = await _service.PreviewImportAsync(csvContent);

        // Assert
        result.TotalRows.Should().Be(20);
        result.ValidRows.Should().Be(20);
        result.SampleRows.Should().HaveCount(10); // Only first 10 shown as sample
    }

    #endregion

    #region ImportAsync Tests

    [Fact]
    public async Task ImportAsync_WithEmptyCsv_ReturnsErrorResult()
    {
        // Arrange
        var request = new JiraImportRequestDto
        {
            CsvContent = "",
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("CSV file is empty");
    }

    [Fact]
    public async Task ImportAsync_WithValidTask_CreatesTask()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status,Sprint
PROJ-123,Test Task,Done,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.ImportedTasks.Should().HaveCount(1);
        result.ImportedTasks[0].IssueKey.Should().Be("PROJ-123");
        result.ImportedTasks[0].Summary.Should().Be("Test Task");
        result.ImportedTasks[0].IsNew.Should().BeTrue();

        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.Title == "Test Task" &&
            t.Status == TaskStatus.Done &&
            t.Tags.Contains("jira:PROJ-123")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithInvalidSprintPattern_SkipsTask()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Status,Sprint
PROJ-123,Test Task,Done,InvalidSprintName";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SkippedCount.Should().Be(1);
        result.Warnings.Should().Contain(w => w.Contains("doesn't match required pattern"));
        result.Warnings.Should().Contain(w => w.Contains("has no valid sprints"));

        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WithExistingTask_AndUpdateExistingFalse_SkipsTask()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);

        var csvContent = @"Issue key,Summary,Status
PROJ-123,Test Task,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SkippedCount.Should().Be(1);
        result.Warnings.Should().Contain(w => w.Contains("already exists, skipping"));

        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WithExistingTask_AndUpdateExistingTrue_UpdatesTask()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Old Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);

        var csvContent = @"Issue key,Summary,Description,Status
PROJ-123,Test Task,New Description,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _taskRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.ImportedTasks[0].IsUpdated.Should().BeTrue();
        result.ImportedTasks[0].IsNew.Should().BeFalse();

        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.Is<TeamTask>(t =>
            t.Description == "New Description" &&
            t.Status == TaskStatus.Done
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithAssigneeName_AssignsToDirectReport()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var idProperty = typeof(DirectReport).GetProperty("Id");
        idProperty!.SetValue(_testDirectReport, directReportId);

        var csvContent = @"Issue key,Summary,Assignee,Sprint
PROJ-123,Test Task,John Doe,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.AssigneeId == directReportId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithProjectName_AssignsToProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var idProperty = typeof(Project).GetProperty("Id");
        idProperty!.SetValue(_testProject, projectId);

        var csvContent = @"Issue key,Summary,Project,Sprint
PROJ-123,Test Task,Test Project,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { _testProject });
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.ProjectId == projectId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithStoryPoints_CalculatesEstimatedHours()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Story Points,Sprint
PROJ-123,Test Task,3,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        // 3 SP should map to 8 hours based on testAppSettings
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.StoryPoints == 3 &&
            t.EstimatedHours == 8
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithTimeSpent_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Σ Time Spent,Sprint
PROJ-123,Test Task,""2h 30m"",LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        // 2h 30m = 150 minutes
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.TimeSpentMinutes == 150
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithParentSummary_CreatesParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentDto = new ParentDto
        {
            Id = parentId,
            Name = "Parent Epic"
        };

        var csvContent = @"Issue key,Summary,Parent Summary,Sprint
PROJ-123,Test Task,Parent Epic,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _parentServiceMock.Setup(s => s.GetOrCreateAsync("Parent Epic", It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDto);

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        _parentServiceMock.Verify(s => s.GetOrCreateAsync("Parent Epic", It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.ParentId == parentId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithMultipleSprints_UsesFirstValidSprint()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Sprint
PROJ-123,Test Task,""LP_1Q25_S1,LP_1Q25_S2""";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => new SprintDto { Id = Guid.NewGuid(), Name = name });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.Sprint == "LP_1Q25_S1,LP_1Q25_S2"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Issue Type Mapping Tests

    [Theory]
    [InlineData("Epic", TaskType.Epic)]
    [InlineData("Story", TaskType.Story)]
    [InlineData("Bug", TaskType.Bug)]
    [InlineData("Sub-task", TaskType.SubTask)]
    [InlineData("Task", TaskType.Task)]
    [InlineData("Unknown", TaskType.Task)]
    public async Task ImportAsync_MapsIssueTypeCorrectly(string jiraType, TaskType expectedType)
    {
        // Arrange
        var csvContent = $@"Issue key,Summary,Issue Type,Sprint
PROJ-123,Test Task,{jiraType},LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.Type == expectedType
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Priority Mapping Tests

    [Theory]
    [InlineData("Lowest", TaskPriority.Low)]
    [InlineData("Low", TaskPriority.Low)]
    [InlineData("Medium", TaskPriority.Medium)]
    [InlineData("High", TaskPriority.High)]
    [InlineData("Highest", TaskPriority.Critical)]
    [InlineData("Critical", TaskPriority.Critical)]
    [InlineData("", TaskPriority.Medium)] // Default
    public async Task ImportAsync_MapsPriorityCorrectly(string jiraPriority, TaskPriority expectedPriority)
    {
        // Arrange
        var csvContent = $@"Issue key,Summary,Priority,Sprint
PROJ-123,Test Task,{jiraPriority},LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid(), Name = "LP_1Q25_S1", TeamName = "LP", Quarter = 1, Year = 25, SprintNumber = 1 });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.Priority == expectedPriority
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Override Preservation Tests

    [Fact]
    public async Task ImportAsync_WithOverriddenAssignee_PreservesAssigneeDuringUpdate()
    {
        // Arrange
        var originalAssigneeId = Guid.NewGuid();
        var existingTask = new TeamTask(
            "Test Task",
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            originalAssigneeId,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);
        existingTask.OverrideAssignee(originalAssigneeId);

        var csvContent = @"Issue key,Summary,Assignee,Status
PROJ-123,Test Task,Jane Smith,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        var newAssignee = new DirectReport("Jane", "Smith", "jane@test.com", "Engineer", "Eng", DateTime.UtcNow);

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { newAssignee });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        // Assignee should remain as original since it's overridden
        existingTask.AssigneeId.Should().Be(originalAssigneeId);
    }

    [Fact]
    public async Task ImportAsync_WithOverriddenStoryPoints_PreservesEstimationDuringUpdate()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);
        existingTask.OverrideEstimation(8, 40);

        var csvContent = @"Issue key,Summary,Story Points,Status
PROJ-123,Test Task,3,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        // Story points should remain as overridden value
        existingTask.StoryPoints.Should().Be(8);
        existingTask.EstimatedHours.Should().Be(40);
    }

    [Fact]
    public async Task ImportAsync_WithOverriddenTimeSpent_PreservesTimeSpentDuringUpdate()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);
        existingTask.OverrideTimeSpent(999);

        var csvContent = @"Issue key,Summary,Σ Time Spent,Status
PROJ-123,Test Task,3600,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        // Time spent should remain as overridden value
        existingTask.TimeSpentMinutes.Should().Be(999);
    }

    [Fact]
    public async Task ImportAsync_WithNoOverrides_UpdatesAllFieldsNormally()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Old Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            5,
            "jira:PROJ-123",
            "",
            null,
            "",
            120,
            null);

        var csvContent = @"Issue key,Summary,Story Points,Σ Time Spent,Status
PROJ-123,Test Task,3,3600,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        // All fields should be updated from CSV since no overrides
        existingTask.StoryPoints.Should().Be(3);
        existingTask.TimeSpentMinutes.Should().Be(60); // 3600 seconds = 60 minutes
    }

    [Fact]
    public async Task ImportAsync_AfterClearingOverride_UpdatesFieldFromCsv()
    {
        // Arrange
        var existingTask = new TeamTask(
            "Test Task",
            "Description",
            TaskType.Task,
            TaskPriority.Medium,
            null,
            null,
            null,
            null,
            null,
            "jira:PROJ-123",
            "",
            "",
            null,
            null);
        // Set override then clear it
        existingTask.OverrideTimeSpent(999);
        existingTask.ClearOverride("TimeSpentMinutes");

        var csvContent = @"Issue key,Summary,Σ Time Spent,Status
PROJ-123,Test Task,3600,Done";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = true,
            MatchField = "IssueKey"
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { existingTask });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _appSettingsServiceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testAppSettings);

        // Act
        var result = await _service.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        // After clearing override, time spent should be updated from CSV
        existingTask.TimeSpentMinutes.Should().Be(60); // 3600 seconds = 60 minutes
    }

    #endregion
}
