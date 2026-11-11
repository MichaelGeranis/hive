namespace Hive.Core.Entities;

/// <summary>
/// What an initiative needs from another team (CORE, CS, Research…), and where that stands.
/// </summary>
public class ExternalDependency
{
    public const int MaxTeamLength = 100;
    public const int MaxNeedLength = 500;

    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public string Team { get; private set; } = string.Empty;
    public string Need { get; private set; } = string.Empty;
    public ExternalDependencyStatus Status { get; private set; } = ExternalDependencyStatus.NeedsCoordination;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ExternalDependency() { }

    public ExternalDependency(
        Guid initiativeId,
        string team,
        string? need = null,
        ExternalDependencyStatus status = ExternalDependencyStatus.NeedsCoordination)
    {
        Validate(team, need, status);

        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        Team = team.Trim();
        Need = need?.Trim() ?? string.Empty;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string team, string? need, ExternalDependencyStatus status)
    {
        Validate(team, need, status);

        Team = team.Trim();
        Need = need?.Trim() ?? string.Empty;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Still open: not yet agreed with the other team, or at risk.</summary>
    public bool IsOpen => Status is ExternalDependencyStatus.NeedsCoordination or ExternalDependencyStatus.AtRisk;

    private static void Validate(string team, string? need, ExternalDependencyStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Unknown dependency status.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(team))
        {
            throw new ArgumentException("A dependency names the team it is on.", nameof(team));
        }

        if (team.Trim().Length > MaxTeamLength)
        {
            throw new ArgumentException($"A team name cannot exceed {MaxTeamLength} characters.", nameof(team));
        }

        if (need is not null && need.Trim().Length > MaxNeedLength)
        {
            throw new ArgumentException($"What is needed cannot exceed {MaxNeedLength} characters.", nameof(need));
        }
    }
}
