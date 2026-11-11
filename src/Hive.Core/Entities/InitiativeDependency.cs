namespace Hive.Core.Entities;

/// <summary>
/// Represents a dependency relationship between two initiatives.
/// </summary>
public class InitiativeDependency
{
    public Guid Id { get; private set; }
    public Guid DependentInitiativeId { get; private set; }
    public Guid DependencyInitiativeId { get; private set; }
    public DependencyType Type { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private InitiativeDependency() { }

    public InitiativeDependency(
        Guid dependentInitiativeId,
        Guid dependencyInitiativeId,
        DependencyType type = DependencyType.FinishToStart,
        string? notes = null)
    {
        if (dependentInitiativeId == dependencyInitiativeId)
        {
            throw new ArgumentException("An initiative cannot depend on itself.", nameof(dependentInitiativeId));
        }

        Id = Guid.NewGuid();
        DependentInitiativeId = dependentInitiativeId;
        DependencyInitiativeId = dependencyInitiativeId;
        Type = type;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }
}
