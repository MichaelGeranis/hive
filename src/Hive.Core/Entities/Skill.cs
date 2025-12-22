namespace Hive.Core.Entities;

/// <summary>
/// Represents a skill or competency that can be assessed.
/// </summary>
public class Skill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SkillCategory Category { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Skill() { }

    public Skill(string name, string description, SkillCategory category)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Category = category;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description, SkillCategory category)
    {
        ValidateName(name);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Category = category;
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
}
