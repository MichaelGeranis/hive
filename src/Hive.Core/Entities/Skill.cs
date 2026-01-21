namespace Hive.Core.Entities;

/// <summary>
/// Represents a skill or competency that can be assessed.
/// </summary>
public class Skill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid SkillCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Skill() { }

    public Skill(string name, string description, Guid skillCategoryId)
    {
        ValidateName(name);
        ValidateCategoryId(skillCategoryId);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SkillCategoryId = skillCategoryId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Legacy constructor for backward compatibility with enum-based categories.
    /// This is used during migration/seeding to create skills with old enum values.
    /// </summary>
    [Obsolete("Use the constructor with Guid skillCategoryId instead.")]
    public Skill(string name, string description, SkillCategory category)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        // SkillCategoryId will be set during seeding when we know the category GUIDs
        SkillCategoryId = Guid.Empty;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description, Guid skillCategoryId)
    {
        ValidateName(name);
        ValidateCategoryId(skillCategoryId);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SkillCategoryId = skillCategoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the category ID (used during migration/seeding).
    /// </summary>
    public void SetCategoryId(Guid skillCategoryId)
    {
        ValidateCategoryId(skillCategoryId);
        SkillCategoryId = skillCategoryId;
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
            throw new ArgumentException("Skill name cannot be empty.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Skill name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateCategoryId(Guid skillCategoryId)
    {
        if (skillCategoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(skillCategoryId));
        }
    }
}
