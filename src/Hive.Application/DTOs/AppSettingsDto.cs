namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for AppSettings.
/// </summary>
public record AppSettingsDto
{
    public Guid Id { get; init; }
    public List<StoryPointMapping> StoryPointMappings { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record StoryPointMapping
{
    public int Points { get; init; }
    public decimal Hours { get; init; }
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating AppSettings.
/// </summary>
public record UpdateAppSettingsDto
{
    public List<StoryPointMapping> StoryPointMappings { get; init; } = new();
}
