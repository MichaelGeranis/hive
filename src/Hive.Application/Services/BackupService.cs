using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Backs up and restores everything the manager has in Hive: every table, every stored field.
/// Settings come along too, all but the secrets (the Claude key, the Jira token and the Google
/// Calendar address), which never leave the machine in a backup. A restore adds what is not
/// there yet and never replaces a record that is; settings are the exception, and take the
/// backup's values while keeping the secrets already entered. A backup made before format 2.0
/// is restored through the older, partial path.
/// </summary>
public class BackupService : IBackupService
{
    public const string FormatVersion = "2.0";
    public const string SettingsTable = "AppSettings";

    // Settings that are credentials: never exported, never overwritten by a restore
    private static readonly IReadOnlySet<string> SecretSettings = new HashSet<string>
    {
        nameof(AppSettings.ClaudeApiKey),
        nameof(AppSettings.JiraApiToken),
        nameof(AppSettings.GoogleCalendarUrl)
    };

    private static readonly IReadOnlySet<string> KeptOnRestore = new HashSet<string>(SecretSettings)
    {
        nameof(AppSettings.Id),
        nameof(AppSettings.CreatedAt)
    };

    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IManagerNoteRepository _managerNoteRepository;
    private readonly INoteFolderRepository _noteFolderRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintCapacityRepository _sprintCapacityRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IAutomaticBackups _automaticBackups;
    private readonly IReadOnlyList<BackupTable> _tables;

