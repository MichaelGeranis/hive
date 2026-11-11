namespace Hive.Application.DTOs;

/// <summary>
/// The upcoming tasks and how ready they are to build: the facts Hive checks itself, and the
/// AI review of each.
/// </summary>
public record TaskReadinessOverviewDto
{
    public bool IsEnabled { get; init; }

    /// <summary>The chosen model has what it needs (Claude needs a key).</summary>
    public bool IsConfigured { get; init; }

    public string? CurrentSprint { get; init; }
    public string? NextSprint { get; init; }

    /// <summary>Not started, in the current or next sprint: current sprint first, then by priority.</summary>
    public IReadOnlyList<TaskReadinessItemDto> Tasks { get; init; } = Array.Empty<TaskReadinessItemDto>();

    /// <summary>Upcoming tasks with no review, or one of an earlier version of the task.</summary>
    public int PendingCount { get; init; }

    /// <summary>Why the last review could not be made, safe to show the manager.</summary>
    public string? Error { get; init; }
}

/// <summary>One upcoming task, with the facts and its review.</summary>
public record TaskReadinessItemDto
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string Sprint { get; init; } = string.Empty;
    public string? JiraKey { get; init; }

    /// <summary>A story or task, which must say what done means.</summary>
    public bool NeedsAcceptanceCriteria { get; init; }

    /// <summary>The description has an acceptance criteria section, or Given … Then scenarios.</summary>
    public bool HasAcceptanceCriteria { get; init; }

    /// <summary>Null until the task has been reviewed.</summary>
    public TaskAiReviewDto? Review { get; init; }
}

/// <summary>
/// The AI review of a task, as shown on the dashboard and as the bee badge on the Tasks page.
/// </summary>
public record TaskAiReviewDto
{
    /// <summary>"Ready", "NeedsWork" or "Unclear".</summary>
    public string Verdict { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Missing { get; init; } = Array.Empty<string>();
    public string Model { get; init; } = string.Empty;
    public DateTime AnalyzedAt { get; init; }

    /// <summary>The review read the task as it stands now.</summary>
    public bool IsUpToDate { get; init; }
}
