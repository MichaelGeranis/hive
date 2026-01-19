using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for backup and restore operations.
/// </summary>
public class BackupService : IBackupService
{
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IPerformanceReviewRepository _reviewRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IMeetingNoteRepository _meetingNoteRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IManagerNoteRepository _managerNoteRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintCapacityRepository _sprintCapacityRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillAssessmentRepository _skillAssessmentRepository;
    private readonly IAppSettingsRepository _settingsRepository;

    public BackupService(
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        ITeamTaskRepository taskRepository,
        IPerformanceReviewRepository reviewRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository meetingNoteRepository,
        ILeaveRepository leaveRepository,
        IManagerNoteRepository managerNoteRepository,
        ISprintRepository sprintRepository,
        ISprintCapacityRepository sprintCapacityRepository,
        IDocumentRepository documentRepository,
        IActivityRepository activityRepository,
        ISkillRepository skillRepository,
        ISkillAssessmentRepository skillAssessmentRepository,
        IAppSettingsRepository settingsRepository)
    {
        _directReportRepository = directReportRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _reviewRepository = reviewRepository;
        _meetingRepository = meetingRepository;
        _meetingNoteRepository = meetingNoteRepository;
        _leaveRepository = leaveRepository;
        _managerNoteRepository = managerNoteRepository;
        _sprintRepository = sprintRepository;
        _sprintCapacityRepository = sprintCapacityRepository;
        _documentRepository = documentRepository;
        _activityRepository = activityRepository;
        _skillRepository = skillRepository;
        _skillAssessmentRepository = skillAssessmentRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<BackupDto> ExportAsync(CancellationToken cancellationToken = default)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var reviews = await _reviewRepository.GetAllAsync(cancellationToken);
        var meetings = await _meetingRepository.GetAllAsync(cancellationToken);
        var meetingNotes = new List<MeetingNote>();
        foreach (var meeting in meetings)
        {
            var notes = await _meetingNoteRepository.GetByMeetingIdAsync(meeting.Id, true, cancellationToken);
            meetingNotes.AddRange(notes);
        }
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var managerNotes = await _managerNoteRepository.GetAllAsync(cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprintCapacities = await _sprintCapacityRepository.GetAllAsync(cancellationToken);
        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        var activities = await _activityRepository.GetAllAsync(cancellationToken);
        var skills = await _skillRepository.GetAllAsync(true, cancellationToken);
        var skillAssessments = await _skillAssessmentRepository.GetAllAsync(cancellationToken);
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        return new BackupDto
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            DirectReports = directReports.Select(MapDirectReport).ToList(),
            Projects = projects.Select(MapProject).ToList(),
            Tasks = tasks.Select(MapTask).ToList(),
            PerformanceReviews = reviews.Select(MapReview).ToList(),
            Meetings = meetings.Select(MapMeeting).ToList(),
            MeetingNotes = meetingNotes.Select(MapMeetingNote).ToList(),
            Leaves = leaves.Select(MapLeave).ToList(),
            ManagerNotes = managerNotes.Select(MapManagerNote).ToList(),
            Sprints = sprints.Select(MapSprint).ToList(),
            SprintCapacities = sprintCapacities.Select(MapSprintCapacity).ToList(),
            Documents = documents.Select(MapDocument).ToList(),
            Activities = activities.Select(MapActivity).ToList(),
            Skills = skills.Select(MapSkill).ToList(),
            SkillAssessments = skillAssessments.Select(MapSkillAssessment).ToList(),
            Settings = settings != null ? MapSettings(settings) : null
        };
    }

