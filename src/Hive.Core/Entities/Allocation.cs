namespace Hive.Core.Entities;

/// <summary>
/// Represents an allocation of a team member to an initiative in a specific sprint.
/// </summary>
public class Allocation
{
    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public Guid DirectReportId { get; private set; }
    public Guid SprintId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Allocation() { }

    public Allocation(
        Guid initiativeId,
        Guid directReportId,
        Guid sprintId)
    {
        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        DirectReportId = directReportId;
        SprintId = sprintId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
