namespace Hive.Core.Entities;

/// <summary>
/// Represents application settings stored in the database.
/// </summary>
public class AppSettings
{
    public Guid Id { get; private set; }
    public string StoryPointMappings { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core / serialization
    private AppSettings() { }

    public AppSettings(string storyPointMappings)
    {
        Id = Guid.NewGuid();
        StoryPointMappings = storyPointMappings ?? "[]";
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStoryPointMappings(string storyPointMappings)
    {
        StoryPointMappings = storyPointMappings ?? "[]";
        UpdatedAt = DateTime.UtcNow;
    }
}
