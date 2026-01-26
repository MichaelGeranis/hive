using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Integration;

/// <summary>
/// Integration tests for JiraImportService testing the full import flow
/// with real services, repositories, and in-memory database.
/// </summary>
public class JiraImportServiceIntegrationTests : IntegrationTestBase
{
    private readonly IJiraImportService _jiraImportService;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintService _sprintService;
    private readonly IParentService _parentService;
    private readonly IAppSettingsService _appSettingsService;

    public JiraImportServiceIntegrationTests() : base(seedData: false)
    {
        _jiraImportService = GetService<IJiraImportService>();
        _taskRepository = GetService<ITeamTaskRepository>();
        _directReportRepository = GetService<IDirectReportRepository>();
        _projectRepository = GetService<IProjectRepository>();
        _sprintService = GetService<ISprintService>();
        _parentService = GetService<IParentService>();
        _appSettingsService = GetService<IAppSettingsService>();
    }

    #region Full Import Flow Tests

    [Fact]
    public async Task ImportAsync_WithValidCsv_CreatesTasksWithCorrectData()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Description,Issue Type,Status,Priority,Sprint
PROJ-1,First Task,Description for first task,Story,Done,High,LP_1Q25_S1
PROJ-2,Second Task,Description for second task,Bug,In Progress,Critical,LP_1Q25_S1
PROJ-3,Third Task,Description for third task,Task,To Do,Medium,LP_1Q25_S2";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.TotalRows.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);

        // Verify tasks were created in database
        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().HaveCount(3);

        var firstTask = tasks.First(t => t.Title == "First Task");
        firstTask.Description.Should().Be("Description for first task");
        firstTask.Type.Should().Be(TaskType.Story);
        firstTask.Status.Should().Be(TaskStatus.Done);
        firstTask.Priority.Should().Be(TaskPriority.High);
        firstTask.Tags.Should().Contain("jira:PROJ-1");

        var secondTask = tasks.First(t => t.Title == "Second Task");
        secondTask.Type.Should().Be(TaskType.Bug);
        secondTask.Status.Should().Be(TaskStatus.InProgress);
        secondTask.Priority.Should().Be(TaskPriority.Critical);

