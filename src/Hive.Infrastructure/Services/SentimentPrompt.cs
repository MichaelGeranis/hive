using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;
using Hive.Application.Interfaces;

namespace Hive.Infrastructure.Services;

/// <summary>
/// The sentiment request every provider sends, and how its answer is read, so Claude and a
/// local Ollama model are asked the same question and give comparable results.
/// </summary>
internal static class SentimentPrompt
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// The notes go in as data, one tagged block each, so markdown inside a note cannot be
    /// mistaken for instructions or for the boundary between notes.
    /// </summary>
    public static string Build(IReadOnlyList<MeetingNoteForAnalysis> notes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("These are an engineering manager's notes from 1:1 meetings with one member of their team.");
        sb.AppendLine("Assess the sentiment they show about that person's morale and engagement, so the manager can notice changes over time.");
        sb.AppendLine();
        sb.AppendLine("- positive, neutral and negative are percentages that total 100.");
        sb.AppendLine("- keyThemes: up to 5 short themes that recur, such as \"career growth\", \"workload\" or \"team collaboration\".");
        sb.AppendLine("- monthlyBreakdown: one entry per calendar month that has notes, with that month's own percentages and note count.");
        sb.AppendLine("- Judge from what the notes say about the person. Blockers, frustration and concerns weigh negative; achievements, recognition and enthusiasm weigh positive.");
        sb.AppendLine("- reasoning: 2 to 4 plain sentences telling the manager why you judged the notes this way, naming what in them led you there and any change over time. Refer to the person as \"they\".");
        sb.AppendLine();
        sb.AppendLine("<notes>");

        foreach (var note in notes.OrderBy(n => n.MeetingDate))
        {
            var date = note.MeetingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.AppendLine($"<note date=\"{date}\">");
            sb.AppendLine(SecurityElement.Escape(note.Content.Trim()));
            sb.AppendLine("</note>");
        }

        sb.AppendLine("</notes>");
        return sb.ToString();
    }

    /// <summary>The empty result for a person with no notes, without asking any model.</summary>
    public static SentimentAnalysisResult NoNotes() => new(
        Positive: 0,
        Neutral: 100,
        Negative: 0,
        OverallSentiment: "Neutral",
        KeyThemes: Array.Empty<string>(),
        MonthlyBreakdown: Array.Empty<MonthlySentiment>(),
        Reasoning: "There are no 1:1 notes to analyse.");

    /// <summary>
    /// Reads the model's JSON answer. Returns null for an empty answer.
    /// </summary>
    /// <exception cref="JsonException">The answer is not the JSON that was asked for.</exception>
    public static SentimentAnalysisResult? Parse(string json, IReadOnlyList<MeetingNoteForAnalysis> notes)
    {
        var result = JsonSerializer.Deserialize<SentimentResponse>(json, JsonOptions);
        if (result is null)
        {
            return null;
        }

        var monthlyBreakdown = result.MonthlyBreakdown?
            .Select(m => new MonthlySentiment(m.Year, m.Month, m.Positive, m.Neutral, m.Negative, m.NotesCount))
            .ToList() ?? new List<MonthlySentiment>();

        // Without a per-month answer, every month carries the overall scores
        if (monthlyBreakdown.Count == 0)
        {
            monthlyBreakdown = notes
                .GroupBy(n => new { n.MeetingDate.Year, n.MeetingDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlySentiment(g.Key.Year, g.Key.Month, result.Positive, result.Neutral, result.Negative, g.Count()))
                .ToList();
        }

        return new SentimentAnalysisResult(
            Positive: result.Positive,
            Neutral: result.Neutral,
            Negative: result.Negative,
            OverallSentiment: result.OverallSentiment ?? "Neutral",
            KeyThemes: result.KeyThemes ?? Array.Empty<string>(),
            MonthlyBreakdown: monthlyBreakdown,
            // Never empty: an empty reasoning marks a cached result as one to analyse again.
            Reasoning: string.IsNullOrWhiteSpace(result.Reasoning)
                ? "The model gave no reasoning for this result."
                : result.Reasoning.Trim());
    }

    /// <summary>The JSON schema the answer must follow, enforced by the provider.</summary>
    public static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            positive = new { type = "number" },
            neutral = new { type = "number" },
            negative = new { type = "number" },
            overallSentiment = new { type = "string", @enum = new[] { "Positive", "Neutral", "Negative", "Mixed" } },
            keyThemes = new { type = "array", items = new { type = "string" } },
            reasoning = new { type = "string" },
            monthlyBreakdown = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        year = new { type = "integer" },
                        month = new { type = "integer" },
                        positive = new { type = "number" },
                        neutral = new { type = "number" },
                        negative = new { type = "number" },
                        notesCount = new { type = "integer" }
                    },
                    required = new[] { "year", "month", "positive", "neutral", "negative", "notesCount" },
                    additionalProperties = false
                }
            }
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[]
        {
            "positive", "neutral", "negative", "overallSentiment", "keyThemes", "monthlyBreakdown", "reasoning"
        }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private sealed class SentimentResponse
    {
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
        public string? OverallSentiment { get; set; }
        public string[]? KeyThemes { get; set; }
        public MonthlyBreakdown[]? MonthlyBreakdown { get; set; }
        public string? Reasoning { get; set; }
    }

    private sealed class MonthlyBreakdown
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
        public int NotesCount { get; set; }
    }
}
