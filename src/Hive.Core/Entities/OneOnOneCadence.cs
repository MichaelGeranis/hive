namespace Hive.Core.Entities;

/// <summary>
/// Where a direct report stands against the manager's 1:1 cadence.
/// </summary>
public enum OneOnOneCadenceStatus
{
    OnTrack,
    DueSoon,
    Overdue,
    NeverMet
}

/// <summary>
/// The rule for when a 1:1 is due: every <c>cadenceDays</c> days since the last one held.
/// </summary>
public static class OneOnOneCadence
{
    public const int DefaultDays = 14;
    public const int MinDays = 1;
    public const int MaxDays = 90;

    /// <summary>
    /// How many days before the due date a 1:1 counts as due soon.
    /// </summary>
    public const int DueSoonWindowDays = 2;

    /// <summary>
    /// Throws when a cadence is outside the range a manager can choose.
    /// </summary>
    public static void Validate(int cadenceDays)
    {
        if (cadenceDays < MinDays || cadenceDays > MaxDays)
        {
            throw new ArgumentException(
                $"The 1:1 cadence must be between {MinDays} and {MaxDays} days.", nameof(cadenceDays));
        }
    }

    /// <summary>
    /// Evaluates a person's last 1:1 held on or before <paramref name="today"/>. A 1:1 is due
    /// on the day the cadence runs out, due soon in the days just before, and overdue after.
    /// </summary>
    public static OneOnOneCadenceStatus Evaluate(DateOnly? lastMeetingDate, DateOnly today, int cadenceDays)
    {
        Validate(cadenceDays);

        if (!lastMeetingDate.HasValue)
            return OneOnOneCadenceStatus.NeverMet;

        var daysSince = today.DayNumber - lastMeetingDate.Value.DayNumber;
        if (daysSince > cadenceDays)
            return OneOnOneCadenceStatus.Overdue;
        if (daysSince >= cadenceDays - DueSoonWindowDays)
            return OneOnOneCadenceStatus.DueSoon;
        return OneOnOneCadenceStatus.OnTrack;
    }

    /// <summary>
    /// Whether the status calls for the manager to book a 1:1.
    /// </summary>
    public static bool NeedsAttention(OneOnOneCadenceStatus status) =>
        status is OneOnOneCadenceStatus.Overdue or OneOnOneCadenceStatus.NeverMet;
}