    public BackupService(
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        ITeamTaskRepository taskRepository,
        IOneOnOneMeetingRepository meetingRepository,
        ILeaveRepository leaveRepository,
        IManagerNoteRepository managerNoteRepository,
        INoteFolderRepository noteFolderRepository,
        ISprintRepository sprintRepository,
        ISprintCapacityRepository sprintCapacityRepository,
        IActivityRepository activityRepository,
        IProductRepository productRepository,
        IProjectProductRepository projectProductRepository,
        IProjectKnowledgeRepository projectKnowledgeRepository,
        IParentRepository parentRepository,
        IQuarterRepository quarterRepository,
        IQuarterMemberRepository quarterMemberRepository,
        IInitiativeRepository initiativeRepository,
        IInitiativeProductRepository initiativeProductRepository,
        IInitiativeMemberRepository initiativeMemberRepository,
        IInitiativeDependencyRepository initiativeDependencyRepository,
        IExternalDependencyRepository externalDependencyRepository,
        IAllocationRepository allocationRepository,
        ISprintGoalRepository sprintGoalRepository,
        ISkillRoadmapRepository skillRoadmapRepository,
        IRoadmapSkillRepository roadmapSkillRepository,
        ICareerPlanRepository careerPlanRepository,
        ICareerPlanSkillRepository careerPlanSkillRepository,
        ISentimentAnalysisCacheRepository sentimentAnalysisCacheRepository,
        IRetroSummaryRepository retroSummaryRepository,
        ITaskReadinessReviewRepository taskReadinessReviewRepository,
        IAppSettingsRepository appSettingsRepository,
        IAutomaticBackups automaticBackups)
    {
        _directReportRepository = directReportRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _meetingRepository = meetingRepository;
        _leaveRepository = leaveRepository;
        _managerNoteRepository = managerNoteRepository;
        _noteFolderRepository = noteFolderRepository;
        _sprintRepository = sprintRepository;
        _sprintCapacityRepository = sprintCapacityRepository;
        _activityRepository = activityRepository;
        _appSettingsRepository = appSettingsRepository;
        _automaticBackups = automaticBackups;

        // Every table, in the order a restore adds them: what is referred to before what refers to it
        _tables =
        [
            BackupTable.For<DirectReport>(directReportRepository.GetAllAsync, directReportRepository.AddAsync),
            BackupTable.For<Project>(projectRepository.GetAllAsync, projectRepository.AddAsync),
            BackupTable.For<Product>(productRepository.GetAllAsync, productRepository.AddAsync),
            BackupTable.For<ProjectProduct>(projectProductRepository.GetAllAsync, projectProductRepository.AddAsync),
            BackupTable.For<ProjectKnowledge>(projectKnowledgeRepository.GetAllAsync, projectKnowledgeRepository.AddAsync),
            BackupTable.For<Sprint>(sprintRepository.GetAllAsync, sprintRepository.AddAsync),
            BackupTable.For<SprintCapacity>(sprintCapacityRepository.GetAllAsync, sprintCapacityRepository.AddAsync),
            BackupTable.For<Parent>(parentRepository.GetAllAsync, parentRepository.AddAsync),
            BackupTable.For<TeamTask>(taskRepository.GetAllAsync, taskRepository.AddAsync),
            BackupTable.For<OneOnOneMeeting>(meetingRepository.GetAllAsync, meetingRepository.AddAsync),
            BackupTable.For<Leave>(leaveRepository.GetAllAsync, leaveRepository.AddAsync),
            BackupTable.For<NoteFolder>(noteFolderRepository.GetAllAsync, noteFolderRepository.AddAsync),
            BackupTable.For<ManagerNote>(managerNoteRepository.GetAllAsync, managerNoteRepository.AddAsync),
            BackupTable.For<Quarter>(quarterRepository.GetAllAsync, quarterRepository.AddAsync),
            BackupTable.For<QuarterMember>(quarterMemberRepository.GetAllAsync, quarterMemberRepository.AddAsync),
            BackupTable.For<Initiative>(initiativeRepository.GetAllAsync, initiativeRepository.AddAsync),
            BackupTable.For<InitiativeProduct>(initiativeProductRepository.GetAllAsync, initiativeProductRepository.AddAsync),
            BackupTable.For<InitiativeMember>(initiativeMemberRepository.GetAllAsync, initiativeMemberRepository.AddAsync),
            BackupTable.For<InitiativeDependency>(initiativeDependencyRepository.GetAllAsync, initiativeDependencyRepository.AddAsync),
            BackupTable.For<ExternalDependency>(externalDependencyRepository.GetAllAsync, externalDependencyRepository.AddAsync),
            BackupTable.For<Allocation>(allocationRepository.GetAllAsync, allocationRepository.AddAsync),
            BackupTable.For<SprintGoal>(sprintGoalRepository.GetAllAsync, sprintGoalRepository.AddAsync),
            BackupTable.For<SkillRoadmap>(skillRoadmapRepository.GetAllAsync, skillRoadmapRepository.AddAsync),
            BackupTable.For<RoadmapSkill>(roadmapSkillRepository.GetAllAsync, roadmapSkillRepository.AddAsync),
            BackupTable.For<CareerPlan>(careerPlanRepository.GetAllAsync, careerPlanRepository.AddAsync),
            BackupTable.For<CareerPlanSkill>(careerPlanSkillRepository.GetAllAsync, careerPlanSkillRepository.AddAsync),
            BackupTable.For<SentimentAnalysisCache>(sentimentAnalysisCacheRepository.GetAllAsync, sentimentAnalysisCacheRepository.AddAsync),
            BackupTable.For<RetroSummary>(retroSummaryRepository.GetAllAsync, retroSummaryRepository.AddAsync),
            BackupTable.For<TaskReadinessReview>(taskReadinessReviewRepository.GetAllAsync, taskReadinessReviewRepository.AddAsync),
            BackupTable.For<Activity>(activityRepository.GetAllAsync, activityRepository.AddAsync)
        ];
    }

    /// <summary>
    /// The names of the tables a backup carries, settings aside, in restore order.
    /// </summary>
    public IReadOnlyList<string> TableNames => _tables.Select(t => t.Name).ToList();

    public AutomaticBackupStatusDto GetAutomaticBackupStatus() => _automaticBackups.GetStatus();

    public async Task<BackupDto> ExportAsync(CancellationToken cancellationToken = default)
    {
        var tables = new Dictionary<string, List<JsonElement>>();
        foreach (var table in _tables)
        {
            tables[table.Name] = (await table.GetAllAsync(cancellationToken))
                .Select(entity => EntitySnapshot.Capture(entity))
                .ToList();
        }

        var settings = await _appSettingsRepository.GetAsync(cancellationToken);
        tables[SettingsTable] = settings is null ? [] : [EntitySnapshot.Capture(settings, SecretSettings)];

        return new BackupDto
        {
            Version = FormatVersion,
            ExportedAt = DateTime.UtcNow,
            Tables = tables
        };
    }

