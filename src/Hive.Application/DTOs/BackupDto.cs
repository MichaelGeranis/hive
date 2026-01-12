namespace Hive.Application.DTOs;

/// <summary>
/// Complete data backup containing all application data.
/// </summary>
public record BackupDto
{
    public string Version { get; init; } = "1.0";
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
    public List<DirectReportBackup> DirectReports { get; init; } = new();
    public List<ProjectBackup> Projects { get; init; } = new();
    public List<TeamTaskBackup> Tasks { get; init; } = new();
    public List<PerformanceReviewBackup> PerformanceReviews { get; init; } = new();
    public List<OneOnOneMeetingBackup> Meetings { get; init; } = new();
    public List<MeetingNoteBackup> MeetingNotes { get; init; } = new();
    public List<LeaveBackup> Leaves { get; init; } = new();
    public List<ManagerNoteBackup> ManagerNotes { get; init; } = new();
    public List<SprintBackup> Sprints { get; init; } = new();
    public List<SprintCapacityBackup> SprintCapacities { get; init; } = new();
    public List<DocumentBackup> Documents { get; init; } = new();
    public List<ActivityBackup> Activities { get; init; } = new();
    public List<SkillBackup> Skills { get; init; } = new();
    public List<SkillAssessmentBackup> SkillAssessments { get; init; } = new();
    public AppSettingsBackup? Settings { get; init; }
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
    public string Labels { get; init; } = string.Empty;
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
    public decimal? EstimatedHours { get; init; }
    public int? StoryPoints { get; init; }
    public decimal? ActualHours { get; init; }
    public string Tags { get; init; } = string.Empty;
    public string Labels { get; init; } = string.Empty;
    public string Sprint { get; init; } = string.Empty;
    public int? TimeSpentMinutes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record PerformanceReviewBackup
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string ReviewPeriod { get; init; } = string.Empty;
    public DateTime ReviewDate { get; init; }
    public int Rating { get; init; }
    public int Status { get; init; }
    public string Strengths { get; init; } = string.Empty;
    public string AreasForImprovement { get; init; } = string.Empty;
    public string GoalsForNextPeriod { get; init; } = string.Empty;
    public string ManagerNotes { get; init; } = string.Empty;
    public string EmployeeSelfAssessment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record OneOnOneMeetingBackup
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public DateTime MeetingDate { get; init; }
    public int DurationMinutes { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Agenda { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record MeetingNoteBackup
{
    public Guid Id { get; init; }
    public Guid MeetingId { get; init; }
    public string Content { get; init; } = string.Empty;
    public int Category { get; init; }
    public bool IsPrivate { get; init; }
    public int? ActionStatus { get; init; }
    public DateTime? ActionDueDate { get; init; }
    public string? ActionAssignee { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
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
    public int Priority { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
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

public record DocumentBackup
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string Tags { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record AppSettingsBackup
{
    public Guid Id { get; init; }
    public string StoryPointMappingsJson { get; init; } = string.Empty;
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

public record SkillBackup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record SkillAssessmentBackup
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public Guid SkillId { get; init; }
    public int Level { get; init; }
    public int? TargetLevel { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
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
    public int PerformanceReviewsRestored { get; init; }
    public int MeetingsRestored { get; init; }
    public int MeetingNotesRestored { get; init; }
    public int LeavesRestored { get; init; }
    public int ManagerNotesRestored { get; init; }
    public int SprintsRestored { get; init; }
    public int SprintCapacitiesRestored { get; init; }
    public int DocumentsRestored { get; init; }
    public int ActivitiesRestored { get; init; }
    public int SkillsRestored { get; init; }
    public int SkillAssessmentsRestored { get; init; }
    public bool SettingsRestored { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}
