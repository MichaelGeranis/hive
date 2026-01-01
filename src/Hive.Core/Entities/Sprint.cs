using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// Represents a sprint with parsed components from sprint names.
/// Sprint names follow the template: {TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}
/// Example: LP_4Q25_S6 (Team LP, Q4 2025, Sprint 6)
/// </summary>
public class Sprint
{
    private static readonly Regex SprintNamePattern = new(
        @"^(\w+)_(\d)Q(\d{2})_S(\d+)$",
        RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TeamName { get; private set; } = string.Empty;
    public int Quarter { get; private set; }
    public int Year { get; private set; }
    public int SprintNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Sprint() { }

    public Sprint(string name)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        ParseSprintName(name);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Parses sprint name following template: {TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}
    /// Example: LP_4Q25_S6 -> TeamName=LP, Quarter=4, Year=2025, SprintNumber=6
    /// </summary>
    private void ParseSprintName(string name)
    {
        var match = SprintNamePattern.Match(name.Trim());

        if (match.Success)
        {
            TeamName = match.Groups[1].Value;
            Quarter = int.Parse(match.Groups[2].Value);
            Year = 2000 + int.Parse(match.Groups[3].Value);
            SprintNumber = int.Parse(match.Groups[4].Value);
        }
        else
        {
            // Fallback for non-standard sprint names
            TeamName = name.Trim();
            Quarter = 0;
            Year = 0;
            SprintNumber = 0;
        }
    }

    public void Update(string name)
    {
        ValidateName(name);

        Name = name.Trim();
        ParseSprintName(name);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the sort order for this sprint based on year, quarter, and sprint number.
    /// Higher values are more recent sprints.
    /// </summary>
    public int GetSortOrder()
    {
        // Format: YYYYQSS (e.g., 2025406 for Q4 2025 Sprint 6)
        return (Year * 1000) + (Quarter * 100) + SprintNumber;
    }

    /// <summary>
    /// Determines if this sprint is before another sprint based on year, quarter, and sprint number.
    /// </summary>
    public bool IsBefore(Sprint other)
    {
        return GetSortOrder() < other.GetSortOrder();
    }

    /// <summary>
    /// Determines if this sprint is after another sprint based on year, quarter, and sprint number.
    /// </summary>
    public bool IsAfter(Sprint other)
    {
        return GetSortOrder() > other.GetSortOrder();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sprint name cannot be empty.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Sprint name cannot exceed 100 characters.", nameof(name));
        }
    }
}
