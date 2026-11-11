namespace Hive.Core.Entities;

/// <summary>
/// Represents a planning quarter (Q1-Q4 of a year).
/// </summary>
public class Quarter
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int QuarterNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public QuarterStatus Status { get; private set; }
    public string OkrReference { get; private set; } = string.Empty;

    // The agreed split of the team's capacity across work types, in percent; they add up to 100
    public int ProductRoadmapPercent { get; private set; } = DefaultProductRoadmapPercent;
    public int TechRoadmapPercent { get; private set; } = DefaultTechRoadmapPercent;
    public int MaintenancePercent { get; private set; } = DefaultMaintenancePercent;

    /// <summary>When the plan was last committed; null while it never was.</summary>
    public DateTime? CommittedAt { get; private set; }

    /// <summary>The plan as it stood when committed, serialised, to compare reality against.</summary>
    public string Baseline { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public const int DefaultProductRoadmapPercent = 50;
    public const int DefaultTechRoadmapPercent = 20;
    public const int DefaultMaintenancePercent = 30;

    private Quarter() { }

    public Quarter(int year, int quarterNumber, string? okrReference = null)
    {
        ValidateYear(year);
        ValidateQuarterNumber(quarterNumber);

        Id = Guid.NewGuid();
        Year = year;
        QuarterNumber = quarterNumber;
        Name = $"Q{quarterNumber} {year}";
        Status = QuarterStatus.Planning;
        OkrReference = okrReference?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? okrReference)
    {
        OkrReference = okrReference?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the target split of capacity across work types. Each share is 0–100 and together
    /// they are the whole team: 100.
    /// </summary>
    public void SetMixTarget(int productRoadmapPercent, int techRoadmapPercent, int maintenancePercent)
    {
        foreach (var (value, name) in new[]
                 {
                     (productRoadmapPercent, nameof(productRoadmapPercent)),
                     (techRoadmapPercent, nameof(techRoadmapPercent)),
                     (maintenancePercent, nameof(maintenancePercent))
                 })
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentException("Each share of the mix must be between 0 and 100 percent.", name);
            }
        }

        if (productRoadmapPercent + techRoadmapPercent + maintenancePercent != 100)
        {
            throw new ArgumentException("The mix must add up to 100 percent.", nameof(productRoadmapPercent));
        }

        ProductRoadmapPercent = productRoadmapPercent;
        TechRoadmapPercent = techRoadmapPercent;
        MaintenancePercent = maintenancePercent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>The target share of a work type, in percent.</summary>
    public int GetMixPercent(WorkType workType) => workType switch
    {
        WorkType.ProductRoadmap => ProductRoadmapPercent,
        WorkType.TechRoadmap => TechRoadmapPercent,
        WorkType.Maintenance => MaintenancePercent,
        _ => 0
    };

    /// <summary>
    /// Commits the plan: records it as the baseline that reality is later compared with.
    /// Committing again replaces the baseline. A completed quarter is history.
    /// </summary>
    public void Commit(string baseline)
    {
        if (Status == QuarterStatus.Completed)
        {
            throw new InvalidOperationException("A completed quarter cannot be committed.");
        }

        if (string.IsNullOrWhiteSpace(baseline))
        {
            throw new ArgumentException("A commitment needs the plan it commits to.", nameof(baseline));
        }

        Baseline = baseline;
        CommittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsCommitted => CommittedAt.HasValue;

    /// <summary>The quarter before this one, as (year, quarter number).</summary>
    public (int Year, int QuarterNumber) Previous() =>
        QuarterNumber == 1 ? (Year - 1, 4) : (Year, QuarterNumber - 1);

    /// <summary>The quarter after this one, as (year, quarter number).</summary>
    public (int Year, int QuarterNumber) Next() =>
        QuarterNumber == 4 ? (Year + 1, 1) : (Year, QuarterNumber + 1);

    public void Activate()
    {
        if (Status == QuarterStatus.Completed)
        {
            throw new InvalidOperationException("Cannot activate a completed quarter.");
        }

        Status = QuarterStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == QuarterStatus.Planning)
        {
            throw new InvalidOperationException("Cannot complete a quarter that is still in planning status.");
        }

        Status = QuarterStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetToPlanning()
    {
        Status = QuarterStatus.Planning;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateYear(int year)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ArgumentException("Year must be between 2000 and 2100.", nameof(year));
        }
    }

    private static void ValidateQuarterNumber(int quarterNumber)
    {
        if (quarterNumber < 1 || quarterNumber > 4)
        {
            throw new ArgumentException("Quarter number must be between 1 and 4.", nameof(quarterNumber));
        }
    }
}
