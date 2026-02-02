namespace Hive.Core.Entities;

/// <summary>
/// Tracks capacity per sprint in story points.
/// Capacity represents the total story points the team can deliver in a sprint
/// based on available team members.
/// </summary>
public class SprintCapacity
{
    public Guid Id { get; private set; }
    public Guid SprintId { get; private set; }
    public int TotalCapacityPoints { get; private set; }
    public int AvailableMembers { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SprintCapacity() { }

    public SprintCapacity(Guid sprintId, int totalCapacityPoints, int availableMembers)
    {
        ValidateCapacity(totalCapacityPoints, nameof(totalCapacityPoints));
        ValidateMembers(availableMembers, nameof(availableMembers));

        Id = Guid.NewGuid();
        SprintId = sprintId;
        TotalCapacityPoints = totalCapacityPoints;
        AvailableMembers = availableMembers;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int totalCapacityPoints, int availableMembers)
    {
        ValidateCapacity(totalCapacityPoints, nameof(totalCapacityPoints));
        ValidateMembers(availableMembers, nameof(availableMembers));

        TotalCapacityPoints = totalCapacityPoints;
        AvailableMembers = availableMembers;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvailableMembers(int availableMembers)
    {
        ValidateMembers(availableMembers, nameof(availableMembers));
        AvailableMembers = availableMembers;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the average capacity per team member.
    /// </summary>
    public double GetCapacityPerMember()
    {
        if (AvailableMembers == 0)
            return 0;

        return (double)TotalCapacityPoints / AvailableMembers;
    }

    private static void ValidateCapacity(int capacity, string paramName)
    {
        if (capacity < 0)
        {
            throw new ArgumentException("Capacity cannot be negative.", paramName);
        }
    }

    private static void ValidateMembers(int members, string paramName)
    {
        if (members < 0)
        {
            throw new ArgumentException("Available members cannot be negative.", paramName);
        }
    }
}
