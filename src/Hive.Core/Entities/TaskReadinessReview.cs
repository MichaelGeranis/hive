namespace Hive.Core.Entities;

/// <summary>
/// The cached AI review of one task before it is built: whether it is ready, a short summary,
/// and what is missing. A cache, not a record: it can be discarded and made again from the
/// task at any time.
/// </summary>
public class TaskReadinessReview
{
    public const int MaxSummaryLength = 2000;
    public const int MaxJsonLength = 8000;
    public const int MaxModelLength = 200;

    public Guid Id { get; private set; }
    public Guid TeamTaskId { get; private set; }
    public TaskReadinessVerdict Verdict { get; private set; }
    public string Summary { get; private set; } = string.Empty;

    /// <summary>What the task lacks, as a JSON list of strings.</summary>
    public string MissingJson { get; private set; } = "[]";

    /// <summary>The model that reviewed the task, e.g. <c>llama3.1:8b</c> or <c>Claude</c>.</summary>
    public string Model { get; private set; } = string.Empty;

    /// <summary>The <see cref="TaskReadiness.Fingerprint"/> of the task as it was reviewed.</summary>
    public string SourceFingerprint { get; private set; } = string.Empty;

    public DateTime AnalyzedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TaskReadinessReview() { }

    public TaskReadinessReview(
        Guid teamTaskId,
        TaskReadinessVerdict verdict,
        string summary,
        string missingJson,
        string model,
        string sourceFingerprint)
    {
        if (teamTaskId == Guid.Empty)
            throw new ArgumentException("A review must be of a task.", nameof(teamTaskId));

        Id = Guid.NewGuid();
        TeamTaskId = teamTaskId;
        CreatedAt = DateTime.UtcNow;
        Apply(verdict, summary, missingJson, model, sourceFingerprint);
    }

    /// <summary>
    /// Replaces the review with a new one of the same task.
    /// </summary>
    public void Update(
        TaskReadinessVerdict verdict,
        string summary,
        string missingJson,
        string model,
        string sourceFingerprint)
    {
        Apply(verdict, summary, missingJson, model, sourceFingerprint);
    }

    /// <summary>
    /// True when the task as it stands now is not the one this review read.
    /// </summary>
    public bool IsStaleFor(string currentFingerprint) =>
        !string.Equals(SourceFingerprint, currentFingerprint, StringComparison.Ordinal);

    private void Apply(
        TaskReadinessVerdict verdict,
        string summary,
        string missingJson,
        string model,
        string sourceFingerprint)
    {
        if (!Enum.IsDefined(verdict))
            throw new ArgumentException("Unknown readiness verdict.", nameof(verdict));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("A task review cannot be empty.", nameof(summary));
        if (string.IsNullOrWhiteSpace(sourceFingerprint))
            throw new ArgumentException("A task review must record what it read.", nameof(sourceFingerprint));

        var missing = string.IsNullOrWhiteSpace(missingJson) ? "[]" : missingJson;
        if (missing.Length > MaxJsonLength)
            throw new ArgumentException($"The missing items cannot exceed {MaxJsonLength} characters.", nameof(missingJson));

        var text = summary.Trim();
        var modelName = model?.Trim() ?? string.Empty;

        Verdict = verdict;
        Summary = text.Length <= MaxSummaryLength ? text : text[..(MaxSummaryLength - 1)].TrimEnd() + "…";
        MissingJson = missing;
        Model = modelName.Length <= MaxModelLength ? modelName : modelName[..MaxModelLength];
        SourceFingerprint = sourceFingerprint;
        AnalyzedAt = DateTime.UtcNow;
    }
}
