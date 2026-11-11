namespace Hive.Core.Entities;

/// <summary>
/// Which point in a person's time at the company a milestone marks.
/// </summary>
public enum TenureMilestoneKind
{
    SixMonths,
    Anniversary
}

/// <summary>
/// A tenure milestone: six months for a newcomer, then every full year.
/// </summary>
public sealed record TenureMilestone(TenureMilestoneKind Kind, int Years, DateOnly Date)
{
    /// <summary>
    /// How many days a milestone is shown for: the week leading up to it, the day itself included.
    /// </summary>
    public const int VisibleDays = 7;

    /// <summary>
    /// The next milestone on or after <paramref name="today"/>: the six-month mark until it has
    /// passed, then each anniversary. None for someone who has not started yet.
    /// </summary>
    public static TenureMilestone? Next(DateOnly hireDate, DateOnly today)
    {
        if (hireDate > today)
            return null;

        var sixMonths = hireDate.AddMonths(6);
        if (sixMonths >= today)
            return new TenureMilestone(TenureMilestoneKind.SixMonths, 0, sixMonths);

        var years = Math.Max(1, today.Year - hireDate.Year);
        var anniversary = hireDate.AddYears(years);
        if (anniversary < today)
        {
            years++;
            anniversary = hireDate.AddYears(years);
        }

        return new TenureMilestone(TenureMilestoneKind.Anniversary, years, anniversary);
    }

    /// <summary>
    /// The milestone to show today, or null when the next one is more than a week away.
    /// </summary>
    public static TenureMilestone? Visible(DateOnly hireDate, DateOnly today)
    {
        var next = Next(hireDate, today);
        return next != null && next.DaysUntil(today) < VisibleDays ? next : null;
    }

    public int DaysUntil(DateOnly today) => Date.DayNumber - today.DayNumber;
}
