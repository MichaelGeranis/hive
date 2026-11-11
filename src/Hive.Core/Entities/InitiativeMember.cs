namespace Hive.Core.Entities;

/// <summary>
/// A person on an initiative: its lead, or a contributor.
/// </summary>
public class InitiativeMember
{
    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public Guid DirectReportId { get; private set; }
    public InitiativeRole Role { get; private set; } = InitiativeRole.Contributor;
    public DateTime CreatedAt { get; private set; }

    private InitiativeMember() { }

    public InitiativeMember(Guid initiativeId, Guid directReportId, InitiativeRole role = InitiativeRole.Contributor)
    {
        ValidateRole(role);

        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        DirectReportId = directReportId;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsLead => Role == InitiativeRole.Lead;

    public void SetRole(InitiativeRole role)
    {
        ValidateRole(role);
        Role = role;
    }

    private static void ValidateRole(InitiativeRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException("A member is a lead or a contributor.", nameof(role));
        }
    }
}
