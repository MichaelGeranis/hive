namespace Hive.Core.Entities;

/// <summary>
/// Represents the goal for a specific sprint within a quarter.
/// </summary>
public class SprintGoal
{
    public Guid Id { get; private set; }
    public Guid QuarterId { get; private set; }
    public Guid SprintId { get; private set; }
    public string Goal { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SprintGoal() { }

    public SprintGoal(Guid quarterId, Guid sprintId, string? goal = null, string? notes = null)
    {
        ValidateGoal(goal);

        Id = Guid.NewGuid();
        QuarterId = quarterId;
        SprintId = sprintId;
        Goal = goal?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? goal, string? notes)
    {
        ValidateGoal(goal);

        Goal = goal?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateGoal(string? goal)
    {
        if (goal is not null && goal.Length > 4000)
        {
            throw new ArgumentException("Goal cannot exceed 4000 characters.", nameof(goal));
        }
    }
}