    public async Task<RestoreResultDto> ImportAsync(BackupDto backup, bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        int directReportsRestored = 0;
        int projectsRestored = 0;
        int tasksRestored = 0;
        int reviewsRestored = 0;
        int meetingsRestored = 0;
        int meetingNotesRestored = 0;
        int leavesRestored = 0;
        int managerNotesRestored = 0;
        int sprintsRestored = 0;
        int sprintCapacitiesRestored = 0;
        int documentsRestored = 0;
        int activitiesRestored = 0;
        int skillsRestored = 0;
        int skillAssessmentsRestored = 0;
        bool settingsRestored = false;

        try
        {
            // Import Direct Reports first (other entities reference them)
            foreach (var dr in backup.DirectReports)
            {
                try
                {
                    var existing = await _directReportRepository.GetByIdAsync(dr.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new DirectReport(dr.FirstName, dr.LastName, dr.Email, dr.JobTitle, dr.Department, dr.HireDate);
                        SetEntityId(entity, dr.Id);
                        await _directReportRepository.AddAsync(entity, cancellationToken);
                        directReportsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Direct report {dr.Email} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore direct report {dr.Email}: {ex.Message}");
                }
            }

            // Import Projects
            foreach (var p in backup.Projects)
            {
                try
                {
                    var existing = await _projectRepository.GetByIdAsync(p.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Project(p.Name, p.Description, p.Labels, p.Url);
                        SetEntityId(entity, p.Id);
                        await _projectRepository.AddAsync(entity, cancellationToken);
                        projectsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Project {p.Name} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore project {p.Name}: {ex.Message}");
                }
            }

            // Import Sprints
            foreach (var s in backup.Sprints)
            {
                try
                {
                    var existing = await _sprintRepository.GetByIdAsync(s.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Sprint(s.Name);
                        SetEntityId(entity, s.Id);
                        await _sprintRepository.AddAsync(entity, cancellationToken);
                        sprintsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Sprint {s.Name} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore sprint {s.Name}: {ex.Message}");
                }
            }

            // Import Tasks
            foreach (var t in backup.Tasks)
            {
                try
                {
                    var existing = await _taskRepository.GetByIdAsync(t.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new TeamTask(
                            t.Title, 
                            t.Description, 
                            (TaskType)t.Type, 
                            (TaskPriority)t.Priority,
                            t.AssigneeId,
                            t.ProjectId,
                            t.DueDate,
                            t.EstimatedHours.HasValue ? (int?)t.EstimatedHours.Value : null,
                            t.StoryPoints,
                            t.Tags,
                            t.Labels,
                            t.Sprint,
                            t.TimeSpentMinutes);
                        SetEntityId(entity, t.Id);
                        await _taskRepository.AddAsync(entity, cancellationToken);
                        tasksRestored++;
                    }
                    else
                    {
                        warnings.Add($"Task {t.Title} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore task {t.Title}: {ex.Message}");
                }
            }

            // Import Performance Reviews
            foreach (var r in backup.PerformanceReviews)
            {
                try
                {
                    var existing = await _reviewRepository.GetByIdAsync(r.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new PerformanceReview(r.DirectReportId, r.ReviewPeriod, r.ReviewDate);
                        SetEntityId(entity, r.Id);
                        entity.UpdateContent(r.Strengths, r.AreasForImprovement, r.ManagerNotes, (PerformanceRating)r.Rating);
                        await _reviewRepository.AddAsync(entity, cancellationToken);
                        reviewsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Review {r.ReviewPeriod} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore review: {ex.Message}");
                }
            }

            // Import Meetings
            foreach (var m in backup.Meetings)
            {
                try
                {
                    var existing = await _meetingRepository.GetByIdAsync(m.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new OneOnOneMeeting(m.DirectReportId, m.MeetingDate, m.DurationMinutes, m.Location, m.Agenda);
                        SetEntityId(entity, m.Id);
                        await _meetingRepository.AddAsync(entity, cancellationToken);
                        meetingsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Meeting on {m.MeetingDate} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore meeting: {ex.Message}");
                }
            }

            // Import Meeting Notes
            foreach (var n in backup.MeetingNotes)
            {
                try
                {
                    var existing = await _meetingNoteRepository.GetByIdAsync(n.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new MeetingNote(n.MeetingId, n.Content, (NoteCategory)n.Category, n.IsPrivate);
                        SetEntityId(entity, n.Id);
                        await _meetingNoteRepository.AddAsync(entity, cancellationToken);
                        meetingNotesRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore meeting note: {ex.Message}");
                }
            }

            // Import Leaves
            foreach (var l in backup.Leaves)
            {
                try
                {
                    var existing = await _leaveRepository.GetByIdAsync(l.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Leave(l.DirectReportId, (LeaveType)l.Type, l.StartDate, l.EndDate, l.Notes);
                        SetEntityId(entity, l.Id);
                        await _leaveRepository.AddAsync(entity, cancellationToken);
                        leavesRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore leave: {ex.Message}");
                }
            }

            // Import Manager Notes
            foreach (var n in backup.ManagerNotes)
            {
                try
                {
                    var existing = await _managerNoteRepository.GetByIdAsync(n.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new ManagerNote(n.Title, n.Content, (NotePriority)n.Priority, n.DueDate, n.Tags);
                        SetEntityId(entity, n.Id);
                        await _managerNoteRepository.AddAsync(entity, cancellationToken);
                        managerNotesRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore manager note {n.Title}: {ex.Message}");
                }
            }

            // Import Sprint Capacities
            foreach (var sc in backup.SprintCapacities)
            {
                try
                {
                    var existing = await _sprintCapacityRepository.GetByIdAsync(sc.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new SprintCapacity(sc.SprintId, sc.TotalCapacityPoints, sc.AvailableMembers);
                        SetEntityId(entity, sc.Id);
                        await _sprintCapacityRepository.AddAsync(entity, cancellationToken);
                        sprintCapacitiesRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore sprint capacity: {ex.Message}");
                }
            }

            // Import Documents
            foreach (var d in backup.Documents)
            {
                try
                {
                    var existing = await _documentRepository.GetByIdAsync(d.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Document(d.Title, d.Content, d.Url, d.Tags);
                        SetEntityId(entity, d.Id);
                        await _documentRepository.AddAsync(entity, cancellationToken);
                        documentsRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore document {d.Title}: {ex.Message}");
                }
            }

            // Import Skills
            foreach (var s in backup.Skills)
            {
                try
                {
                    var existing = await _skillRepository.GetByIdAsync(s.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Skill(s.Name, s.Description, (SkillCategory)s.Category);
                        SetEntityId(entity, s.Id);
                        if (!s.IsActive)
                        {
                            entity.Deactivate();
                        }
                        await _skillRepository.AddAsync(entity, cancellationToken);
                        skillsRestored++;
                    }
                    else
                    {
                        warnings.Add($"Skill {s.Name} already exists, skipping");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore skill {s.Name}: {ex.Message}");
                }
            }

            // Import Skill Assessments
            foreach (var sa in backup.SkillAssessments)
            {
                try
                {
                    var existing = await _skillAssessmentRepository.GetByIdAsync(sa.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new SkillAssessment(
                            sa.DirectReportId,
                            sa.SkillId,
                            (ProficiencyLevel)sa.Level,
                            sa.TargetLevel.HasValue ? (ProficiencyLevel?)sa.TargetLevel.Value : null,
                            sa.Notes);
                        SetEntityId(entity, sa.Id);
                        await _skillAssessmentRepository.AddAsync(entity, cancellationToken);
                        skillAssessmentsRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore skill assessment: {ex.Message}");
                }
            }

            // Import Activities (append-only logs, should be last)
            foreach (var a in backup.Activities)
            {
                try
                {
                    var existing = await _activityRepository.GetByIdAsync(a.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new Activity(
                            (ActivityType)a.ActivityType,
                            (EntityType)a.EntityType,
                            a.EntityId,
                            a.EntityName,
                            a.Description,
                            a.Timestamp);
                        SetEntityId(entity, a.Id);
                        await _activityRepository.AddAsync(entity, cancellationToken);
                        activitiesRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore activity: {ex.Message}");
                }
            }

            // Settings are typically singleton, update if exists
            if (backup.Settings != null)
            {
                try
                {
                    var existingSettings = await _settingsRepository.GetAsync(cancellationToken);
                    if (existingSettings != null)
                    {
                        existingSettings.UpdateStoryPointMappings(backup.Settings.StoryPointMappingsJson);
                        await _settingsRepository.UpdateAsync(existingSettings, cancellationToken);
                        settingsRestored = true;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore settings: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Critical error during restore: {ex.Message}");
        }

        return new RestoreResultDto
        {
            Success = errors.Count == 0,
            DirectReportsRestored = directReportsRestored,
            ProjectsRestored = projectsRestored,
            TasksRestored = tasksRestored,
            PerformanceReviewsRestored = reviewsRestored,
            MeetingsRestored = meetingsRestored,
            MeetingNotesRestored = meetingNotesRestored,
            LeavesRestored = leavesRestored,
            ManagerNotesRestored = managerNotesRestored,
            SprintsRestored = sprintsRestored,
            SprintCapacitiesRestored = sprintCapacitiesRestored,
            DocumentsRestored = documentsRestored,
            ActivitiesRestored = activitiesRestored,
            SkillsRestored = skillsRestored,
            SkillAssessmentsRestored = skillAssessmentsRestored,
            SettingsRestored = settingsRestored,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void SetEntityId(object entity, Guid id)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(entity, id);
        }
        else
        {
            // Try setting via backing field
            var field = entity.GetType().GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(entity, id);
        }
    }

    #region Mapping Methods

    private static DirectReportBackup MapDirectReport(DirectReport dr) => new()
    {
        Id = dr.Id,
        FirstName = dr.FirstName,
        LastName = dr.LastName,
        Email = dr.Email,
        JobTitle = dr.JobTitle,
        Department = dr.Department,
        HireDate = dr.HireDate,
        CreatedAt = dr.CreatedAt,
        UpdatedAt = dr.UpdatedAt
    };

    private static ProjectBackup MapProject(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Labels = p.Labels,
        Url = p.Url,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static TeamTaskBackup MapTask(TeamTask t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Type = (int)t.Type,
        Priority = (int)t.Priority,
        Status = (int)t.Status,
        AssigneeId = t.AssigneeId,
        ProjectId = t.ProjectId,
        DueDate = t.DueDate,
        EstimatedHours = t.EstimatedHours,
        StoryPoints = t.StoryPoints,
        Tags = t.Tags,
        Labels = t.Labels,
        Sprint = t.Sprint,
        TimeSpentMinutes = t.TimeSpentMinutes,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static PerformanceReviewBackup MapReview(PerformanceReview r) => new()
    {
        Id = r.Id,
        DirectReportId = r.DirectReportId,
        ReviewPeriod = r.ReviewPeriod,
        ReviewDate = r.ReviewDate,
        Rating = (int)r.Rating,
        Strengths = r.Strengths,
        AreasForImprovement = r.AreasForImprovement,
        ManagerNotes = r.ManagerNotes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static OneOnOneMeetingBackup MapMeeting(OneOnOneMeeting m) => new()
    {
        Id = m.Id,
        DirectReportId = m.DirectReportId,
        MeetingDate = m.MeetingDate,
        DurationMinutes = m.DurationMinutes,
        Location = m.Location,
        Agenda = m.Agenda,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    private static MeetingNoteBackup MapMeetingNote(MeetingNote n) => new()
    {
        Id = n.Id,
        MeetingId = n.MeetingId,
        Content = n.Content,
        Category = (int)n.Category,
        IsPrivate = n.IsPrivate,
        ActionStatus = n.ActionStatus.HasValue ? (int?)n.ActionStatus.Value : null,
        ActionDueDate = n.ActionDueDate,
        ActionAssignee = n.ActionAssignee,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt
    };

    private static LeaveBackup MapLeave(Leave l) => new()
    {
        Id = l.Id,
        DirectReportId = l.DirectReportId,
        Type = (int)l.Type,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        Notes = l.Notes ?? string.Empty,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    private static ManagerNoteBackup MapManagerNote(ManagerNote n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Content = n.Content,
        Tags = n.Tags,
        Priority = (int)n.Priority,
        IsCompleted = n.IsCompleted,
        DueDate = n.DueDate,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt,
        CompletedAt = n.CompletedAt
    };

    private static SprintBackup MapSprint(Sprint s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static SprintCapacityBackup MapSprintCapacity(SprintCapacity sc) => new()
    {
        Id = sc.Id,
        SprintId = sc.SprintId,
        TotalCapacityPoints = sc.TotalCapacityPoints,
        AvailableMembers = sc.AvailableMembers,
        CreatedAt = sc.CreatedAt,
        UpdatedAt = sc.UpdatedAt
    };

    private static DocumentBackup MapDocument(Document d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Content = d.Content,
        Url = d.Url,
        Tags = d.Tags,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    private static AppSettingsBackup MapSettings(AppSettings s) => new()
    {
        Id = s.Id,
        StoryPointMappingsJson = s.StoryPointMappings,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static ActivityBackup MapActivity(Activity a) => new()
    {
        Id = a.Id,
        ActivityType = (int)a.ActivityType,
        EntityType = (int)a.EntityType,
        EntityId = a.EntityId,
        EntityName = a.EntityName,
        Description = a.Description,
        Timestamp = a.Timestamp,
        CreatedAt = a.CreatedAt
    };

    private static SkillBackup MapSkill(Skill s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        Category = (int)s.Category,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static SkillAssessmentBackup MapSkillAssessment(SkillAssessment sa) => new()
    {
        Id = sa.Id,
        DirectReportId = sa.DirectReportId,
        SkillId = sa.SkillId,
        Level = (int)sa.Level,
        TargetLevel = sa.TargetLevel.HasValue ? (int?)sa.TargetLevel.Value : null,
        Notes = sa.Notes,
        UpdatedAt = sa.UpdatedAt
    };

    #endregion
}