        // Verify sprints were auto-created
        var sprints = await _sprintService.GetAllAsync();
        sprints.Should().Contain(s => s.Name == "LP_1Q25_S1");
        sprints.Should().Contain(s => s.Name == "LP_1Q25_S2");
    }

    [Fact]
    public async Task ImportAsync_WithStoryPoints_CalculatesEstimatedHoursFromSettings()
    {
        // Arrange - First ensure settings exist with default mappings
        var settings = await _appSettingsService.GetAsync();
        settings.StoryPointMappings.Should().NotBeEmpty("AppSettingsService should create default mappings");

        var csvContent = @"Issue key,Summary,Story Points,Sprint
PROJ-1,Small Task,1,LP_1Q25_S1
PROJ-2,Medium Task,3,LP_1Q25_S1
PROJ-3,Large Task,5,LP_1Q25_S1
PROJ-4,XL Task,8,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(4);

        var tasks = await _taskRepository.GetAllAsync();

        // Verify story points are set and estimated hours calculated based on default mappings
        // Default: 1SP=2h, 2SP=4h, 3SP=8h, 5SP=24h, 8SP=72h
        var task1 = tasks.First(t => t.Title == "Small Task");
        task1.StoryPoints.Should().Be(1);
        task1.EstimatedHours.Should().Be(2);

        var task2 = tasks.First(t => t.Title == "Medium Task");
        task2.StoryPoints.Should().Be(3);
        task2.EstimatedHours.Should().Be(8);

        var task3 = tasks.First(t => t.Title == "Large Task");
        task3.StoryPoints.Should().Be(5);
        task3.EstimatedHours.Should().Be(24);

        var task4 = tasks.First(t => t.Title == "XL Task");
        task4.StoryPoints.Should().Be(8);
        task4.EstimatedHours.Should().Be(72);
    }

    [Fact]
    public async Task ImportAsync_WithAssignee_MatchesDirectReport()
    {
        // Arrange - Create a direct report first
        var directReport = new DirectReport(
            "John",
            "Doe",
            "john.doe@test.com",
            "Software Engineer",
            "Engineering",
            DateTime.UtcNow.AddYears(-2));
        await _directReportRepository.AddAsync(directReport);

        var csvContent = @"Issue key,Summary,Assignee,Sprint
PROJ-1,Assigned Task,John Doe,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.First();
        task.AssigneeId.Should().Be(directReport.Id);
    }

    [Fact]
    public async Task ImportAsync_WithProject_MatchesProject()
    {
        // Arrange - Create a project first
        var project = new Project("Test Project", "Test Description", "test", "https://example.com");
        await _projectRepository.AddAsync(project);

        var csvContent = @"Issue key,Summary,Project,Sprint
PROJ-1,Project Task,Test Project,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.First();
        task.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task ImportAsync_WithParentSummary_CreatesParentAndLinks()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Parent Summary,Sprint
PROJ-1,Child Task,Epic Parent,LP_1Q25_S1
PROJ-2,Another Child,Epic Parent,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(2);

        // Verify parent was created
        var parents = await _parentService.GetAllAsync();
        parents.Should().ContainSingle(p => p.Name == "Epic Parent");

        // Verify both tasks are linked to the same parent
        var tasks = await _taskRepository.GetAllAsync();
        var parentId = parents.First(p => p.Name == "Epic Parent").Id;
        tasks.Should().OnlyContain(t => t.ParentId == parentId);
    }

    [Fact]
    public async Task ImportAsync_WithEpic_SetsParentTimeSpentAndTeamTaskId()
    {
        // Arrange - Import an Epic with time spent
        var csvContent = @"Issue key,Summary,Issue Type,Σ Time Spent,Sprint
PROJ-1,My Epic,Epic,2h 30m,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        // Verify the task was created as an Epic
        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().ContainSingle();
        var epicTask = tasks.First();
        epicTask.Type.Should().Be(TaskType.Epic);
        epicTask.TimeSpentMinutes.Should().Be(150); // 2h 30m = 150 minutes

        // Verify the Parent was created with TimeSpentMinutes and TeamTaskId
        var parents = await _parentService.GetAllAsync();
        parents.Should().ContainSingle(p => p.Name == "My Epic");
        var parent = parents.First();
        parent.TimeSpentMinutes.Should().Be(150);
        parent.TeamTaskId.Should().Be(epicTask.Id);
    }

    [Fact]
    public async Task ImportAsync_WithEpicAndChildren_SetsParentTimeSpentAndTeamTaskId()
    {
        // Arrange - Import children first, then the Epic
        var csvContent = @"Issue key,Summary,Issue Type,Parent Summary,Σ Time Spent,Sprint
PROJ-2,Child Task 1,Story,My Epic,30m,LP_1Q25_S1
PROJ-3,Child Task 2,Story,My Epic,45m,LP_1Q25_S1
PROJ-1,My Epic,Epic,,2h 30m,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(3);

        // Verify the Epic task was created
        var tasks = await _taskRepository.GetAllAsync();
        var epicTask = tasks.First(t => t.Type == TaskType.Epic);
        epicTask.Title.Should().Be("My Epic");
        epicTask.TimeSpentMinutes.Should().Be(150);

        // Verify the Parent has TimeSpentMinutes and TeamTaskId set
        var parents = await _parentService.GetAllAsync();
        parents.Should().ContainSingle(p => p.Name == "My Epic");
        var parent = parents.First();
        parent.TimeSpentMinutes.Should().Be(150); // Epic's own time
        parent.TeamTaskId.Should().Be(epicTask.Id);

        // Verify children are linked to the parent
        var childTasks = tasks.Where(t => t.Type != TaskType.Epic).ToList();
        childTasks.Should().HaveCount(2);
        childTasks.Should().OnlyContain(t => t.ParentId == parent.Id);
    }

    [Fact]
    public async Task ImportAsync_WithParentKey_IdentifiesParentTaskAndSetsTimeSpentAndTeamTaskId()
    {
        // Arrange - Import tasks where parent is identified by "Parent key" column, not Issue Type
        // This simulates a CSV where any task can be a parent (not just Epics)
        var csvContent = @"Issue key,Summary,Issue Type,Parent key,Parent summary,Σ Time Spent,Sprint
PROJ-1,Parent Task,Story,,,3h,LP_1Q25_S1
PROJ-2,Child Task 1,Story,PROJ-1,Parent Task,30m,LP_1Q25_S1
PROJ-3,Child Task 2,Task,PROJ-1,Parent Task,45m,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(3);

        // Verify the parent task was created (it's a Story, not an Epic)
        var tasks = await _taskRepository.GetAllAsync();
        var parentTask = tasks.First(t => t.Title == "Parent Task");
        parentTask.Type.Should().Be(TaskType.Story); // Not an Epic!
        parentTask.TimeSpentMinutes.Should().Be(180); // 3h = 180 minutes

        // Verify the Parent entity has TimeSpentMinutes and TeamTaskId set
        // because PROJ-1 is referenced by other tasks via "Parent key"
        var parents = await _parentService.GetAllAsync();
        parents.Should().ContainSingle(p => p.Name == "Parent Task");
        var parent = parents.First();
        parent.TimeSpentMinutes.Should().Be(180); // Parent's own time
        parent.TeamTaskId.Should().Be(parentTask.Id);

        // Verify children are linked to the parent
        var childTasks = tasks.Where(t => t.Title != "Parent Task").ToList();
        childTasks.Should().HaveCount(2);
        childTasks.Should().OnlyContain(t => t.ParentId == parent.Id);
    }

    [Fact]
    public async Task ImportAsync_WithUpdateExisting_UpdatesExistingTask()
    {
        // Arrange - First import
        var csvContent1 = @"Issue key,Summary,Description,Status,Sprint
PROJ-1,Original Title,Original Description,To Do,LP_1Q25_S1";

        await _jiraImportService.ImportAsync(new JiraImportRequestDto
        {
            CsvContent = csvContent1,
            UpdateExisting = false,
            MatchField = "IssueKey"
        });

        // Verify initial state
        var initialTasks = await _taskRepository.GetAllAsync();
        initialTasks.Should().ContainSingle();
        initialTasks.First().Description.Should().Be("Original Description");

        // Arrange - Second import with updates
        var csvContent2 = @"Issue key,Summary,Description,Status,Sprint
PROJ-1,Original Title,Updated Description,Done,LP_1Q25_S1";

        // Act - Import with UpdateExisting = true
        var result = await _jiraImportService.ImportAsync(new JiraImportRequestDto
        {
            CsvContent = csvContent2,
            UpdateExisting = true,
            MatchField = "IssueKey"
        });

        // Assert
        result.SuccessCount.Should().Be(1);
        result.ImportedTasks.First().IsUpdated.Should().BeTrue();
        result.ImportedTasks.First().IsNew.Should().BeFalse();

        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().ContainSingle(); // Still only one task
        tasks.First().Description.Should().Be("Updated Description");
        tasks.First().Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task ImportAsync_WithUpdateExistingFalse_SkipsExistingTask()
    {
        // Arrange - First import
        var csvContent = @"Issue key,Summary,Sprint
PROJ-1,Task Title,LP_1Q25_S1";

        await _jiraImportService.ImportAsync(new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        });

        // Act - Second import without UpdateExisting
        var result = await _jiraImportService.ImportAsync(new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        });

        // Assert
        result.SkippedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Warnings.Should().Contain(w => w.Contains("already exists, skipping"));

        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().ContainSingle();
    }

    #endregion

    #region Sprint Filtering Tests

    [Fact]
    public async Task ImportAsync_WithInvalidSprintPattern_SkipsTask()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Sprint
