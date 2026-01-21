namespace Hive.Core.Entities;

/// <summary>
/// Represents a category for grouping skills.
/// </summary>
public class SkillCategoryEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SkillCategoryEntity() { }

    public SkillCategoryEntity(string name, string description, int sortOrder = 0)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a SkillCategoryEntity with a specific ID (for seeding/migration purposes).
    /// </summary>
    public static SkillCategoryEntity CreateWithId(Guid id, string name, string description, int sortOrder = 0)
    {
        var category = new SkillCategoryEntity(name, description, sortOrder);
        category.Id = id;
        return category;
    }

    public void Update(string name, string description, int sortOrder)
    {
        ValidateName(name);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));
        }
    }
}
