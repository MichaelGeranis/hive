using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hive.Core.Entities;

/// <summary>
/// One team retrospective: a note in the Retro folder, and the day the retro was held.
/// </summary>
public sealed record Retrospective(ManagerNote Note, DateOnly Date)
{
    private static readonly string[] TitleDateFormats = ["yyyy-MM-dd", "dd/MM/yyyy"];

    /// <summary>
    /// The day a retro was held: the date its title starts with (<c>2026-09-28</c> or
    /// <c>28/09/2026</c>), or else the day the note was created.
    /// </summary>
    public static DateOnly DateOf(ManagerNote note)
    {
        var title = note.Title.Trim();
        if (title.Length >= 10 &&
            DateOnly.TryParseExact(title[..10], TitleDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateOnly.FromDateTime(note.CreatedAt);
    }

    /// <summary>
    /// The latest retros, newest first, at most <paramref name="count"/>. A note with nothing
    /// written in it is not a retro yet.
    /// </summary>
    public static IReadOnlyList<Retrospective> Latest(IEnumerable<ManagerNote> retroNotes, int count)
    {
        return retroNotes
            .Where(n => !string.IsNullOrWhiteSpace(n.Content))
            .Select(n => new Retrospective(n, DateOf(n)))
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Note.CreatedAt)
            .Take(Math.Max(count, 0))
            .ToList();
    }

    /// <summary>
    /// What a set of retros is, as they stand: which notes, on which days, last changed when.
    /// Adding, editing or removing a retro changes it, which is how a summary knows it is out
    /// of date.
    /// </summary>
    public static string Fingerprint(IEnumerable<Retrospective> retros)
    {
        var text = new StringBuilder();
        foreach (var retro in retros.OrderBy(r => r.Note.Id))
        {
            var changedAt = retro.Note.UpdatedAt ?? retro.Note.CreatedAt;
            text.Append(retro.Note.Id.ToString("N"))
                .Append('|').Append(changedAt.Ticks.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(retro.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
