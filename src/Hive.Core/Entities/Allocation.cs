namespace Hive.Core.Entities;

/// <summary>
/// One person on one initiative in one sprint, for a share of their time. Two initiatives
/// in the same cell are two allocations, e.g. 50% each.
/// </summary>
public class Allocation
{
    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public Guid DirectReportId { get; private set; }
    public Guid SprintId { get; private set; }

    /// <summary>The share of the person's sprint, 1–100.</summary>
    public int Percent { get; private set; } = 100;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Allocation() { }

    public Allocation(
        Guid initiativeId,
        Guid directReportId,
        Guid sprintId,
        int percent = 100)
    {
        ValidatePercent(percent);

        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        DirectReportId = directReportId;
        SprintId = sprintId;
        Percent = percent;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetPercent(int percent)
    {
        ValidatePercent(percent);
        Percent = percent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The rule for one person's sprint: each initiative at most once, each share 1–100,
    /// and the shares together no more than the whole sprint.
    /// </summary>
    public static void ValidateCell(IReadOnlyCollection<(Guid InitiativeId, int Percent)> shares)
    {
        foreach (var share in shares)
        {
            ValidatePercent(share.Percent);
        }

        if (shares.Select(s => s.InitiativeId).Distinct().Count() != shares.Count)
        {
            throw new ArgumentException("An initiative appears only once in a person's sprint.", nameof(shares));
        }

        if (shares.Sum(s => s.Percent) > 100)
        {
            throw new ArgumentException("A person's sprint cannot be allocated beyond 100%.", nameof(shares));
        }
    }

    private static void ValidatePercent(int percent)
    {
        if (percent < 1 || percent > 100)
        {
            throw new ArgumentException("An allocation is between 1% and 100% of a sprint.", nameof(percent));
        }
    }
}
