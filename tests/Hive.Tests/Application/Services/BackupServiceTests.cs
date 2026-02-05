using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class BackupServiceTests
{
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock;
    private readonly Mock<IPerformanceReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IMeetingNoteRepository> _meetingNoteRepositoryMock;
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IManagerNoteRepository> _managerNoteRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ISprintCapacityRepository> _sprintCapacityRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly Mock<ISkillRepository> _skillRepositoryMock;
    private readonly Mock<ISkillAssessmentRepository> _skillAssessmentRepositoryMock;
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _taskRepositoryMock = new Mock<ITeamTaskRepository>();
        _reviewRepositoryMock = new Mock<IPerformanceReviewRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _meetingNoteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _managerNoteRepositoryMock = new Mock<IManagerNoteRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _sprintCapacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _activityRepositoryMock = new Mock<IActivityRepository>();
        _skillRepositoryMock = new Mock<ISkillRepository>();
        _skillAssessmentRepositoryMock = new Mock<ISkillAssessmentRepository>();
        _settingsRepositoryMock = new Mock<IAppSettingsRepository>();

        _service = new BackupService(
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _meetingNoteRepositoryMock.Object,
            _leaveRepositoryMock.Object,
            _managerNoteRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _sprintCapacityRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _activityRepositoryMock.Object,
            _skillRepositoryMock.Object,
            _skillAssessmentRepositoryMock.Object,
            _settingsRepositoryMock.Object);
    }

    #region ExportAsync Tests

    [Fact]
    public async Task ExportAsync_WithNoData_ReturnsEmptyBackup()
    {
        // Arrange
        SetupEmptyRepositories();

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be("1.0");
        result.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.DirectReports.Should().BeEmpty();
        result.Projects.Should().BeEmpty();
        result.Tasks.Should().BeEmpty();
        result.PerformanceReviews.Should().BeEmpty();
        result.Meetings.Should().BeEmpty();
        result.MeetingNotes.Should().BeEmpty();
        result.Leaves.Should().BeEmpty();
        result.ManagerNotes.Should().BeEmpty();
        result.Sprints.Should().BeEmpty();
        result.SprintCapacities.Should().BeEmpty();
        result.Documents.Should().BeEmpty();
        result.Activities.Should().BeEmpty();
        result.Skills.Should().BeEmpty();
        result.SkillAssessments.Should().BeEmpty();
        result.Settings.Should().BeNull();
    }

    [Fact]
    public async Task ExportAsync_WithDirectReports_IncludesDirectReportsInBackup()
    {
        // Arrange
        var directReport = CreateDirectReport();
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });
        SetupEmptyRepositoriesExcept(nameof(IDirectReportRepository));

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.DirectReports.Should().HaveCount(1);
        var backup = result.DirectReports[0];
        backup.Id.Should().Be(directReport.Id);
        backup.FirstName.Should().Be(directReport.FirstName);
        backup.LastName.Should().Be(directReport.LastName);
        backup.Email.Should().Be(directReport.Email);
        backup.JobTitle.Should().Be(directReport.JobTitle);
        backup.Department.Should().Be(directReport.Department);
        backup.HireDate.Should().Be(directReport.HireDate);
    }

    [Fact]
    public async Task ExportAsync_WithProjects_IncludesProjectsInBackup()
    {
        // Arrange
        var project = CreateProject();
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        SetupEmptyRepositoriesExcept(nameof(IProjectRepository));

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Projects.Should().HaveCount(1);
        var backup = result.Projects[0];
        backup.Id.Should().Be(project.Id);
        backup.Name.Should().Be(project.Name);
        backup.Description.Should().Be(project.Description);
    }

    [Fact]
    public async Task ExportAsync_WithTasks_IncludesTasksInBackup()
    {
        // Arrange
        var task = CreateTask();
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task });
        SetupEmptyRepositoriesExcept(nameof(ITeamTaskRepository));

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Tasks.Should().HaveCount(1);
        var backup = result.Tasks[0];
        backup.Id.Should().Be(task.Id);
        backup.Title.Should().Be(task.Title);
        backup.Description.Should().Be(task.Description);
        backup.Type.Should().Be((int)task.Type);
        backup.Priority.Should().Be((int)task.Priority);
    }

    [Fact]
    public async Task ExportAsync_WithMeetingsAndNotes_IncludesMeetingNotesInBackup()
    {
        // Arrange
        var meeting = CreateMeeting();
        var meetingNote = CreateMeetingNote(meeting.Id);

        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting });
        _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { meetingNote });
        SetupEmptyRepositoriesExcept(nameof(IOneOnOneMeetingRepository), nameof(IMeetingNoteRepository));

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Meetings.Should().HaveCount(1);
        result.MeetingNotes.Should().HaveCount(1);
        var noteBackup = result.MeetingNotes[0];
        noteBackup.Id.Should().Be(meetingNote.Id);
        noteBackup.MeetingId.Should().Be(meetingNote.MeetingId);
        noteBackup.Content.Should().Be(meetingNote.Content);
    }

    [Fact]
    public async Task ExportAsync_WithSettings_IncludesSettingsInBackup()
    {
        // Arrange
        var settings = CreateAppSettings();
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        SetupEmptyRepositoriesExcept(nameof(IAppSettingsRepository));

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.Settings.Should().NotBeNull();
        result.Settings!.Id.Should().Be(settings.Id);
        result.Settings.StoryPointMappingsJson.Should().Be(settings.StoryPointMappings);
    }

    [Fact]
    public async Task ExportAsync_WithAllData_ExportsCompleteBackup()
    {
        // Arrange
        var directReport = CreateDirectReport();
        var project = CreateProject();
        var task = CreateTask();
        var review = CreatePerformanceReview(directReport.Id);
        var meeting = CreateMeeting(directReport.Id);
        var meetingNote = CreateMeetingNote(meeting.Id);
        var leave = CreateLeave(directReport.Id);
        var managerNote = CreateManagerNote();
        var sprint = CreateSprint();
        var sprintCapacity = CreateSprintCapacity(sprint.Id);
        var document = CreateDocument();
        var settings = CreateAppSettings();

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { directReport });
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask> { task });
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview> { review });
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting> { meeting });
        _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { meetingNote });
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { leave });
        _managerNoteRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManagerNote> { managerNote });
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { sprint });
        _sprintCapacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SprintCapacity> { sprintCapacity });
        _documentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document> { document });
        _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());
        _skillRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>());
        _skillAssessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment>());
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.ExportAsync();

        // Assert
        result.DirectReports.Should().HaveCount(1);
        result.Projects.Should().HaveCount(1);
        result.Tasks.Should().HaveCount(1);
        result.PerformanceReviews.Should().HaveCount(1);
        result.Meetings.Should().HaveCount(1);
        result.MeetingNotes.Should().HaveCount(1);
        result.Leaves.Should().HaveCount(1);
        result.ManagerNotes.Should().HaveCount(1);
        result.Sprints.Should().HaveCount(1);
        result.SprintCapacities.Should().HaveCount(1);
        result.Documents.Should().HaveCount(1);
        result.Activities.Should().BeEmpty();
        result.Skills.Should().BeEmpty();
        result.SkillAssessments.Should().BeEmpty();
        result.Settings.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportAsync_CallsAllRepositories()
    {
        // Arrange
        SetupEmptyRepositories();

        // Act
        await _service.ExportAsync();

        // Assert
        _directReportRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _projectRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _taskRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _reviewRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _meetingRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _leaveRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _managerNoteRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sprintRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sprintCapacityRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _documentRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _activityRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _skillRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _skillAssessmentRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _settingsRepositoryMock.Verify(r => r.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
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
                    Labels = "label1,label2",
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
    public async Task ImportAsync_WithSettings_UpdatesExistingSettings()
    {
        // Arrange
        var existingSettings = CreateAppSettings();
        var backup = new BackupDto
        {
            Settings = new AppSettingsBackup
            {
                Id = existingSettings.Id,
                StoryPointMappingsJson = "{\"Small\":1,\"Medium\":3,\"Large\":5}"
            }
        };

        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);
        _settingsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.SettingsRestored.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        _settingsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
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
                    Description = "Test Description",
                    Labels = ""
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
        var reviewId = Guid.NewGuid();
        var meetingId = Guid.NewGuid();
        var meetingNoteId = Guid.NewGuid();
        var leaveId = Guid.NewGuid();
        var managerNoteId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        var sprintCapacityId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

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
                    Description = "Description",
                    Labels = ""
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
            PerformanceReviews = new List<PerformanceReviewBackup>
            {
                new()
                {
                    Id = reviewId,
                    DirectReportId = directReportId,
                    ReviewPeriod = "2024-Q1",
                    ReviewDate = DateTime.UtcNow,
                    Rating = (int)PerformanceRating.MeetsExpectations
                }
            },
            Meetings = new List<OneOnOneMeetingBackup>
            {
                new()
                {
                    Id = meetingId,
                    DirectReportId = directReportId,
                    MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Agenda = "Check-in"
                }
            },
            MeetingNotes = new List<MeetingNoteBackup>
            {
                new()
                {
                    Id = meetingNoteId,
                    MeetingId = meetingId,
                    Content = "Note content",
                    Category = (int)NoteCategory.Discussion
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
                    Tags = "tag1",
                    Priority = (int)NotePriority.Normal
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
            Documents = new List<DocumentBackup>
            {
                new()
                {
                    Id = documentId,
                    Title = "Document Title",
                    Content = "Document Content",
                    Tags = "doc"
                }
            }
        };

        // Setup all repositories to return null (new entities)
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask?)null);
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReview?)null);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);
        _meetingNoteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote?)null);
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);
        _managerNoteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);
        _sprintCapacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);
        _documentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Setup Add methods
        _directReportRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport dr, CancellationToken _) => dr);
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);
        _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerformanceReview r, CancellationToken _) => r);
        _meetingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneOnOneMeeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting m, CancellationToken _) => m);
        _meetingNoteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote n, CancellationToken _) => n);
        _leaveRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Leave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave l, CancellationToken _) => l);
        _managerNoteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote n, CancellationToken _) => n);
        _sprintRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint s, CancellationToken _) => s);
        _sprintCapacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity sc, CancellationToken _) => sc);
        _documentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        // Act
        var result = await _service.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.DirectReportsRestored.Should().Be(1);
        result.ProjectsRestored.Should().Be(1);
        result.TasksRestored.Should().Be(1);
        result.PerformanceReviewsRestored.Should().Be(1);
        result.MeetingsRestored.Should().Be(1);
        result.MeetingNotesRestored.Should().Be(1);
        result.LeavesRestored.Should().Be(1);
        result.ManagerNotesRestored.Should().Be(1);
        result.SprintsRestored.Should().Be(1);
        result.SprintCapacitiesRestored.Should().Be(1);
        result.DocumentsRestored.Should().Be(1);
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyRepositories()
    {
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamTask>());
        _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());
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
        _documentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());
        _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());
        _skillRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>());
        _skillAssessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment>());
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);
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

        if (!exceptRepositories.Contains(nameof(IPerformanceReviewRepository)))
            _reviewRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PerformanceReview>());

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

        if (!exceptRepositories.Contains(nameof(IDocumentRepository)))
            _documentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Document>());

        if (!exceptRepositories.Contains(nameof(IActivityRepository)))
            _activityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Activity>());

        if (!exceptRepositories.Contains(nameof(ISkillRepository)))
            _skillRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Skill>());

        if (!exceptRepositories.Contains(nameof(ISkillAssessmentRepository)))
            _skillAssessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SkillAssessment>());

        if (!exceptRepositories.Contains(nameof(IAppSettingsRepository)))
            _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSettings?)null);

        if (!exceptRepositories.Contains(nameof(IMeetingNoteRepository)))
            _meetingNoteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MeetingNote>());
    }

    private static DirectReport CreateDirectReport() =>
        new("John", "Doe", "john.doe@test.com", "Engineer", "Engineering", DateTime.UtcNow);

    private static Project CreateProject() =>
        new("Test Project", "Description", "label1,label2", "https://github.com/test/project");

    private static TeamTask CreateTask() =>
        new("Test Task", "Description", TaskType.Task, TaskPriority.High, null, null, null, null, null, "tag1", "label1", null, null);

    private static PerformanceReview CreatePerformanceReview(Guid? directReportId = null) =>
        new(directReportId ?? Guid.NewGuid(), "2024-Q1", DateTime.UtcNow);

    private static OneOnOneMeeting CreateMeeting(Guid? directReportId = null) =>
        new(directReportId ?? Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "Check-in");

    private static MeetingNote CreateMeetingNote(Guid meetingId) =>
        new(meetingId, "Note content", NoteCategory.Discussion);

    private static Leave CreateLeave(Guid directReportId) =>
        new(directReportId, LeaveType.Vacation, DateTime.UtcNow, DateTime.UtcNow.AddDays(5), "Vacation");

    private static ManagerNote CreateManagerNote() =>
        new("Note Title", "Note Content", NotePriority.Normal, null, "tag1");

    private static Sprint CreateSprint() =>
        new("Sprint 1");

    private static SprintCapacity CreateSprintCapacity(Guid sprintId) =>
        new(sprintId, 50, 5);

    private static Document CreateDocument() =>
        new("Document Title", "Document Content", null, "tag1");

    private static AppSettings CreateAppSettings() =>
        new("[]");

    #endregion
}