    public async Task<RestoreResultDto> ImportAsync(BackupDto backup, bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        if (backup.Tables.Count == 0)
        {
            return await ImportLegacyAsync(backup, cancellationToken);
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var restored = new Dictionary<string, int>();
        var skipped = new Dictionary<string, int>();

        foreach (var table in _tables)
        {
            if (!backup.Tables.TryGetValue(table.Name, out var rows) || rows.Count == 0)
                continue;

            var existingIds = (await table.GetAllAsync(cancellationToken)).Select(BackupTable.IdOf).ToHashSet();
            foreach (var row in rows)
            {
                try
                {
                    if (existingIds.Contains(EntitySnapshot.IdOf(row)))
                    {
                        skipped[table.Name] = skipped.GetValueOrDefault(table.Name) + 1;
                        continue;
                    }

                    await table.AddAsync(row, cancellationToken);
                    restored[table.Name] = restored.GetValueOrDefault(table.Name) + 1;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore a row of {table.Name}: {ex.Message}");
                }
            }

            if (skipped.TryGetValue(table.Name, out var count))
            {
                warnings.Add($"{count} {table.Name} row{(count == 1 ? "" : "s")} already existed and {(count == 1 ? "was" : "were")} kept");
            }
        }

        var settingsRestored = false;
        if (backup.Tables.TryGetValue(SettingsTable, out var settingsRows) && settingsRows.Count > 0)
        {
            try
            {
                settingsRestored = await RestoreSettingsAsync(settingsRows[0], cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to restore settings: {ex.Message}");
            }
        }

        foreach (var name in backup.Tables.Keys.Where(k => k != SettingsTable && _tables.All(t => t.Name != k)))
        {
            warnings.Add($"The backup has a table this version of Hive does not know, {name}; it was left out");
        }

        return new RestoreResultDto
        {
            Success = errors.Count == 0,
            Restored = restored,
            Skipped = skipped,
            SettingsRestored = settingsRestored,
            DirectReportsRestored = restored.GetValueOrDefault(nameof(DirectReport)),
            ProjectsRestored = restored.GetValueOrDefault(nameof(Project)),
            TasksRestored = restored.GetValueOrDefault(nameof(TeamTask)),
            MeetingsRestored = restored.GetValueOrDefault(nameof(OneOnOneMeeting)),
            LeavesRestored = restored.GetValueOrDefault(nameof(Leave)),
            ManagerNotesRestored = restored.GetValueOrDefault(nameof(ManagerNote)),
            NoteFoldersRestored = restored.GetValueOrDefault(nameof(NoteFolder)),
            SprintsRestored = restored.GetValueOrDefault(nameof(Sprint)),
            SprintCapacitiesRestored = restored.GetValueOrDefault(nameof(SprintCapacity)),
            ActivitiesRestored = restored.GetValueOrDefault(nameof(Activity)),
            Errors = errors,
            Warnings = warnings
        };
    }

    // Settings are one row: the backup's values replace the current ones, secrets aside
    private async Task<bool> RestoreSettingsAsync(JsonElement snapshot, CancellationToken cancellationToken)
    {
        var current = await _appSettingsRepository.GetAsync(cancellationToken);
        if (current is null)
        {
            await _appSettingsRepository.AddAsync(EntitySnapshot.Restore<AppSettings>(snapshot), cancellationToken);
            return true;
        }

        EntitySnapshot.Apply(current, snapshot, KeptOnRestore);
        await _appSettingsRepository.UpdateAsync(current, cancellationToken);
        return true;
    }

    /// <summary>
    /// One table: how to read every row, and how to add one from its snapshot.
    /// </summary>
    private sealed record BackupTable(
        string Name,
        Func<CancellationToken, Task<IEnumerable<object>>> GetAllAsync,
        Func<JsonElement, CancellationToken, Task> AddAsync)
    {
        public static BackupTable For<T>(
            Func<CancellationToken, Task<IReadOnlyList<T>>> getAll,
            Func<T, CancellationToken, Task> add) where T : class =>
            new(
                typeof(T).Name,
                async ct => (await getAll(ct)).Cast<object>(),
                (row, ct) => add(EntitySnapshot.Restore<T>(row), ct));

        public static Guid IdOf(object entity) =>
            (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
    }

    /// <summary>
    /// Restores a backup made before format 2.0, which carried only some tables and fields.
    /// </summary>
    private async Task<RestoreResultDto> ImportLegacyAsync(BackupDto backup, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        int directReportsRestored = 0;
        int projectsRestored = 0;
        int tasksRestored = 0;
        int meetingsRestored = 0;
        int meetingNotesRestored = 0;
        int leavesRestored = 0;
        int managerNotesRestored = 0;
        int noteFoldersRestored = 0;
        int sprintsRestored = 0;
        int sprintCapacitiesRestored = 0;
        int activitiesRestored = 0;

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
                        var entity = new Project(p.Name, p.Description, p.Url);
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
                            description: t.Description,
                            type: (TaskType)t.Type,
                            priority: (TaskPriority)t.Priority,
                            assigneeId: t.AssigneeId,
                            projectId: t.ProjectId,
                            dueDate: t.DueDate,
                            storyPoints: t.StoryPoints,
                            tags: t.Tags,
                            labels: t.Labels,
                            components: t.Components,
                            sprint: t.Sprint,
                            parentId: t.ParentId);
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

            // Import Meetings
            foreach (var m in backup.Meetings)
            {
                try
                {
                    var existing = await _meetingRepository.GetByIdAsync(m.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new OneOnOneMeeting(m.MeetingDate, m.Content, m.Tags, m.DirectReportId);
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

            // Fold notes from a pre-redesign backup into the body of their meeting,
            // so restoring an old export does not lose what was written in a 1:1.
            foreach (var group in backup.MeetingNotes.GroupBy(n => n.MeetingId))
            {
                try
                {
                    var meeting = await _meetingRepository.GetByIdAsync(group.Key, cancellationToken);
                    if (meeting is null)
                    {
                        warnings.Add($"Legacy 1:1 notes reference a meeting that is not in the backup, skipping");
                        continue;
                    }

                    var folded = FoldLegacyNotes(meeting.Content, group.OrderBy(n => n.CreatedAt));
                    meeting.UpdateContent(folded);
                    await _meetingRepository.UpdateAsync(meeting, cancellationToken);
                    meetingNotesRestored += group.Count();
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore legacy 1:1 notes: {ex.Message}");
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

            // Import Note Folders before the notes that are filed in them
            foreach (var f in backup.NoteFolders)
            {
                try
                {
                    var existing = await _noteFolderRepository.GetByIdAsync(f.Id, cancellationToken);
                    if (existing == null)
                    {
                        var entity = new NoteFolder(f.Name, f.ParentFolderId, f.SortOrder);
                        SetEntityId(entity, f.Id);
                        await _noteFolderRepository.AddAsync(entity, cancellationToken);
                        noteFoldersRestored++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to restore note folder {f.Name}: {ex.Message}");
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
                        var entity = new ManagerNote(n.Title, n.Content, n.Tags, n.FolderId);
                        SetEntityId(entity, n.Id);
                        if (n.IsPinned)
                        {
                            entity.Pin();
                        }
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
            MeetingsRestored = meetingsRestored,
            MeetingNotesRestored = meetingNotesRestored,
            LeavesRestored = leavesRestored,
            ManagerNotesRestored = managerNotesRestored,
            NoteFoldersRestored = noteFoldersRestored,
            SprintsRestored = sprintsRestored,
            SprintCapacitiesRestored = sprintCapacitiesRestored,
            ActivitiesRestored = activitiesRestored,
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

    /// <summary>
    /// Appends legacy note rows to a meeting body as plain markdown lines.
    /// </summary>
    private static string FoldLegacyNotes(string content, IEnumerable<MeetingNoteBackup> notes)
    {
        var lines = notes
            .Select(n => n.Content.Trim())
            .Where(text => text.Length > 0)
            .Select(text => $"- {text}");

        var appended = string.Join("\n", lines);
        if (appended.Length == 0)
        {
            return content;
        }

        return string.IsNullOrWhiteSpace(content) ? appended : $"{content.TrimEnd()}\n\n{appended}";
    }
}
