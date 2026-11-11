using System.Text.Json;

namespace Hive.Application.DTOs;

/// <summary>
/// A backup of everything in Hive. From format 2.0 it is <see cref="Tables"/>: every table by
/// entity name, each row every stored field of one record, and settings without their
/// secrets. The typed lists below are format 1.0, which carried only some tables and fields;
/// they are read on restore so an older backup still restores, and never written.
/// </summary>
public record BackupDto
{
    public string Version { get; init; } = "1.0";
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Every table, by entity name (e.g. <c>TeamTask</c>, <c>AppSettings</c>), in restore order.
    /// </summary>
    public Dictionary<string, List<JsonElement>> Tables { get; init; } = new();

    public List<DirectReportBackup> DirectReports { get; init; } = new();
    public List<ProjectBackup> Projects { get; init; } = new();
    public List<TeamTaskBackup> Tasks { get; init; } = new();
    public List<OneOnOneMeetingBackup> Meetings { get; init; } = new();
    /// <summary>Legacy 1:1 notes, read on restore only. Never written by an export.</summary>
    public List<MeetingNoteBackup> MeetingNotes { get; init; } = new();
    public List<LeaveBackup> Leaves { get; init; } = new();
    public List<ManagerNoteBackup> ManagerNotes { get; init; } = new();
    public List<NoteFolderBackup> NoteFolders { get; init; } = new();
    public List<SprintBackup> Sprints { get; init; } = new();
    public List<SprintCapacityBackup> SprintCapacities { get; init; } = new();
    public List<ActivityBackup> Activities { get; init; } = new();
}

public record DirectReportBackup
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime HireDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record ProjectBackup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record TeamTaskBackup
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Type { get; init; }
    public int Priority { get; init; }
    public int Status { get; init; }
    public Guid? AssigneeId { get; init; }
    public Guid? ProjectId { get; init; }
    public DateTime? DueDate { get; init; }
    public int? StoryPoints { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string? Components { get; init; }
    public string Sprint { get; init; } = string.Empty;
    public int? PreviousSprintsStoryPoints { get; init; }
    public Guid? ParentId { get; init; }
    public string OverriddenFields { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record OneOnOneMeetingBackup
{
    public Guid Id { get; init; }
    public Guid? DirectReportId { get; init; }
    public DateOnly MeetingDate { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// A note taken inside a 1:1, from a backup written before 1:1s became single markdown
/// notes. Only read on restore: its content is folded into its meeting's body.
/// </summary>
public record MeetingNoteBackup
{
    public Guid Id { get; init; }
    public Guid MeetingId { get; init; }
    public string Content { get; init; } = string.Empty;
    public int Category { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record LeaveBackup
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public int Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record ManagerNoteBackup
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public Guid? FolderId { get; init; }
    public bool IsPinned { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record NoteFolderBackup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record SprintBackup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record SprintCapacityBackup
{
    public Guid Id { get; init; }
    public Guid SprintId { get; init; }
    public int TotalCapacityPoints { get; init; }
    public int AvailableMembers { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record ActivityBackup
{
    public Guid Id { get; init; }
    public int ActivityType { get; init; }
    public int EntityType { get; init; }
    public Guid EntityId { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// The automatic backups of the database file: taken at startup and every hour.
/// </summary>
public record AutomaticBackupStatusDto
{
    /// <summary>False with the in-memory database, which has no file to back up.</summary>
    public bool Enabled { get; init; }
    public string? Folder { get; init; }
    public int Count { get; init; }

    /// <summary>When the newest backup was taken, in UTC; null before the first.</summary>
    public DateTime? LastBackupAt { get; init; }
    public int IntervalHours { get; init; }
}

/// <summary>
/// Result of a restore operation.
/// </summary>
public record RestoreResultDto
{
    public bool Success { get; init; }
    public int DirectReportsRestored { get; init; }
    public int ProjectsRestored { get; init; }
    public int TasksRestored { get; init; }
    public int MeetingsRestored { get; init; }
    public int MeetingNotesRestored { get; init; }
    public int LeavesRestored { get; init; }
    public int ManagerNotesRestored { get; init; }
    public int NoteFoldersRestored { get; init; }
    public int SprintsRestored { get; init; }
    public int SprintCapacitiesRestored { get; init; }
    public int ActivitiesRestored { get; init; }

    /// <summary>
    /// Rows added, by table. Tables with none are left out.
    /// </summary>
    public Dictionary<string, int> Restored { get; init; } = new();

    /// <summary>
    /// Rows already in Hive and kept as they were, by table.
    /// </summary>
    public Dictionary<string, int> Skipped { get; init; } = new();

    /// <summary>
    /// Whether settings were taken from the backup (secrets are never in one).
    /// </summary>
    public bool SettingsRestored { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}
