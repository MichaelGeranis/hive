namespace Hive.Core.Entities;

/// <summary>
/// Represents a project that groups related tasks.
/// </summary>
public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? TargetEndDate { get; private set; }
    public DateTime? ActualEndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Project() { }

    public Project(string name, string? description = null, DateTime? startDate = null, DateTime? targetEndDate = null, string? labels = null)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Status = ProjectStatus.Planning;
        StartDate = startDate;
        TargetEndDate = targetEndDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description, DateTime? startDate, DateTime? targetEndDate, string? labels = null)
    {
        ValidateName(name);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        StartDate = startDate;
        TargetEndDate = targetEndDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = ProjectStatus.Active;
        StartDate ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetToPlanning()
    {
        Status = ProjectStatus.Planning;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PutOnHold()
    {
        if (Status != ProjectStatus.Active)
        {
            throw new InvalidOperationException("Only active projects can be put on hold.");
        }

        Status = ProjectStatus.OnHold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == ProjectStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled project.");
        }

        Status = ProjectStatus.Completed;
        ActualEndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ProjectStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed project.");
        }

        Status = ProjectStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Project name cannot exceed 200 characters.", nameof(name));
        }
    }
}
