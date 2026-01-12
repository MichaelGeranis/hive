namespace Hive.Core.Entities;

/// <summary>
/// Represents a project that groups related tasks (e.g., a GitHub project).
/// </summary>
public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Project() { }

    public Project(string name, string? description = null, string? labels = null, string? url = null)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Url = url?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description, string? labels = null, string? url = null)
    {
        ValidateName(name);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Labels = labels?.Trim() ?? string.Empty;
        Url = url?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Project name cannot exceed 200 characters.", nameof(name));
        }
    }
}
