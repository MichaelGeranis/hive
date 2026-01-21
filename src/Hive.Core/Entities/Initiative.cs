namespace Hive.Core.Entities;

/// <summary>
/// Represents a work initiative/option for a quarter, derived from OKRs.
/// </summary>
public class Initiative
{
    public Guid Id { get; private set; }
    public Guid QuarterId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public InitiativeStatus Status { get; private set; }
    public InitiativePriority Priority { get; private set; }
    public string OkrObjective { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Initiative() { }

    public Initiative(
        Guid quarterId,
        string name,
        string? description = null,
        InitiativePriority priority = InitiativePriority.Medium,
        string? okrObjective = null,
        string? color = null,
        Guid? projectId = null)
    {
        ValidateName(name);
        ValidateColor(color);

        Id = Guid.NewGuid();
        QuarterId = quarterId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = InitiativeStatus.Planned;
        Priority = priority;
        OkrObjective = okrObjective?.Trim() ?? string.Empty;
        Color = string.IsNullOrWhiteSpace(color) ? GenerateDefaultColor() : color.Trim();
        ProjectId = projectId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string? description,
        InitiativePriority priority,
        string? okrObjective,
        string? color,
        Guid? projectId)
    {
        ValidateName(name);
        ValidateColor(color);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        OkrObjective = okrObjective?.Trim() ?? string.Empty;
        Color = string.IsNullOrWhiteSpace(color) ? Color : color.Trim();
        ProjectId = projectId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(InitiativeStatus status)
    {
        Status = status;
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

    private static string GenerateDefaultColor()
    {
        // Generate a random pastel color for initiatives
        var colors = new[]
        {
            "#3B82F6", // Blue
            "#10B981", // Green
            "#F59E0B", // Amber
            "#EF4444", // Red
            "#8B5CF6", // Purple
            "#EC4899", // Pink
            "#06B6D4", // Cyan
            "#F97316", // Orange
        };
        return colors[Random.Shared.Next(colors.Length)];
    }
}
