namespace Hive.Core.Entities;

/// <summary>
/// Represents a parent item that groups related tasks (e.g., Epics, Features).
/// Parents can have labels that link them to Projects.
/// </summary>
public class Parent
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public int? TimeSpentMinutes { get; private set; }
    public Guid? TeamTaskId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Parent() { }

    public Parent(string name, string? labels = null, int? timeSpentMinutes = null, Guid? teamTaskId = null)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Labels = labels?.Trim() ?? string.Empty;
        TimeSpentMinutes = timeSpentMinutes;
        TeamTaskId = teamTaskId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? labels = null, int? timeSpentMinutes = null)
    {
        ValidateName(name);

        Name = name.Trim();
        Labels = labels?.Trim() ?? string.Empty;
        TimeSpentMinutes = timeSpentMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimeSpent(int? timeSpentMinutes)
    {
        TimeSpentMinutes = timeSpentMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkToTask(Guid teamTaskId)
    {
        TeamTaskId = teamTaskId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parent name cannot be empty.", nameof(name));
        }

        if (name.Length > 500)
        {
            throw new ArgumentException("Parent name cannot exceed 500 characters.", nameof(name));
        }
    }
}
