using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// Text rules shared by everything in Hive that is written as free-form markdown:
/// how a title is taken from the first line, and how a tag string is normalised.
/// </summary>
public static class NoteText
{
    private const int MaxTitleLength = 200;

    /// <summary>
    /// Derives a title from the first non-empty line of content, stripping the markdown
    /// that decorates that line. Falls back to the supplied placeholder for empty text.
    /// </summary>
    public static string DeriveTitle(string? content, string fallback)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return fallback;
        }

        var firstLine = content
            .Split('\n')
            .Select(StripMarkdown)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return fallback;
        }

        return firstLine.Length > MaxTitleLength ? firstLine[..MaxTitleLength].TrimEnd() : firstLine;
    }

    /// <summary>
    /// Removes the markdown decoration from a single line so it reads as a plain title.
    /// </summary>
    private static string StripMarkdown(string line)
    {
        var text = line.Replace("\r", string.Empty).Trim();

        // Leading block markers: headings, quotes, list bullets, ordered list numbers.
        text = Regex.Replace(text, @"^(#{1,6}\s+|>\s*|[-*+]\s+|\d+[.)]\s+)", string.Empty);

        // Task list checkbox left behind by a bullet marker.
        text = Regex.Replace(text, @"^\[[ xX]\]\s*", string.Empty);

        // A horizontal rule carries no title.
        if (Regex.IsMatch(text, @"^([-*_]\s*){3,}$"))
        {
            return string.Empty;
        }

        // Inline emphasis and code markers.
        text = text.Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("`", string.Empty)
            .Replace("~~", string.Empty);

        return text.Trim();
    }

    /// <summary>
    /// Lists the top-level bullets under every heading whose first word is
    /// <paramref name="sectionName"/>, ignoring case and any decoration such as an emoji, so
    /// <c>## hot 🔥</c> is the "hot" section. A section runs until the next heading of the same
    /// or a higher level, or a horizontal rule. Nested bullets belong to their parent and are
    /// left out, as are ticked task-list items: they are done. Each bullet reads as plain text.
    /// </summary>
    public static IReadOnlyList<string> ExtractSectionItems(string? content, string sectionName)
    {
        var items = new List<string>();
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(sectionName))
        {
            return items;
        }

        var wanted = NormalizeNameToken(sectionName);
        int? sectionLevel = null;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Replace("\r", string.Empty).TrimEnd();

            var heading = Regex.Match(line, @"^\s{0,3}(#{1,6})\s+(.*)$");
            if (heading.Success)
            {
                var level = heading.Groups[1].Value.Length;
                var firstWord = heading.Groups[2].Value
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (NormalizeNameToken(firstWord) == wanted)
                {
                    sectionLevel = level;
                }
                else if (sectionLevel.HasValue && level <= sectionLevel.Value)
                {
                    sectionLevel = null;
                }

                continue;
            }

            if (!sectionLevel.HasValue)
            {
                continue;
            }

            if (Regex.IsMatch(line, @"^\s{0,3}([-*_]\s*){3,}$"))
            {
                sectionLevel = null;
                continue;
            }

            // Top-level bullets only: a nested bullet is indented under its parent.
            if (!Regex.IsMatch(line, @"^\s?([-*+]|\d+[.)])\s+"))
            {
                continue;
            }

            if (Regex.IsMatch(line, @"^\s?([-*+]|\d+[.)])\s+\[[xX]\]"))
            {
                continue;
            }

            var text = StripMarkdown(line);
            if (text.Length > 0)
            {
                items.Add(text);
            }
        }

        return items;
    }

    /// <summary>
    /// How note titles sort: ordinal, ignoring the case of ASCII letters only. This is exactly
    /// SQLite's <c>NOCASE</c> collation, so a list sorted in memory and one sorted by the
    /// database come out in the same order.
    /// </summary>
    public static readonly IComparer<string> TitleComparer = Comparer<string>.Create(
        (a, b) => string.CompareOrdinal(LowerAscii(a), LowerAscii(b)));

    private static string LowerAscii(string? value) =>
        value is null ? string.Empty : string.Create(value.Length, value, (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });

    /// <summary>
    /// Normalises a free-form tag string to a lowercase, comma-separated, sorted list.
    /// </summary>
    public static string NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return string.Empty;
        }

        var tagList = tags
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t);

        return string.Join(",", tagList);
    }

    /// <summary>
    /// Splits a normalised tag string back into its individual tags.
    /// </summary>
    public static string[] SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return Array.Empty<string>();
        }

        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Reduces a tag or a person's name to comparable letters and digits, so that
    /// "#Badredin", "badredin" and "Badredin," all match the same person.
    /// </summary>
    public static string NormalizeNameToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Reads a date written as a tag in <c>#YYYYMMDD</c> form. Returns null when no tag
    /// in the list is a valid date.
    /// </summary>
    public static DateOnly? ParseDateTag(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            var digits = tag.Trim().TrimStart('#');
            if (digits.Length != 8 || !digits.All(char.IsDigit))
            {
                continue;
            }

            var year = int.Parse(digits[..4]);
            var month = int.Parse(digits.Substring(4, 2));
            var day = int.Parse(digits.Substring(6, 2));

            if (month is < 1 or > 12 || day < 1 || year < 1)
            {
                continue;
            }

            if (day > DateTime.DaysInMonth(year, month))
            {
                continue;
            }

            return new DateOnly(year, month, day);
        }

        return null;
    }
}
