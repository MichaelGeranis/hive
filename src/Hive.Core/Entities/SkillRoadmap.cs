namespace Hive.Core.Entities;

/// <summary>
/// A reusable roadmap of skills for a role, such as "Backend Engineer": sections of hard and soft
/// skills, in order. A <see cref="CareerPlan"/> copies its skills when it is created, so editing a
/// roadmap never changes a plan already in flight.
/// </summary>
public class SkillRoadmap
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxLibraryKeyLength = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The built-in roadmap this one was added from, or null for a roadmap made by hand.
    /// </summary>
    public string? LibraryKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SkillRoadmap() { }

    public SkillRoadmap(string name, string? description = null, string? libraryKey = null)
    {
        ValidateName(name);
        ValidateDescription(description);

        if (libraryKey is not null && (string.IsNullOrWhiteSpace(libraryKey) || libraryKey.Length > MaxLibraryKeyLength))
        {
            throw new ArgumentException($"Library key must be 1 to {MaxLibraryKeyLength} characters.", nameof(libraryKey));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        LibraryKey = libraryKey?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description)
    {
        ValidateName(name);
        ValidateDescription(description);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Roadmap name cannot be empty.", nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Roadmap name cannot exceed {MaxNameLength} characters.", nameof(name));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Roadmap description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }
    }
}
