namespace Hive.Core.Entities;

/// <summary>
/// A direct report's career plan: one per person, ongoing. It records the skills of a
/// <see cref="SkillRoadmap"/> (copied into <see cref="CareerPlanSkill"/>): which they have, which
/// not yet, and which they are working on now. It has no goal: the person's
/// <see cref="CurrentStage"/> is a fact, the yardstick their skills are read against. It is the
/// manager's own view; the direct report never sees it.
/// </summary>
public class CareerPlan
{
    public const int MaxRoadmapNameLength = SkillRoadmap.MaxNameLength;
    public const int MaxNotesLength = 4000;

    /// <summary>
    /// A plan nothing on which has changed for this many days has gone quiet.
    /// </summary>
    public const int QuietDays = 60;

    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }

    /// <summary>
    /// The name of the roadmap the skills were copied from, kept as it was when the plan was made.
    /// </summary>
    public string RoadmapName { get; private set; } = string.Empty;

    /// <summary>
    /// The stage of the direct report's current role. Not a goal: it is what their skills are
    /// compared with, to show where they sit on the ladder.
    /// </summary>
    public CareerStage CurrentStage { get; private set; }

    public string Notes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CareerPlan() { }

    public CareerPlan(Guid directReportId, string roadmapName, CareerStage currentStage, string? notes = null)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }

        if (string.IsNullOrWhiteSpace(roadmapName))
        {
            throw new ArgumentException("Roadmap name cannot be empty.", nameof(roadmapName));
        }

        if (roadmapName.Trim().Length > MaxRoadmapNameLength)
        {
            throw new ArgumentException($"Roadmap name cannot exceed {MaxRoadmapNameLength} characters.", nameof(roadmapName));
        }

        Validate(currentStage, notes);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        RoadmapName = roadmapName.Trim();
        CurrentStage = currentStage;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a change of role stage (a promotion, say) or new notes.
    /// </summary>
    public void Update(CareerStage currentStage, string? notes)
    {
        Validate(currentStage, notes);

        CurrentStage = currentStage;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// A plan has gone quiet when neither it nor any of its skills has changed for
    /// <see cref="QuietDays"/> days.
    /// </summary>
    public bool IsQuiet(DateTime? lastSkillChangeAt, DateTime now)
    {
        var lastChange = new[] { CreatedAt, UpdatedAt ?? CreatedAt, lastSkillChangeAt ?? CreatedAt }.Max();
        return lastChange < now.AddDays(-QuietDays);
    }

    private static void Validate(CareerStage currentStage, string? notes)
    {
        if (!Enum.IsDefined(currentStage))
        {
            throw new ArgumentException("Unknown career stage.", nameof(currentStage));
        }

        if (notes is not null && notes.Trim().Length > MaxNotesLength)
        {
            throw new ArgumentException($"Notes cannot exceed {MaxNotesLength} characters.", nameof(notes));
        }
    }
}
