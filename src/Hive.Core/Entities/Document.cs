namespace Hive.Core.Entities;

/// <summary>
/// Represents a document or resource saved by the manager.
/// </summary>
public class Document
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Url { get; private set; }
    public string Tags { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Document() { }

    public Document(string title, string content = "", string? url = null, string? tags = null)
    {
        ValidateTitle(title);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Url = url?.Trim();
        Tags = NormalizeTags(tags);
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string content, string? url, string? tags)
    {
        ValidateTitle(title);

        Title = title.Trim();
        Content = content?.Trim() ?? string.Empty;
        Url = url?.Trim();
        Tags = NormalizeTags(tags);
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title cannot be empty.", nameof(title));
        }

        if (title.Length > 500)
        {
            throw new ArgumentException("Document title cannot exceed 500 characters.", nameof(title));
        }
    }

    private static string NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return string.Empty;
        }

        // Split by comma, trim, remove duplicates, join back
        var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return string.Join(", ", tagArray);
    }
}