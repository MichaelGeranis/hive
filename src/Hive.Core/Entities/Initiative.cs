namespace Hive.Core.Entities;

/// <summary>
/// Represents a work initiative/option for a quarter.
/// </summary>
public class Initiative
{
    public Guid Id { get; private set; }
    public Guid QuarterId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public string TshirtSize { get; private set; } = "M";
    public string Url { get; private set; } = string.Empty;
    public WorkType WorkType { get; private set; } = WorkType.ProductRoadmap;
    public Guid? StartSprintId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private static readonly string[] ValidTshirtSizes = { "S", "M", "L", "XL" };

    // Available colors for initiatives - used by service to assign unique colors
    public static readonly string[] AvailableColors =
    {
        "#3B82F6", // Blue
        "#10B981", // Green
        "#F59E0B", // Amber
        "#EF4444", // Red
        "#8B5CF6", // Purple
        "#EC4899", // Pink
        "#06B6D4", // Cyan
        "#F97316", // Orange
        "#14B8A6", // Teal
        "#6366F1", // Indigo
        "#84CC16", // Lime
        "#A855F7", // Violet
    };

    private Initiative() { }

    public Initiative(
        Guid quarterId,
        string name,
        string color,
        string? description = null,
        Guid? projectId = null,
        string? tshirtSize = null,
        string? url = null,
        WorkType workType = WorkType.ProductRoadmap,
        Guid? startSprintId = null)
    {
        ValidateName(name);
        ValidateColor(color);
        ValidateTshirtSize(tshirtSize);
        ValidateUrl(url);

        Id = Guid.NewGuid();
        QuarterId = quarterId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Color = color.Trim();
        ProjectId = projectId;
        TshirtSize = string.IsNullOrWhiteSpace(tshirtSize) ? "M" : tshirtSize.Trim().ToUpperInvariant();
        Url = url?.Trim() ?? string.Empty;
        WorkType = workType;
        StartSprintId = startSprintId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string? description,
        string? color,
        Guid? projectId,
        string? tshirtSize = null,
        string? url = null,
        WorkType? workType = null,
        Guid? startSprintId = null,
        bool clearStartSprint = false)
    {
        ValidateName(name);
        ValidateColor(color);
        ValidateTshirtSize(tshirtSize);
        ValidateUrl(url);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Color = string.IsNullOrWhiteSpace(color) ? Color : color.Trim();
        ProjectId = projectId;
        TshirtSize = string.IsNullOrWhiteSpace(tshirtSize) ? TshirtSize : tshirtSize.Trim().ToUpperInvariant();
        Url = url?.Trim() ?? string.Empty;
        if (workType.HasValue) WorkType = workType.Value;
        if (clearStartSprint) StartSprintId = null;
        else if (startSprintId.HasValue) StartSprintId = startSprintId.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToSprint(Guid? startSprintId)
    {
        StartSprintId = startSprintId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Initiative name cannot be empty.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Initiative name cannot exceed 200 characters.", nameof(name));
        }
    }

    private static void ValidateColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return;

        // Validate hex color format (#RRGGBB or #RGB)
        var trimmed = color.Trim();
        if (!trimmed.StartsWith("#"))
        {
            throw new ArgumentException("Color must be a hex color starting with #.", nameof(color));
        }

        var hex = trimmed.Substring(1);
        if (hex.Length != 3 && hex.Length != 6)
        {
            throw new ArgumentException("Color must be in #RGB or #RRGGBB format.", nameof(color));
        }

        if (!hex.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
        {
            throw new ArgumentException("Color must contain only hex characters.", nameof(color));
        }
    }

    private static void ValidateTshirtSize(string? tshirtSize)
    {
        if (string.IsNullOrWhiteSpace(tshirtSize)) return;

        var size = tshirtSize.Trim().ToUpperInvariant();
        if (!ValidTshirtSizes.Contains(size))
        {
            throw new ArgumentException($"T-shirt size must be one of: {string.Join(", ", ValidTshirtSizes)}.", nameof(tshirtSize));
        }
    }

    private static void ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var trimmed = url.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("URL cannot exceed 500 characters.", nameof(url));
        }

        // Basic URL validation
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be a valid HTTP or HTTPS URL.", nameof(url));
        }
    }
}