PROJ-1,Invalid Sprint Task,InvalidSprintName";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SkippedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Warnings.Should().Contain(w => w.Contains("doesn't match required pattern"));

        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithMultipleSprints_KeepsOnlyValidOnes()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Sprint
PROJ-1,Multi Sprint Task,""LP_1Q25_S1,InvalidSprint,LP_1Q25_S2""";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.Warnings.Should().Contain(w => w.Contains("InvalidSprint") && w.Contains("doesn't match"));

        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.First();
        task.Sprint.Should().Contain("LP_1Q25_S1");
        task.Sprint.Should().Contain("LP_1Q25_S2");
        task.Sprint.Should().NotContain("InvalidSprint");
    }

    [Fact]
    public async Task ImportAsync_WithSprintTeamFilter_FiltersSprintsByTeam()
    {
        // Arrange - Set up team filter in settings
        await _appSettingsService.UpdateAsync(new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>
            {
                new() { Points = 1, Hours = 2, Label = "1 SP" }
            },
            SprintTeamFilter = "LP"
        });

        var csvContent = @"Issue key,Summary,Sprint
PROJ-1,LP Team Task,LP_1Q25_S1
PROJ-2,Other Team Task,OTHER_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1); // Only LP team task
        result.SkippedCount.Should().Be(1); // Other team task skipped
        result.Warnings.Should().Contain(w => w.Contains("OTHER_1Q25_S1") && w.Contains("doesn't match configured team filter"));

        var tasks = await _taskRepository.GetAllAsync();
        tasks.Should().ContainSingle();
        tasks.First().Title.Should().Be("LP Team Task");
    }

    #endregion

    #region Status Mapping Tests

    [Theory]
    [InlineData("Backlog", TaskStatus.Backlog)]
    [InlineData("To Do", TaskStatus.Todo)]
    [InlineData("In Progress", TaskStatus.InProgress)]
    [InlineData("In Review", TaskStatus.InReview)]
    [InlineData("Done", TaskStatus.Done)]
    [InlineData("Cancelled", TaskStatus.Cancelled)]
    [InlineData("Blocked", TaskStatus.Blocked)]
    public async Task ImportAsync_MapsJiraStatusCorrectly(string jiraStatus, TaskStatus expectedStatus)
    {
        // Arrange
        var csvContent = $@"Issue key,Summary,Status,Sprint
PROJ-1,Status Test Task,{jiraStatus},LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        tasks.First().Status.Should().Be(expectedStatus);
    }

    #endregion

    #region Time Tracking Tests

    [Theory]
    [InlineData("2h 30m", 150)]
    [InlineData("1d", 480)]
    [InlineData("1w", 2400)]
    [InlineData("45m", 45)]
    [InlineData("3600", 60)] // Seconds
    public async Task ImportAsync_ParsesTimeSpentCorrectly(string timeSpent, int expectedMinutes)
    {
        // Arrange
        var csvContent = $@"Issue key,Summary,Σ Time Spent,Sprint
PROJ-1,Time Test Task,{timeSpent},LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        tasks.First().TimeSpentMinutes.Should().Be(expectedMinutes);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ImportAsync_WithMissingRequiredFields_ReportsErrors()
    {
        // Arrange - Row with neither issue key nor summary
        var csvContent = @"Issue key,Summary,Sprint
,,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.ErrorCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("Missing both Issue Key and Summary"));
    }

    [Fact]
    public async Task ImportAsync_WithUnmatchedProject_AddsWarning()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Project,Sprint
PROJ-1,Task Title,NonExistentProject,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1); // Task still created
        result.Warnings.Should().Contain(w => w.Contains("NonExistentProject") && w.Contains("not found"));

        var tasks = await _taskRepository.GetAllAsync();
        tasks.First().ProjectId.Should().BeNull();
    }

    #endregion

    #region Preview Tests

    [Fact]
    public async Task PreviewImportAsync_ReturnsAccuratePreview()
    {
        // Arrange - A row is valid if it has either issue key OR summary
        var csvContent = @"Issue key,Summary,Status,Story Points
PROJ-1,Valid Task,Done,5
,Has Summary Only,Done,3
PROJ-3,,Done,2
,,Done,1";

        // Act
        var result = await _jiraImportService.PreviewImportAsync(csvContent);

        // Assert
        result.TotalRows.Should().Be(4);
        result.ValidRows.Should().Be(2); // First two rows are valid (have summary)
        result.InvalidRows.Should().Be(2); // Last two rows invalid (no summary)
        result.DetectedColumns.Should().Contain("Issue key");
        result.DetectedColumns.Should().Contain("Summary");
        result.DetectedColumns.Should().Contain("Status");
        result.DetectedColumns.Should().Contain("Story Points");

        result.SampleRows.Should().HaveCount(4);
        result.SampleRows[0].IsValid.Should().BeTrue();  // Has both key and summary
        result.SampleRows[1].IsValid.Should().BeTrue();  // Has summary (key not required)
        result.SampleRows[2].IsValid.Should().BeFalse(); // Has key but no summary
        result.SampleRows[3].IsValid.Should().BeFalse(); // Has neither
    }

    [Fact]
    public async Task PreviewImportAsync_HandlesLargeFileWithSampleLimit()
    {
        // Arrange - Create CSV with 25 rows
        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Issue key,Summary,Status");

        for (int i = 1; i <= 25; i++)
        {
            csvBuilder.AppendLine($"PROJ-{i},Task {i},Done");
        }

        // Act
        var result = await _jiraImportService.PreviewImportAsync(csvBuilder.ToString());

        // Assert
        result.TotalRows.Should().Be(25);
        result.ValidRows.Should().Be(25);
        result.SampleRows.Should().HaveCount(10); // Limited to 10 samples
    }

    #endregion

    #region Labels and Tags Tests

    [Fact]
    public async Task ImportAsync_WithLabels_ImportsLabelsCorrectly()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Labels,Sprint
PROJ-1,Labeled Task,""frontend,urgent,p1"",LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.First();
        task.Labels.Should().Contain("frontend");
        task.Labels.Should().Contain("urgent");
        task.Labels.Should().Contain("p1");
    }

    [Fact]
    public async Task ImportAsync_AddsJiraTagToImportedTasks()
    {
        // Arrange
        var csvContent = @"Issue key,Summary,Sprint
PROJ-123,Tagged Task,LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.First();
        task.Tags.Should().Contain("jira:PROJ-123");
        task.Tags.Should().Contain("imported-from-jira");
    }

    #endregion

    #region Due Date Tests

    [Theory]
    [InlineData("2024-01-15")]
    [InlineData("15/Jan/24")]
    [InlineData("01/15/2024")]
    public async Task ImportAsync_ParsesDueDateFormats(string dueDateStr)
    {
        // Arrange
        var csvContent = $@"Issue key,Summary,Due date,Sprint
PROJ-1,Due Date Task,{dueDateStr},LP_1Q25_S1";

        var request = new JiraImportRequestDto
        {
            CsvContent = csvContent,
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        // Act
        var result = await _jiraImportService.ImportAsync(request);

        // Assert
        result.SuccessCount.Should().Be(1);

        var tasks = await _taskRepository.GetAllAsync();
        tasks.First().DueDate.Should().NotBeNull();
        tasks.First().DueDate!.Value.Year.Should().Be(2024);
        tasks.First().DueDate!.Value.Month.Should().Be(1);
        tasks.First().DueDate!.Value.Day.Should().Be(15);
    }

    #endregion
}
