using System.Security;
using System.Text;
using System.Text.Json;
using Hive.Application.Interfaces;
using Hive.Core.Entities;

namespace Hive.Infrastructure.Services;

/// <summary>
/// The task readiness request every provider sends, and how its answer is read, so Claude and
/// a local Ollama model are asked the same question.
/// </summary>
internal static class TaskPrompt
{
    public const int MaxMissing = 6;
    private const int MaxItemLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// The task goes in as data, in tagged elements with markup escaped, so its description
    /// cannot be mistaken for instructions.
    /// </summary>
    public static string Build(TaskForReview task)
    {
        var sb = new StringBuilder();
        sb.AppendLine("This is a work item a software team is about to build, as written in Jira.");
        sb.AppendLine("Judge whether it says enough to build it without going back to ask, so the engineering manager can get it fixed first.");
        sb.AppendLine();
        sb.AppendLine("- verdict: \"Ready\" when an engineer could build and test it as written; \"NeedsWork\" when something important is missing or unclear; \"Unclear\" when there is too little written to tell what it is.");
        sb.AppendLine("- summary: 1 or 2 plain sentences on what the work item asks for.");
        sb.AppendLine($"- missing: at most {MaxMissing} questions an engineer would have to ask before starting, most important first, each about this work item in particular (for example \"What happens to a draft when the session expires?\"). Never list a generic category such as \"designs\", \"data\" or \"edge cases\". Leave it empty when it is ready.");
        sb.AppendLine("- What matters depends on the type. A story or task needs acceptance criteria: how anyone will know it is done. A bug needs steps to reproduce, and the expected and actual behaviour.");
        sb.AppendLine("- A small, clear work item with testable acceptance criteria is Ready; do not hold it back for things a team settles while building it.");
        sb.AppendLine("- summary says what the work item asks for in your own words; do not just repeat the title.");
        sb.AppendLine(task.HasAcceptanceCriteria
            ? "- The description has an acceptance criteria section. Do not report acceptance criteria as missing; say only if they are too vague to test."
            : "- The description has no acceptance criteria section.");
        sb.AppendLine("- Judge only what is written; do not invent requirements.");
        sb.AppendLine();
        sb.AppendLine("<work-item>");
        sb.AppendLine($"<type>{SecurityElement.Escape(task.Type)}</type>");
        sb.AppendLine($"<title>{SecurityElement.Escape(task.Title.Trim())}</title>");
        sb.AppendLine("<description>");
        sb.AppendLine(string.IsNullOrWhiteSpace(task.Description) ? "(no description)" : SecurityElement.Escape(task.Description.Trim()));
        sb.AppendLine("</description>");
        sb.AppendLine("</work-item>");
        return sb.ToString();
    }

    /// <summary>
    /// Reads the model's JSON answer. Returns null for an empty answer, one with no summary or
    /// one whose verdict is not one of the three.
    /// </summary>
    /// <exception cref="JsonException">The answer is not the JSON that was asked for.</exception>
    public static TaskReviewResult? Parse(string json)
    {
        var result = JsonSerializer.Deserialize<TaskResponse>(json, JsonOptions);
        if (result is null || string.IsNullOrWhiteSpace(result.Summary) ||
            !TaskReadiness.TryParseVerdict(result.Verdict, out var verdict))
        {
            return null;
        }

        var missing = (result.Missing ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Take(MaxMissing)
            .Select(Clip)
            .ToList();

        return new TaskReviewResult(verdict, result.Summary.Trim(), missing);
    }

    /// <summary>The JSON schema the answer must follow, enforced by the provider.</summary>
    public static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            verdict = new { type = "string", @enum = new[] { "Ready", "NeedsWork", "Unclear" } },
            summary = new { type = "string" },
            missing = new { type = "array", items = new { type = "string" } }
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "verdict", "summary", "missing" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private static string Clip(string text)
    {
        var value = text.Trim();
        return value.Length <= MaxItemLength ? value : value[..(MaxItemLength - 1)].TrimEnd() + "…";
    }

    private sealed class TaskResponse
    {
        public string? Verdict { get; set; }
        public string? Summary { get; set; }
        public string[]? Missing { get; set; }
    }
}
