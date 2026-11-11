using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class BackupServiceTests
{
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IManagerNoteRepository> _managerNoteRepositoryMock;
    private readonly Mock<INoteFolderRepository> _noteFolderRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ISprintCapacityRepository> _sprintCapacityRepositoryMock;
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly Mock<IAppSettingsRepository> _appSettingsRepositoryMock = new();
    private readonly Mock<IAutomaticBackups> _automaticBackupsMock = new();
    private readonly Mock<IParentRepository> _parentRepositoryMock = EmptyRepository<IParentRepository, Parent>();
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _managerNoteRepositoryMock = new Mock<IManagerNoteRepository>();
        _noteFolderRepositoryMock = new Mock<INoteFolderRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _sprintCapacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _activityRepositoryMock = new Mock<IActivityRepository>();

        _service = new BackupService(
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _leaveRepositoryMock.Object,
            _managerNoteRepositoryMock.Object,
            _noteFolderRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _sprintCapacityRepositoryMock.Object,
            _activityRepositoryMock.Object,
            EmptyRepository<IProductRepository, Product>().Object,
            EmptyRepository<IProjectProductRepository, ProjectProduct>().Object,
            EmptyRepository<IProjectKnowledgeRepository, ProjectKnowledge>().Object,
            _parentRepositoryMock.Object,
            EmptyRepository<IQuarterRepository, Quarter>().Object,
            EmptyRepository<IQuarterMemberRepository, QuarterMember>().Object,
            EmptyRepository<IInitiativeRepository, Initiative>().Object,
            EmptyRepository<IInitiativeProductRepository, InitiativeProduct>().Object,
            EmptyRepository<IInitiativeMemberRepository, InitiativeMember>().Object,
            EmptyRepository<IInitiativeDependencyRepository, InitiativeDependency>().Object,
            EmptyRepository<IExternalDependencyRepository, ExternalDependency>().Object,
            EmptyRepository<IAllocationRepository, Allocation>().Object,
            EmptyRepository<ISprintGoalRepository, SprintGoal>().Object,
            EmptyRepository<ISkillRoadmapRepository, SkillRoadmap>().Object,
            EmptyRepository<IRoadmapSkillRepository, RoadmapSkill>().Object,
            EmptyRepository<ICareerPlanRepository, CareerPlan>().Object,
            EmptyRepository<ICareerPlanSkillRepository, CareerPlanSkill>().Object,
            EmptyRepository<ISentimentAnalysisCacheRepository, SentimentAnalysisCache>().Object,
            EmptyRepository<IRetroSummaryRepository, RetroSummary>().Object,
            EmptyRepository<ITaskReadinessReviewRepository, TaskReadinessReview>().Object,
            _appSettingsRepositoryMock.Object,
            _automaticBackupsMock.Object);

        // Folders are read on every export; an empty list is the default for these tests.
        _noteFolderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteFolder>());
    }

    #region ExportAsync Tests

    [Fact]
    public async Task ExportAsync_WritesEveryTableAndSettings_InTheCurrentFormat()
    {
        // Arrange
        SetupEmptyRepositories();

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Version.Should().Be("2.0");
        result.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Tables.Keys.Should().BeEquivalentTo(_service.TableNames.Append("AppSettings"));
        _service.TableNames.Should().HaveCount(30);
        result.DirectReports.Should().BeEmpty("the typed lists are format 1.0 and only read on restore");
    }

    [Fact]
    public async Task ExportAsync_CapturesEveryStoredField_AndNoComputedOnes()
    {
        // Arrange — state only reachable through the entity's methods
        var person = new DirectReport("Jane", "Roe", "jane@test.com", "Engineer", "Engineering", DateTime.UtcNow, isDirect: false);
        var task = CreateTask();
        task.Start();
        task.Complete();
        SetupEmptyRepositories();
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([person]);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        // Act
        var result = await _service.ExportAsync();

        // Assert
        var personRow = result.Tables["DirectReport"].Single();
        personRow.GetProperty("Id").GetGuid().Should().Be(person.Id);
        personRow.GetProperty("IsDirect").GetBoolean().Should().BeFalse();
        personRow.TryGetProperty("FullName", out _).Should().BeFalse("it is computed, not stored");
        var taskRow = result.Tables["TeamTask"].Single();
        taskRow.GetProperty("Status").GetInt32().Should().Be((int)Hive.Core.Entities.TaskStatus.Done);
        taskRow.GetProperty("StatusChangedAt").GetDateTime().Should().Be(task.StatusChangedAt);
    }

    [Fact]
    public async Task ExportAsync_LeavesTheSecretsOutOfSettings()
    {
        // Arrange
        var settings = new AppSettings();
        settings.UpdateClaudeApiKey("sk-secret");
        settings.UpdateOneOnOneCadence(21);
        SetupEmptyRepositories();
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Act
        var result = await _service.ExportAsync();

        // Assert
        var row = result.Tables["AppSettings"].Single();
        row.GetProperty("OneOnOneCadenceDays").GetInt32().Should().Be(21);
        row.TryGetProperty("ClaudeApiKey", out _).Should().BeFalse();
        row.TryGetProperty("JiraApiToken", out _).Should().BeFalse();
        row.TryGetProperty("GoogleCalendarUrl", out _).Should().BeFalse();
        row.GetRawText().Should().NotContain("sk-secret");
    }

    [Fact]
    public async Task ImportAsync_CurrentFormat_AddsWhatIsMissingAndKeepsWhatExists()
    {
        // Arrange — a backup of two parents, one of which Hive already has
        var kept = Parent.FromJira("MAR-1", "Epic", "Kept");
        var added = Parent.FromJira("MAR-2", "Epic", "Added");
        var backup = new BackupDto
        {
            Version = "2.0",
            Tables = new() { ["Parent"] = [EntitySnapshot.Capture(kept), EntitySnapshot.Capture(added)] }
        };
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([kept]);
        Parent? stored = null;
        _parentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .Callback<Parent, CancellationToken>((p, _) => stored = p)
            .ReturnsAsync((Parent p, CancellationToken _) => p);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.Restored.Should().Equal(new Dictionary<string, int> { ["Parent"] = 1 });
        result.Skipped.Should().Equal(new Dictionary<string, int> { ["Parent"] = 1 });
        stored!.Id.Should().Be(added.Id);
        stored.JiraKey.Should().Be("MAR-2");
        stored.Name.Should().Be("Added");
    }

    [Fact]
    public async Task ImportAsync_CurrentFormat_TakesTheSettingsButKeepsTheSecrets()
    {
        // Arrange
        var saved = new AppSettings();
        saved.UpdateOneOnOneCadence(21);
        var current = new AppSettings();
        current.UpdateClaudeApiKey("sk-current");
        var backup = new BackupDto { Version = "2.0", Tables = new() { ["AppSettings"] = [EntitySnapshot.Capture(saved)] } };
        _appSettingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.SettingsRestored.Should().BeTrue();
        current.OneOnOneCadenceDays.Should().Be(21);
        current.ClaudeApiKey.Should().Be("sk-current");
        _appSettingsRepositoryMock.Verify(r => r.UpdateAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_CurrentFormat_WarnsAboutATableItDoesNotKnow()
    {
        // Arrange
        var backup = new BackupDto { Version = "2.0", Tables = new() { ["Spaceship"] = [JsonSerializer.SerializeToElement(new { Id = Guid.NewGuid() })] } };

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.Warnings.Should().ContainSingle().Which.Should().Contain("Spaceship");
    }

    [Fact]
    public void GetAutomaticBackupStatus_PassesOnWhatTheBackupsReport()
    {
        var status = new AutomaticBackupStatusDto { Enabled = true, Folder = "/data", Count = 3, IntervalHours = 1 };
        _automaticBackupsMock.Setup(b => b.GetStatus()).Returns(status);

        _service.GetAutomaticBackupStatus().Should().Be(status);
    }

    #endregion

    #region ImportAsync Tests

    [Fact]
    public async Task ImportAsync_WithEmptyBackup_ReturnsSuccessWithZeroRestored()
    {
        // Arrange
        var backup = new BackupDto();

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.DirectReportsRestored.Should().Be(0);
        result.ProjectsRestored.Should().Be(0);
        result.TasksRestored.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithNewDirectReport_RestoresDirectReport()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var backup = new BackupDto
        {
            DirectReports = new List<DirectReportBackup>
            {
                new()
                {
                    Id = directReportId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@test.com",
                    JobTitle = "Engineer",
                    Department = "Engineering",
                    HireDate = DateTime.UtcNow
                }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);
        _directReportRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport dr, CancellationToken _) => dr);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.DirectReportsRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        _directReportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithExistingDirectReport_SkipsAndAddsWarning()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var existingDirectReport = CreateDirectReport();
        var backup = new BackupDto
        {
            DirectReports = new List<DirectReportBackup>
            {
                new()
                {
                    Id = directReportId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@test.com",
                    JobTitle = "Engineer",
                    Department = "Engineering",
                    HireDate = DateTime.UtcNow
                }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDirectReport);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.DirectReportsRestored.Should().Be(0);
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("already exists");
        _directReportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WithNewProject_RestoresProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var backup = new BackupDto
        {
            Projects = new List<ProjectBackup>
            {
                new()
                {
                    Id = projectId,
                    Name = "Test Project",
                    Description = "Test Description",
                    Url = "https://github.com/test/project"
                }
            }
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.ProjectsRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithNewTask_RestoresTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var backup = new BackupDto
        {
            Tasks = new List<TeamTaskBackup>
            {
                new()
                {
                    Id = taskId,
                    Title = "Test Task",
                    Description = "Test Description",
                    Type = (int)TaskType.Task,
                    Priority = (int)TaskPriority.High,
                    Status = (int)Hive.Core.Entities.TaskStatus.InProgress,
                    Tags = "tag1,tag2",
                    Labels = "label1"
                }
            }
        };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.TasksRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithNewSprint_RestoresSprint()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var backup = new BackupDto
        {
            Sprints = new List<SprintBackup>
            {
                new()
                {
                    Id = sprintId,
                    Name = "Sprint 1"
                }
            }
        };

        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);
        _sprintRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint s, CancellationToken _) => s);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.SprintsRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        _sprintRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithRepositoryException_AddsErrorAndContinues()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var backup = new BackupDto
        {
            DirectReports = new List<DirectReportBackup>
            {
                new()
                {
                    Id = directReportId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@test.com",
                    JobTitle = "Engineer",
                    Department = "Engineering",
                    HireDate = DateTime.UtcNow
                }
            },
            Projects = new List<ProjectBackup>
            {
                new()
                {
                    Id = projectId,
                    Name = "Test Project",
                    Description = "Test Description"
                }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeFalse();
        result.DirectReportsRestored.Should().Be(0);
        result.ProjectsRestored.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Database error");
    }

    [Fact]
    public async Task ImportAsync_WithCompleteBackup_RestoresAllEntities()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var meetingId = Guid.NewGuid();
        var meetingNoteId = Guid.NewGuid();
        var leaveId = Guid.NewGuid();
        var managerNoteId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        var sprintCapacityId = Guid.NewGuid();

        var backup = new BackupDto
        {
            DirectReports = new List<DirectReportBackup>
            {
                new()
                {
                    Id = directReportId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@test.com",
                    JobTitle = "Engineer",
                    Department = "Engineering",
                    HireDate = DateTime.UtcNow
                }
            },
            Projects = new List<ProjectBackup>
            {
                new()
                {
                    Id = projectId,
                    Name = "Test Project",
                    Description = "Description"
                }
            },
            Tasks = new List<TeamTaskBackup>
            {
                new()
                {
                    Id = taskId,
                    Title = "Test Task",
                    Description = "Description",
                    Type = (int)TaskType.Task,
                    Priority = (int)TaskPriority.High,
                    Tags = "",
                    Labels = ""
                }
            },
            Meetings = new List<OneOnOneMeetingBackup>
            {
                new()
                {
                    Id = meetingId,
                    DirectReportId = directReportId,
                    MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Content = "Check-in"
                }
            },
            MeetingNotes = new List<MeetingNoteBackup>
            {
                new()
                {
                    Id = meetingNoteId,
                    MeetingId = meetingId,
                    Content = "Note content",
                    Category = 0
                }
            },
            Leaves = new List<LeaveBackup>
            {
                new()
                {
                    Id = leaveId,
                    DirectReportId = directReportId,
                    Type = (int)LeaveType.Vacation,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(5),
                    Notes = "Vacation"
                }
            },
            ManagerNotes = new List<ManagerNoteBackup>
            {
                new()
                {
                    Id = managerNoteId,
                    Title = "Note Title",
                    Content = "Note Content",
                    Tags = "tag1"
                }
            },
            Sprints = new List<SprintBackup>
            {
                new()
                {
                    Id = sprintId,
                    Name = "Sprint 1"
                }
            },
            SprintCapacities = new List<SprintCapacityBackup>
            {
                new()
                {
                    Id = sprintCapacityId,
                    SprintId = sprintId,
                    TotalCapacityPoints = 50,
                    AvailableMembers = 5
                }
            },
        };

        // Setup all repositories to return null (new entities)
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);
        // The meeting store has to remember what it restored: legacy 1:1 notes are folded
        // into the body of the meeting that was just added.
        OneOnOneMeeting? restoredMeeting = null;
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => restoredMeeting);
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);
        _managerNoteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);
        _sprintCapacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Setup Add methods
        _directReportRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport dr, CancellationToken _) => dr);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);
        _meetingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneOnOneMeeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting m, CancellationToken _) =>
            {
                restoredMeeting = m;
                return m;
            });
        _leaveRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Leave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave l, CancellationToken _) => l);
        _managerNoteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote n, CancellationToken _) => n);
        _sprintRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint s, CancellationToken _) => s);
        _sprintCapacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity sc, CancellationToken _) => sc);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.DirectReportsRestored.Should().Be(1);
        result.ProjectsRestored.Should().Be(1);
        result.TasksRestored.Should().Be(1);
        result.MeetingsRestored.Should().Be(1);
        result.MeetingNotesRestored.Should().Be(1);
        restoredMeeting!.Content.Should().Contain("Note content");
        result.LeavesRestored.Should().Be(1);
        result.ManagerNotesRestored.Should().Be(1);
        result.SprintsRestored.Should().Be(1);
        result.SprintCapacitiesRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    // A repository whose "get all" answers an empty list rather than null
    private static Mock<TRepository> EmptyRepository<TRepository, TEntity>() where TRepository : class
    {
        var mock = new Mock<TRepository>();
        mock.SetReturnsDefault(Task.FromResult<IReadOnlyList<TEntity>>([]));
        return mock;
    }

    private void SetupEmptyRepositories()
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _managerNoteRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManagerNote>());
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint>());
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SprintCapacity>());
        _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());
    }

    private void SetupEmptyRepositoriesExcept(params string[] exceptRepositories)
    {
        if (!exceptRepositories.Contains(nameof(IDirectReportRepository)))
            _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DirectReport>());

        if (!exceptRepositories.Contains(nameof(IProjectRepository)))
            _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Project>());

        if (!exceptRepositories.Contains(nameof(ITeamTaskRepository)))
            _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TeamTask>());


        if (!exceptRepositories.Contains(nameof(IOneOnOneMeetingRepository)))
            _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OneOnOneMeeting>());

        if (!exceptRepositories.Contains(nameof(ILeaveRepository)))
            _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Leave>());

        if (!exceptRepositories.Contains(nameof(IManagerNoteRepository)))
            _managerNoteRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ManagerNote>());

        if (!exceptRepositories.Contains(nameof(ISprintRepository)))
            _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Sprint>());

        if (!exceptRepositories.Contains(nameof(ISprintCapacityRepository)))
            _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SprintCapacity>());


        if (!exceptRepositories.Contains(nameof(IActivityRepository)))
            _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Activity>());
    }

    private static DirectReport CreateDirectReport() =>
        new("John", "Doe", "john.doe@test.com", "Engineer", "Engineering", DateTime.UtcNow);

    private static Project CreateProject() =>
        new("Test Project", "Description", "https://github.com/test/project");

    private static TeamTask CreateTask() =>
        new("Test Task", "Description", TaskType.Task, TaskPriority.High, tags: "tag1", labels: "label1");

    private static OneOnOneMeeting CreateMeeting(Guid? directReportId = null) =>
        new(DateOnly.FromDateTime(DateTime.UtcNow), "Check-in\n\n- Went well", "johndoe", directReportId ?? Guid.NewGuid());

    private static Leave CreateLeave(Guid directReportId) =>
        new(directReportId, LeaveType.Vacation, DateTime.UtcNow, DateTime.UtcNow.AddDays(5), "Vacation");

    private static ManagerNote CreateManagerNote() =>
        new("Note Title", "Note Content", "tag1");

    private static Sprint CreateSprint() =>
        new("Sprint 1");

    private static SprintCapacity CreateSprintCapacity(Guid sprintId) =>
        new(sprintId, 50, 5);


    #endregion

    [Fact]
    public async Task ImportAsync_RestoresFoldersAndKeepsNotesFiledInThem()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var backup = new BackupDto
        {
            NoteFolders = new List<NoteFolderBackup>
            {
                new() { Id = folderId, Name = "Work", SortOrder = 0, CreatedAt = DateTime.UtcNow }
            },
            ManagerNotes = new List<ManagerNoteBackup>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Note",
                    Content = "Body",
                    FolderId = folderId,
                    IsPinned = true,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        NoteFolder? restoredFolder = null;
        ManagerNote? restoredNote = null;

        _noteFolderRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolder?)null);
        _noteFolderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<NoteFolder>(), It.IsAny<CancellationToken>()))
            .Callback<NoteFolder, CancellationToken>((folder, _) => restoredFolder = folder)
            .ReturnsAsync((NoteFolder folder, CancellationToken _) => folder);
        _managerNoteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);
        _managerNoteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()))
            .Callback<ManagerNote, CancellationToken>((note, _) => restoredNote = note)
            .ReturnsAsync((ManagerNote note, CancellationToken _) => note);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.NoteFoldersRestored.Should().Be(1);
        result.ManagerNotesRestored.Should().Be(1);
        restoredFolder!.Id.Should().Be(folderId);
        restoredNote!.FolderId.Should().Be(folderId);
        restoredNote.IsPinned.Should().BeTrue();
    }
}
