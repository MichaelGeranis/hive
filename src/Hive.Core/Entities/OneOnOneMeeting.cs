namespace Hive.Core.Entities;

/// <summary>
/// A 1:1 with a direct report, written as a single free-form markdown note.
/// The note is the meeting record: there is no agenda field and no child notes.
/// </summary>
public class OneOnOneMeeting
{
    /// <summary>
    /// The title carried by a 1:1 whose first line is still empty.
    /// </summary>
    public const string DefaultTitle = "New 1:1";

    public Guid Id { get; private set; }

    /// <summary>
    /// The person this 1:1 was with. Null when the tags name nobody Hive knows, or name
    /// more than one person — an unlinked 1:1 is kept and shown, never silently reassigned.
    /// </summary>
    public Guid? DirectReportId { get; private set; }

    /// <summary>
    /// The day the 1:1 happened. Defaults to the day the note was started and can be
    /// moved by tagging the note with a date.
    /// </summary>
    public DateOnly MeetingDate { get; private set; }

    /// <summary>
    /// The heading of the note, derived from the first line of its content.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// What was discussed, written as markdown.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Comma-separated tags. One naming a direct report links the 1:1 to that person;
    /// one in <c>#YYYYMMDD</c> form sets the meeting date.
    /// </summary>
    public string Tags { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OneOnOneMeeting() { }

    public OneOnOneMeeting(
        DateOnly meetingDate,
        string? content = null,
        string? tags = null,
        Guid? directReportId = null)
    {
        Id = Guid.NewGuid();
        MeetingDate = meetingDate;
        Content = content ?? string.Empty;
        Title = NoteText.DeriveTitle(Content, DefaultTitle);
        Tags = NoteText.NormalizeTags(tags);
        DirectReportId = directReportId;
        CreatedAt = DateTime.UtcNow;

        ApplyDateTag();
    }

    /// <summary>
    /// Opens a blank 1:1 note for a day, ready to be written into while the meeting happens.
    /// </summary>
    public static OneOnOneMeeting CreateBlank(DateOnly meetingDate)
    {
        return new OneOnOneMeeting(meetingDate);
    }

    /// <summary>
    /// Replaces the body of the note and re-derives its title from the first line.
    /// Whitespace is preserved exactly as typed.
    /// </summary>
    public void UpdateContent(string? content)
    {
        Content = content ?? string.Empty;
        Title = NoteText.DeriveTitle(Content, DefaultTitle);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the tags. A <c>#YYYYMMDD</c> tag moves the meeting to that date; the
    /// person a tag names is resolved outside the entity, which cannot see the team.
    /// </summary>
    public void UpdateTags(string? tags)
    {
        Tags = NoteText.NormalizeTags(tags);
        ApplyDateTag();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links the 1:1 to a direct report, or leaves it unlinked when null.
    /// </summary>
    public void LinkTo(Guid? directReportId)
    {
        if (DirectReportId == directReportId)
        {
            return;
        }

        DirectReportId = directReportId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves the 1:1 to another day.
    /// </summary>
    public void SetMeetingDate(DateOnly meetingDate)
    {
        if (MeetingDate == meetingDate)
        {
            return;
        }

        MeetingDate = meetingDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list of tags as an array.
    /// </summary>
    public string[] GetTagsList() => NoteText.SplitTags(Tags);

    /// <summary>
    /// Checks if the 1:1 carries a specific tag.
    /// </summary>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(Tags))
        {
            return false;
        }

        return GetTagsList().Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the tags name nobody Hive recognises, so the 1:1 belongs to no one yet.
    /// </summary>
    public bool IsUnlinked() => !DirectReportId.HasValue;

    private void ApplyDateTag()
    {
        var tagged = NoteText.ParseDateTag(GetTagsList());
        if (tagged.HasValue)
        {
            MeetingDate = tagged.Value;
        }
    }
}
