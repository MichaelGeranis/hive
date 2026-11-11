using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// A body of work on a quarter's plan: a candidate option from the PM, work left over from the
/// previous quarter, or one of the standing lanes (Maintenance, Tech roadmap).
/// </summary>
public class Initiative
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxDecisionNoteLength = 1000;

    public Guid Id { get; private set; }
    public Guid QuarterId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    /// <summary>
    /// The T-shirt size from the solution design. An option has none until it is ready for
    /// commitment: sizing is what the discovery and the solution design lead to.
    /// </summary>
    public string? TshirtSize { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public WorkType WorkType { get; private set; } = WorkType.ProductRoadmap;
    public InitiativeKind Kind { get; private set; } = InitiativeKind.Option;
    public InitiativeStatus Status { get; private set; } = InitiativeStatus.InDiscovery;

    /// <summary>The PM's priority: 1 is the most important. Leftovers and lanes sort apart.</summary>
    public int Rank { get; private set; }

    public ReadinessIssue Readiness { get; private set; } = ReadinessIssue.None;

    /// <summary>Why it was committed, made a stretch or dropped.</summary>
    public string DecisionNote { get; private set; } = string.Empty;

    /// <summary>The first sprint work can start in, e.g. when requirements land late.</summary>
    public Guid? EarliestSprintId { get; private set; }

    /// <summary>When an option arrived after the quarter was planned; null for planned options.</summary>
    public DateTime? ArrivedOn { get; private set; }

    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // A size is S, M or L, optionally widened with X (XS, XL, XXL…) and nudged up with a plus (L+)
    private static readonly Regex SizePattern = new("^(X{0,3}S|M|X{0,3}L)\\+?$", RegexOptions.Compiled);

    // Available colors for initiatives - used by service to assign unique colors
    public static readonly string[] AvailableColors =
    {
        "#8B5CF6", // Purple
        "#10B981", // Green
        "#F59E0B", // Amber
        "#EC4899", // Pink
        "#06B6D4", // Cyan
        "#F97316", // Orange
        "#14B8A6", // Teal
        "#6366F1", // Indigo
        "#84CC16", // Lime
        "#A855F7", // Violet
        "#0EA5E9", // Sky
        "#D946EF", // Fuchsia
    };

    public const string MaintenanceColor = "#EF4444";
    public const string TechRoadmapColor = "#3B82F6";

    private Initiative() { }

    public Initiative(
        Guid quarterId,
        string name,
        string color,
        string? description = null,
        string? tshirtSize = null,
        string? url = null,
        WorkType workType = WorkType.ProductRoadmap,
        InitiativeKind kind = InitiativeKind.Option,
        int rank = 0,
        DateTime? arrivedOn = null,
        DateTime? dueDate = null,
        InitiativeStatus status = InitiativeStatus.InDiscovery)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidateColor(color);
        ValidateTshirtSize(tshirtSize);
        ValidateUrl(url);
        ValidateRank(rank);
        ValidateKinds(workType, kind);
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Unknown status.", nameof(status));
        }

        var initialStatus = kind == InitiativeKind.Bucket ? InitiativeStatus.Committed : status;
        ValidateSizeAllowed(tshirtSize, kind, initialStatus);

        Id = Guid.NewGuid();
        QuarterId = quarterId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Color = color.Trim();
        TshirtSize = NormaliseSize(tshirtSize);
        Url = url?.Trim() ?? string.Empty;
        WorkType = workType;
        Kind = kind;
        Rank = rank;
        ArrivedOn = arrivedOn?.Date;
        DueDate = dueDate?.Date;
        Status = initialStatus;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// A standing lane: work that happens every quarter and is sized by the mix target rather
    /// than by a T-shirt, such as Maintenance or the Tech roadmap. It is always committed.
    /// </summary>
    public static Initiative CreateBucket(Guid quarterId, WorkType workType)
    {
        var (name, color) = workType switch
        {
            WorkType.Maintenance => ("Maintenance", MaintenanceColor),
            WorkType.TechRoadmap => ("Tech roadmap", TechRoadmapColor),
            _ => throw new ArgumentException("Only Maintenance and Tech roadmap are standing lanes.", nameof(workType))
        };

        return new Initiative(quarterId, name, color, workType: workType, kind: InitiativeKind.Bucket);
    }

    public bool IsBucket => Kind == InitiativeKind.Bucket;

    /// <summary>
    /// Whether the initiative is still in the running for the quarter: not dropped.
    /// </summary>
    public bool IsInPlay => Status != InitiativeStatus.Dropped;

    public void Update(
        string name,
        string? description,
        string? color,
        string? url,
        WorkType? workType,
        InitiativeKind? kind,
        DateTime? arrivedOn,
        DateTime? dueDate)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidateColor(color);
        ValidateUrl(url);
        ValidateKinds(workType ?? WorkType, kind ?? Kind);

        if (IsBucket && kind.HasValue && kind.Value != InitiativeKind.Bucket)
        {
            throw new InvalidOperationException("A standing lane cannot become an option.");
        }

        if (!IsBucket && kind == InitiativeKind.Bucket)
        {
            throw new InvalidOperationException("An option cannot become a standing lane.");
        }

        if (IsBucket && workType.HasValue && workType.Value != WorkType)
        {
            throw new InvalidOperationException("A standing lane keeps its work type.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Color = string.IsNullOrWhiteSpace(color) ? Color : color.Trim();
        Url = url?.Trim() ?? string.Empty;
        if (workType.HasValue) WorkType = workType.Value;
        if (kind.HasValue) Kind = kind.Value;
        ArrivedOn = arrivedOn?.Date;
        DueDate = dueDate?.Date;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Whether an option in this status can carry a size: from ready for commitment on.</summary>
    public static bool CanBeSized(InitiativeStatus status) =>
        status is not (InitiativeStatus.InDiscovery or InitiativeStatus.InSolutionDesign);

    /// <summary>
    /// Sizes the option, or clears its size with null. Only an option ready for commitment, or
    /// decided since, can be sized; a standing lane is sized by the mix, never by a T-shirt.
    /// </summary>
    public void SetSize(string? tshirtSize)
    {
        ValidateTshirtSize(tshirtSize);
        ValidateSizeAllowed(tshirtSize, Kind, Status);
        TshirtSize = NormaliseSize(tshirtSize);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRank(int rank)
    {
        ValidateRank(rank);
        Rank = rank;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEarliestSprint(Guid? sprintId)
    {
        EarliestSprintId = sprintId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records what still stands in the way. Raising an issue on an option ready for commitment
    /// puts it back in solution design: it is no longer ready, and so no longer sized.
    /// </summary>
    public void SetReadiness(ReadinessIssue issues)
    {
        const ReadinessIssue all = ReadinessIssue.RequirementsPending | ReadinessIssue.EstimationPending |
                                   ReadinessIssue.ReleaseCoordination | ReadinessIssue.SolutionDesignPending;
        if ((issues & ~all) != 0)
        {
            throw new ArgumentException("Unknown readiness issue.", nameof(issues));
        }

        Readiness = issues;
        if (issues != ReadinessIssue.None && Status == InitiativeStatus.ReadyForCommitment)
        {
            Status = InitiativeStatus.InSolutionDesign;
            TshirtSize = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasReadinessIssues => Readiness != ReadinessIssue.None;

    public void MarkInDiscovery() => ChangeStatus(InitiativeStatus.InDiscovery, null);

    public void MarkInSolutionDesign() => ChangeStatus(InitiativeStatus.InSolutionDesign, null);

    /// <summary>
    /// Ready for commitment. An option with open readiness issues is not ready.
    /// </summary>
    public void MarkReadyForCommitment()
    {
        if (HasReadinessIssues)
        {
            throw new InvalidOperationException("An option with open readiness issues cannot be ready for commitment. Resolve them first.");
        }

        ChangeStatus(InitiativeStatus.ReadyForCommitment, null);
    }

    public void Commit(string? note = null) => ChangeStatus(InitiativeStatus.Committed, note);

    public void MakeStretch(string? note = null) => ChangeStatus(InitiativeStatus.Stretch, note);

    public void Drop(string? note = null) => ChangeStatus(InitiativeStatus.Dropped, note);

    /// <summary>
    /// Delivered: only work the quarter took on, committed or stretch, can be delivered.
    /// </summary>
    public void MarkDelivered()
    {
        if (Status != InitiativeStatus.Committed && Status != InitiativeStatus.Stretch)
        {
            throw new InvalidOperationException("Only a committed or stretch option can be delivered.");
        }

        ChangeStatus(InitiativeStatus.Delivered, null);
    }

    /// <summary>
    /// A copy of this option for the next quarter, as a leftover: same name, notes, size and
    /// design link. With nothing left to wait on it is ready for commitment and keeps its size;
    /// otherwise it is back in solution design, unsized.
    /// </summary>
    public Initiative CarryOverTo(Guid quarterId)
    {
        if (IsBucket)
        {
            throw new InvalidOperationException("Standing lanes exist every quarter and are not carried over.");
        }

        if (quarterId == QuarterId)
        {
            throw new InvalidOperationException("An option is carried over to another quarter.");
        }

        var ready = !HasReadinessIssues;
        var copy = new Initiative(
            quarterId, Name, Color, Description, ready ? TshirtSize : null, Url, WorkType, InitiativeKind.Leftover, 0, null, DueDate,
            ready ? InitiativeStatus.ReadyForCommitment : InitiativeStatus.InSolutionDesign);
        copy.SetReadiness(Readiness);
        return copy;
    }

    private void ChangeStatus(InitiativeStatus status, string? note)
    {
        if (IsBucket)
        {
            throw new InvalidOperationException("A standing lane is always committed.");
        }

        if (note is not null && note.Trim().Length > MaxDecisionNoteLength)
        {
            throw new ArgumentException($"A decision note cannot exceed {MaxDecisionNoteLength} characters.", nameof(note));
        }

        Status = status;
        if (!CanBeSized(status)) TshirtSize = null;
        if (note is not null) DecisionNote = note.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateSizeAllowed(string? tshirtSize, InitiativeKind kind, InitiativeStatus status)
    {
        if (NormaliseSize(tshirtSize) is null) return;

        if (kind == InitiativeKind.Bucket)
        {
            throw new InvalidOperationException("A standing lane is sized by the mix, not by a T-shirt.");
        }

        if (!CanBeSized(status))
        {
            throw new InvalidOperationException("An option is sized once it is ready for commitment.");
        }
    }

    private static string? NormaliseSize(string? size) =>
        string.IsNullOrWhiteSpace(size) ? null : size.Trim().ToUpperInvariant();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Initiative name cannot be empty.", nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Initiative name cannot exceed {MaxNameLength} characters.", nameof(name));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Initiative notes cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }
    }

    private static void ValidateKinds(WorkType workType, InitiativeKind kind)
    {
        if (!Enum.IsDefined(workType))
        {
            throw new ArgumentException("Work type must be Maintenance, Product roadmap or Tech roadmap.", nameof(workType));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Unknown initiative kind.", nameof(kind));
        }
    }

    private static void ValidateRank(int rank)
    {
        if (rank < 0 || rank > 999)
        {
            throw new ArgumentException("Priority must be between 0 and 999.", nameof(rank));
        }
    }

    private static void ValidateColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return;

        // Validate hex color format (#RRGGBB or #RGB)
        var trimmed = color.Trim();
        if (!trimmed.StartsWith("#"))
        {
            throw new ArgumentException("Color must be a hex color starting with #.", nameof(color));
        }

        var hex = trimmed.Substring(1);
        if (hex.Length != 3 && hex.Length != 6)
        {
            throw new ArgumentException("Color must be in #RGB or #RRGGBB format.", nameof(color));
        }

        if (!hex.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
        {
            throw new ArgumentException("Color must contain only hex characters.", nameof(color));
        }
    }

    private static void ValidateTshirtSize(string? tshirtSize)
    {
        var size = NormaliseSize(tshirtSize);
        if (size is not null && !SizePattern.IsMatch(size))
        {
            throw new ArgumentException(
                "T-shirt size must be a size such as S, M, L, XL or XXL, optionally with a plus (L+).",
                nameof(tshirtSize));
        }
    }

    private static void ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var trimmed = url.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("URL cannot exceed 500 characters.", nameof(url));
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be a valid HTTP or HTTPS URL.", nameof(url));
        }
    }
}
