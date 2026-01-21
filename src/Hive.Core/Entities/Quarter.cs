namespace Hive.Core.Entities;

/// <summary>
/// Represents a planning quarter (Q1-Q4 of a year).
/// </summary>
public class Quarter
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int QuarterNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public QuarterStatus Status { get; private set; }
    public string OkrReference { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Quarter() { }

    public Quarter(int year, int quarterNumber, string? okrReference = null)
    {
        ValidateYear(year);
        ValidateQuarterNumber(quarterNumber);

        Id = Guid.NewGuid();
        Year = year;
        QuarterNumber = quarterNumber;
        Name = $"Q{quarterNumber} {year}";
        Status = QuarterStatus.Planning;
        OkrReference = okrReference?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? okrReference)
    {
        OkrReference = okrReference?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Status == QuarterStatus.Completed)
        {
            throw new InvalidOperationException("Cannot activate a completed quarter.");
        }

        Status = QuarterStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == QuarterStatus.Planning)
        {
            throw new InvalidOperationException("Cannot complete a quarter that is still in planning status.");
        }

        Status = QuarterStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetToPlanning()
    {
        Status = QuarterStatus.Planning;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateYear(int year)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ArgumentException("Year must be between 2000 and 2100.", nameof(year));
        }
    }

    private static void ValidateQuarterNumber(int quarterNumber)
    {
        if (quarterNumber < 1 || quarterNumber > 4)
        {
            throw new ArgumentException("Quarter number must be between 1 and 4.", nameof(quarterNumber));
        }
    }
}
