namespace Hive.Core.Entities;

/// <summary>
/// Represents a task or work item that can be assigned to team members.
/// </summary>
public class TeamTask
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskType Type { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? EstimatedHours { get; private set; }
    public int? StoryPoints { get; private set; }
    public int? ActualHours { get; private set; }
    public string Tags { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public string Sprint { get; private set; } = string.Empty;
    public int? TimeSpentMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private TeamTask() { }

    public TeamTask(
        string title,
        string? description = null,
        TaskType type = TaskType.Task,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        Guid? projectId = null,
        DateTime? dueDate = null,
        int? estimatedHours = null,
        int? storyPoints = null,
        string? tags = null,
        string? labels = null,
        string? sprint = null,
        int? timeSpentMinutes = null)
    {
        ValidateTitle(title);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Priority = priority;
        Status = TaskStatus.Backlog;
        AssigneeId = assigneeId;
        ProjectId = projectId;
        DueDate = dueDate;
        EstimatedHours = estimatedHours;
        StoryPoints = storyPoints;
        Tags = tags?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Sprint = sprint?.Trim() ?? string.Empty;
        TimeSpentMinutes = timeSpentMinutes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string? description,
        TaskType type,
        TaskPriority priority,
        DateTime? dueDate,
        int? estimatedHours,
        int? storyPoints,
        string? tags,
        string? labels = null,
        string? sprint = null,
        int? timeSpentMinutes = null)
    {
        ValidateTitle(title);

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Priority = priority;
        DueDate = dueDate;
        EstimatedHours = estimatedHours;
        StoryPoints = storyPoints;
        Tags = tags?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Sprint = sprint?.Trim() ?? string.Empty;
        TimeSpentMinutes = timeSpentMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToProject(Guid? projectId)
    {
        ProjectId = projectId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToBacklog()
    {
        Status = TaskStatus.Backlog;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToTodo()
    {
        Status = TaskStatus.Todo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status == TaskStatus.Done || Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot start a completed or cancelled task.");
        }

        Status = TaskStatus.InProgress;
        StartedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToReview()
    {
        if (Status != TaskStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress tasks can be moved to review.");
        }

        Status = TaskStatus.InReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(int? actualHours = null)
    {
        if (Status == TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled task.");
        }

        Status = TaskStatus.Done;
        CompletedAt = DateTime.UtcNow;
        if (actualHours.HasValue)
        {
            ActualHours = actualHours;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == TaskStatus.Done)
        {
            throw new InvalidOperationException("Cannot cancel a completed task.");
        }

        Status = TaskStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        if (Status != TaskStatus.Done && Status != TaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Only completed or cancelled tasks can be reopened.");
        }

        Status = TaskStatus.Todo;
        CompletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LogHours(int hours)
    {
        if (hours < 0)
        {
            throw new ArgumentException("Hours cannot be negative.", nameof(hours));
        }

        ActualHours = (ActualHours ?? 0) + hours;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue()
    {
        if (!DueDate.HasValue)
            return false;

        return Status != TaskStatus.Done
               && Status != TaskStatus.Cancelled
               && DueDate.Value < DateTime.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title cannot be empty.", nameof(title));
        }

        if (title.Length > 500)
        {
            throw new ArgumentException("Task title cannot exceed 500 characters.", nameof(title));
        }
    }
}
