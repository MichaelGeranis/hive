using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;
using Hive.Application.Interfaces;

namespace Hive.Infrastructure.Services;

/// <summary>
/// The retro summary request every provider sends, and how its answer is read, so Claude and
/// a local Ollama model are asked the same question.
/// </summary>
internal static class RetroPrompt
{
    public const int MaxWarnings = 5;
    public const int MaxActionItems = 10;
    public const int MaxSuggestions = 5;
    private const int MaxItemLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// The retros go in as data, one tagged block each and oldest first, so markdown inside a
    /// retro cannot be mistaken for instructions or for the boundary between retros.
    /// </summary>
    public static string Build(IReadOnlyList<RetroForAnalysis> retros)
    {
        var sb = new StringBuilder();
        sb.AppendLine("These are an engineering manager's notes from their team's sprint retrospectives, oldest first.");
        sb.AppendLine("Summarise them for the manager, so they can see what the team keeps raising and what to do about it.");
        sb.AppendLine();
        sb.AppendLine("- summary: 3 to 5 plain sentences on how the retros read, what went well, what did not, and how that changed over time.");
        sb.AppendLine($"- warnings: at most {MaxWarnings}, one sentence each, only for what needs the manager's attention: a problem raised in several retros, an action item agreed and not followed up in a later retro, falling morale, or anything serious. Leave the list empty when there is nothing.");
        sb.AppendLine($"- actionItems: at most {MaxActionItems}, what the team agreed to do, as the retros put it, most recent first. retroDate is the date of the retro it was agreed in, as yyyy-MM-dd. owner is the person named for it, or an empty string. Leave out items a later retro reports as done.");
        sb.AppendLine($"- suggestions: 2 to {MaxSuggestions} concrete things the manager could do next, grounded in what the retros say.");
        sb.AppendLine("- Use only what the retros say; do not invent people, events or numbers.");
        sb.AppendLine();
        sb.AppendLine("<retros>");

        foreach (var retro in retros.OrderBy(r => r.RetroDate))
        {
            var date = retro.RetroDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.AppendLine($"<retro date=\"{date}\">");
            sb.AppendLine(SecurityElement.Escape(retro.Content.Trim()));
            sb.AppendLine("</retro>");
        }

        sb.AppendLine("</retros>");
        return sb.ToString();
    }

    /// <summary>
    /// Reads the model's JSON answer. Returns null for an empty answer or one with no summary.
    /// </summary>
    /// <exception cref="JsonException">The answer is not the JSON that was asked for.</exception>
    public static RetroSummaryResult? Parse(string json)
    {
        var result = JsonSerializer.Deserialize<RetroResponse>(json, JsonOptions);
        if (result is null || string.IsNullOrWhiteSpace(result.Summary))
        {
            return null;
        }

        var actionItems = (result.ActionItems ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Text))
            .Take(MaxActionItems)
            .Select(a => new RetroActionItem(Clip(a.Text!), ParseDate(a.RetroDate), Clip(a.Owner ?? string.Empty)))
            .ToList();

        return new RetroSummaryResult(
            Summary: result.Summary.Trim(),
            Warnings: Clean(result.Warnings, MaxWarnings),
            ActionItems: actionItems,
            Suggestions: Clean(result.Suggestions, MaxSuggestions));
    }

    /// <summary>The JSON schema the answer must follow, enforced by the provider.</summary>
    public static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            summary = new { type = "string" },
            warnings = new { type = "array", items = new { type = "string" } },
            actionItems = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string" },
                        retroDate = new { type = "string" },
                        owner = new { type = "string" }
                    },
                    required = new[] { "text", "retroDate", "owner" },
                    additionalProperties = false
                }
            },
            suggestions = new { type = "array", items = new { type = "string" } }
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "summary", "warnings", "actionItems", "suggestions" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private static List<string> Clean(IEnumerable<string>? items, int max) =>
        (items ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Take(max)
            .Select(Clip)
            .ToList();

    private static string Clip(string text)
    {
        var value = text.Trim();
        return value.Length <= MaxItemLength ? value : value[..(MaxItemLength - 1)].TrimEnd() + "…";
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private sealed class RetroResponse
    {
        public string? Summary { get; set; }
        public string[]? Warnings { get; set; }
        public ActionItem[]? ActionItems { get; set; }
        public string[]? Suggestions { get; set; }
    }

    private sealed class ActionItem
    {
        public string? Text { get; set; }
        public string? RetroDate { get; set; }
        public string? Owner { get; set; }
    }
}
