namespace Hive.Core.Entities;

/// <summary>
/// Represents a team member's contribution points for a specific project.
/// Manual points are stored here; automatic points are calculated from completed tasks.
/// </summary>
public class KnowledgePoint
{
    /// <summary>
    /// Unique identifier for the knowledge point record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The team member whose points are being tracked.
    /// </summary>
    public Guid DirectReportId { get; private set; }

    /// <summary>
    /// The project the points are associated with.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Manually assigned points by the manager.
    /// </summary>
    public int ManualPoints { get; private set; }

    /// <summary>
    /// Optional notes explaining why points were awarded.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// When the record was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private KnowledgePoint() { }

    /// <summary>
    /// Creates a new knowledge point record.
    /// </summary>
    public KnowledgePoint(Guid directReportId, Guid projectId, int manualPoints = 0, string? notes = null)
    {
        ValidateDirectReportId(directReportId);
        ValidateProjectId(projectId);
        ValidateManualPoints(manualPoints);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        ProjectId = projectId;
        ManualPoints = manualPoints;
        Notes = notes?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds points to the current manual points total.
    /// </summary>
    public void AddPoints(int points, string? notes = null)
    {
        if (points < 0)
        {
            throw new ArgumentException("Points to add cannot be negative.", nameof(points));
        }

        ManualPoints += points;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? notes.Trim()
                : $"{Notes}\n{notes.Trim()}";
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the manual points to a specific value.
    /// </summary>
    public void UpdateManualPoints(int manualPoints, string? notes = null)
    {
        ValidateManualPoints(manualPoints);

        ManualPoints = manualPoints;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateDirectReportId(Guid directReportId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }
    }

    private static void ValidateProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
        }
    }

    private static void ValidateManualPoints(int manualPoints)
    {
        if (manualPoints < 0)
        {
            throw new ArgumentException("Manual points cannot be negative.", nameof(manualPoints));
        }
    }
}
