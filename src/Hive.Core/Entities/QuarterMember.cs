namespace Hive.Core.Entities;

/// <summary>
/// A person on a quarter's plan, and how much of their time the team can plan with: 100 for a
/// full-time engineer, less for a part-timer or someone shared with another team.
/// </summary>
public class QuarterMember
{
    public const int DefaultCapacityPercent = 100;

    public Guid Id { get; private set; }
    public Guid QuarterId { get; private set; }
    public Guid DirectReportId { get; private set; }
    public int CapacityPercent { get; private set; } = DefaultCapacityPercent;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private QuarterMember() { }

    public QuarterMember(Guid quarterId, Guid directReportId, int capacityPercent = DefaultCapacityPercent)
    {
        ValidateCapacity(capacityPercent);

        Id = Guid.NewGuid();
        QuarterId = quarterId;
        DirectReportId = directReportId;
        CapacityPercent = capacityPercent;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetCapacity(int capacityPercent)
    {
        ValidateCapacity(capacityPercent);
        CapacityPercent = capacityPercent;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CapacityFactor => CapacityPercent / 100m;

    private static void ValidateCapacity(int capacityPercent)
    {
        if (capacityPercent < 1 || capacityPercent > 100)
        {
            throw new ArgumentException("Planned capacity is between 1% and 100%.", nameof(capacityPercent));
        }
    }
}
