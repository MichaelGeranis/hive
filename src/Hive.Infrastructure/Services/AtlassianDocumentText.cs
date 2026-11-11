using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hive.Infrastructure.Services;

/// <summary>
/// Turns an Atlassian Document Format value (how Jira Cloud stores rich text such as an
/// issue description) into Markdown: headings as <c>#</c>, bold, italic and code marks, list
/// items as "- " or "1. ", task items as "- [ ] ", code blocks fenced, quotes as "> ", tables
/// as pipe tables, and mentions, links and emoji as their text. Media is left out.
/// </summary>
public static class AtlassianDocumentText
{
    private static readonly Regex ExtraBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

    public static string ToMarkdown(JsonElement document)
    {
        if (document.ValueKind == JsonValueKind.String)
        {
            // Older endpoints return plain text as-is
            return document.GetString()?.Trim() ?? string.Empty;
        }

        if (document.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        Write(document, text, listPrefix: null);
        return ExtraBlankLines.Replace(text.ToString().Replace("\r", string.Empty), "\n\n").Trim();
    }

    private static void Write(JsonElement node, StringBuilder text, string? listPrefix)
    {
        var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;

        switch (type)
        {
            case "text":
                text.Append(WithMarks(node, node.TryGetProperty("text", out var value) ? value.GetString() : null));
                return;

            case "hardBreak":
                text.Append('\n');
                return;

            case "mention":
            case "emoji":
            case "status":
                text.Append(Attribute(node, "text") ?? Attribute(node, "shortName"));
                return;

            case "inlineCard":
            case "blockCard":
            case "embedCard":
                text.Append(Attribute(node, "url"));
                if (type != "inlineCard") text.Append("\n\n");
                return;

            case "rule":
                text.Append("---\n\n");
                return;

            case "media":
            case "mediaSingle":
            case "mediaGroup":
            case "mediaInline":
                return;

            case "taskList":
                foreach (var item in Children(node))
                {
                    var done = Attribute(item, "state") == "DONE";
                    text.Append(done ? "- [x] " : "- [ ] ");
                    WriteChildren(item, text, listPrefix: null);
                    text.Append('\n');
                }

                text.Append('\n');
                return;

            case "bulletList":
            case "orderedList":
                var number = 1;
                foreach (var item in Children(node))
                {
                    WriteChildren(item, text, type == "bulletList" ? "- " : $"{number++}. ");
                }

                text.Append('\n');
                return;

            case "listItem":
                WriteChildren(node, text, listPrefix);
                return;

            case "table":
                var rowIndex = 0;
                foreach (var row in Children(node))
                {
                    var cells = Children(row).Select(cell =>
                    {
                        var cellText = new StringBuilder();
                        WriteChildren(cell, cellText, listPrefix: null);
                        return cellText.ToString().Replace("\n", " ").Replace("|", "\\|").Trim();
                    }).ToList();
                    text.Append("| ").Append(string.Join(" | ", cells)).Append(" |\n");
                    if (rowIndex++ == 0)
                    {
                        text.Append('|').Append(string.Concat(cells.Select(_ => " --- |"))).Append('\n');
                    }
                }

                text.Append('\n');
                return;

            case "heading":
                text.Append(new string('#', Math.Clamp(IntAttribute(node, "level") ?? 1, 1, 6))).Append(' ');
                WriteChildren(node, text, listPrefix: null);
                text.Append("\n\n");
                return;

            case "codeBlock":
                text.Append("```").Append(Attribute(node, "language")).Append('\n');
                WriteChildren(node, text, listPrefix: null);
                text.Append("\n```\n\n");
                return;

            case "blockquote":
                var quote = new StringBuilder();
                WriteChildren(node, quote, listPrefix: null);
                foreach (var line in quote.ToString().Trim().Split('\n'))
                {
                    text.Append(line.Length == 0 ? ">" : "> " + line).Append('\n');
                }

                text.Append('\n');
                return;

            case "paragraph":
            case "panel":
                if (listPrefix is not null)
                {
                    text.Append(listPrefix);
                }

                WriteChildren(node, text, listPrefix: null);
                text.Append(listPrefix is not null ? "\n" : "\n\n");
                return;

            default:
                WriteChildren(node, text, listPrefix);
                return;
        }
    }

    private static void WriteChildren(JsonElement node, StringBuilder text, string? listPrefix)
    {
        foreach (var child in Children(node))
        {
            Write(child, text, listPrefix);
        }
    }

    private static IEnumerable<JsonElement> Children(JsonElement node) =>
        node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array
            ? content.EnumerateArray()
            : Enumerable.Empty<JsonElement>();

    /// <summary>Bold, italic and inline code, the marks Markdown can say.</summary>
    private static string? WithMarks(JsonElement node, string? value)
    {
        if (string.IsNullOrEmpty(value) || !node.TryGetProperty("marks", out var marks) || marks.ValueKind != JsonValueKind.Array)
        {
            return value;
        }

        var types = marks.EnumerateArray()
            .Select(m => m.TryGetProperty("type", out var type) ? type.GetString() : null)
            .ToHashSet();

        if (types.Contains("code")) return $"`{value}`";
        if (types.Contains("strong")) value = $"**{value}**";
        if (types.Contains("em")) value = $"*{value}*";
        return value;
    }

    private static int? IntAttribute(JsonElement node, string name) =>
        node.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object &&
        attrs.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static string? Attribute(JsonElement node, string name) =>
        node.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object &&
        attrs.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
