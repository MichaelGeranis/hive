namespace Hive.Core.Entities;

/// <summary>
/// The cached AI summary of the team's latest retrospectives: what they say, the warnings,
/// the action items and the suggestions. A cache, not a record: it can be discarded and made
/// again from the retros at any time.
/// </summary>
public class RetroSummary
{
    public const int MaxSummaryLength = 4000;
    public const int MaxJsonLength = 16000;
    public const int MaxModelLength = 200;

    public Guid Id { get; private set; }
    public string Summary { get; private set; } = string.Empty;

    // Lists stored as JSON: warnings and suggestions are strings, action items objects
    public string WarningsJson { get; private set; } = "[]";
    public string ActionItemsJson { get; private set; } = "[]";
    public string SuggestionsJson { get; private set; } = "[]";

    /// <summary>The model that wrote the summary, e.g. <c>llama3.1:8b</c> or <c>Claude</c>.</summary>
    public string Model { get; private set; } = string.Empty;

    public int RetrosAnalyzed { get; private set; }
    public DateOnly LatestRetroDate { get; private set; }

    /// <summary>The <see cref="Retrospective.Fingerprint"/> of the retros that were read.</summary>
    public string SourceFingerprint { get; private set; } = string.Empty;

    public DateTime AnalyzedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RetroSummary() { }

    public RetroSummary(
        string summary,
        string warningsJson,
        string actionItemsJson,
        string suggestionsJson,
        string model,
        int retrosAnalyzed,
        DateOnly latestRetroDate,
        string sourceFingerprint)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Apply(summary, warningsJson, actionItemsJson, suggestionsJson, model, retrosAnalyzed, latestRetroDate, sourceFingerprint);
    }

    /// <summary>
    /// Replaces the summary with a new analysis of the retros.
    /// </summary>
    public void Update(
        string summary,
        string warningsJson,
        string actionItemsJson,
        string suggestionsJson,
        string model,
        int retrosAnalyzed,
        DateOnly latestRetroDate,
        string sourceFingerprint)
    {
        Apply(summary, warningsJson, actionItemsJson, suggestionsJson, model, retrosAnalyzed, latestRetroDate, sourceFingerprint);
    }

    /// <summary>
    /// True when the retros as they stand now are not the ones this summary read.
    /// </summary>
    public bool IsStaleFor(string currentFingerprint) =>
        !string.Equals(SourceFingerprint, currentFingerprint, StringComparison.Ordinal);

    private void Apply(
        string summary,
        string warningsJson,
        string actionItemsJson,
        string suggestionsJson,
        string model,
        int retrosAnalyzed,
        DateOnly latestRetroDate,
        string sourceFingerprint)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("A retro summary cannot be empty.", nameof(summary));
        if (retrosAnalyzed < 1)
            throw new ArgumentException("A retro summary reads at least one retro.", nameof(retrosAnalyzed));
        if (string.IsNullOrWhiteSpace(sourceFingerprint))
            throw new ArgumentException("A retro summary must record which retros it read.", nameof(sourceFingerprint));

        var text = summary.Trim();
        Summary = text.Length <= MaxSummaryLength ? text : text[..(MaxSummaryLength - 1)].TrimEnd() + "…";
        WarningsJson = ValidateJson(warningsJson, nameof(warningsJson));
        ActionItemsJson = ValidateJson(actionItemsJson, nameof(actionItemsJson));
        SuggestionsJson = ValidateJson(suggestionsJson, nameof(suggestionsJson));

        var modelName = model?.Trim() ?? string.Empty;
        Model = modelName.Length <= MaxModelLength ? modelName : modelName[..MaxModelLength];

        RetrosAnalyzed = retrosAnalyzed;
        LatestRetroDate = latestRetroDate;
        SourceFingerprint = sourceFingerprint;
        AnalyzedAt = DateTime.UtcNow;
    }

    private static string ValidateJson(string? json, string paramName)
    {
        var value = string.IsNullOrWhiteSpace(json) ? "[]" : json;
        if (value.Length > MaxJsonLength)
            throw new ArgumentException($"A retro summary list cannot exceed {MaxJsonLength} characters.", paramName);
        return value;
    }
}
