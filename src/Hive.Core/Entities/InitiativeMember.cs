namespace Hive.Core.Entities;

/// <summary>
/// Represents a team member assigned to an initiative.
/// </summary>
public class InitiativeMember
{
    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public Guid DirectReportId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InitiativeMember() { }

    public InitiativeMember(Guid initiativeId, Guid directReportId)
    {
        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        DirectReportId = directReportId;
        CreatedAt = DateTime.UtcNow;
    }
}
