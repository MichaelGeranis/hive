namespace Hive.Core.Entities;

/// <summary>
/// How much of a sprint a person is there for: its working days (Monday to Friday) less the
/// working days of their active leave, public holidays included.
/// </summary>
public static class SprintAvailability
{
    /// <summary>The working days in a sprint; zero when it has no dates.</summary>
    public static int WorkingDays(Sprint sprint)
    {
        if (!sprint.StartDate.HasValue || !sprint.EndDate.HasValue) return 0;
        return CountWorkingDays(sprint.StartDate.Value.Date, sprint.EndDate.Value.Date);
    }

    /// <summary>The working days of a sprint that the person's active leave takes up.</summary>
    public static int LeaveDays(Sprint sprint, IEnumerable<Leave> leaves)
    {
        if (!sprint.StartDate.HasValue || !sprint.EndDate.HasValue) return 0;

        var start = sprint.StartDate.Value.Date;
        var end = sprint.EndDate.Value.Date;
        var days = new HashSet<DateTime>();
        foreach (var leave in leaves.Where(l => l.Status == LeaveStatus.Active && l.OverlapsWith(start, end)))
        {
            var from = leave.StartDate.Date > start ? leave.StartDate.Date : start;
            var to = leave.EndDate.Date < end ? leave.EndDate.Date : end;
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                if (IsWorkingDay(day)) days.Add(day);
            }
        }

        return days.Count;
    }

    /// <summary>
    /// The share of the sprint the person is available, 0–1. A sprint without dates cannot be
    /// measured, so it counts as fully available.
    /// </summary>
    public static decimal Availability(Sprint sprint, IEnumerable<Leave> leaves)
    {
        var working = WorkingDays(sprint);
        if (working == 0) return 1m;

        var off = LeaveDays(sprint, leaves);
        return Math.Round(Math.Max(0, working - off) / (decimal)working, 4);
    }

    private static bool IsWorkingDay(DateTime day) =>
        day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday;

    private static int CountWorkingDays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (IsWorkingDay(day)) count++;
        }

        return count;
    }
}
