namespace Hive.Application.DTOs;

/// <summary>
/// The retro summary for the dashboard: whether it can run, the retros it reads, and what the
/// model made of them.
/// </summary>
public record RetroSummaryDto
{
    public bool IsEnabled { get; init; }

    /// <summary>The chosen model has what it needs (Claude needs a key).</summary>
    public bool IsConfigured { get; init; }

    /// <summary>A folder named Retro exists in Notes.</summary>
    public bool HasRetroFolder { get; init; }

    /// <summary>The latest retros, newest first: the ones the summary reads.</summary>
    public IReadOnlyList<RetroNoteDto> Retros { get; init; } = Array.Empty<RetroNoteDto>();

    /// <summary>Null until a summary has been made.</summary>
    public string? Summary { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<RetroActionItemDto> ActionItems { get; init; } = Array.Empty<RetroActionItemDto>();
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();

    public string? Model { get; init; }
    public int RetrosAnalyzed { get; init; }
    public DateTime? AnalyzedAt { get; init; }

    /// <summary>The summary read the retros as they stand now.</summary>
    public bool IsUpToDate { get; init; }

    /// <summary>Why the summary could not be updated, safe to show the manager.</summary>
    public string? Error { get; init; }
}

/// <summary>One retro note, to link to from the summary.</summary>
public record RetroNoteDto
{
    public Guid NoteId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateOnly RetroDate { get; init; }
}

/// <summary>Something the team agreed to do in a retro.</summary>
public record RetroActionItemDto
{
    public string Text { get; init; } = string.Empty;
    public DateOnly? RetroDate { get; init; }

    /// <summary>Empty when the notes name nobody.</summary>
    public string Owner { get; init; } = string.Empty;
}
