using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SprintAvailabilityTests
{
    private static readonly Guid PersonId = Guid.NewGuid();

    private static Sprint SprintOf(DateTime start, DateTime end)
    {
        var sprint = new Sprint("MAR_4Q26_S1");
        sprint.UpdateDates(start, end);
        return sprint;
    }

    [Fact]
    public void WorkingDays_TwoWeeks_IsTen()
    {
        SprintAvailability.WorkingDays(SprintOf(new DateTime(2026, 10, 5), new DateTime(2026, 10, 18))).Should().Be(10);
    }

    [Fact]
    public void Availability_NoDates_CountsAsFullyAvailable()
    {
        SprintAvailability.Availability(new Sprint("MAR_4Q26_S1"), Array.Empty<Leave>()).Should().Be(1m);
    }

    [Fact]
    public void Availability_LeaveOverWeekendAndOutsideSprint_CountsOnlyWorkingDaysInside()
    {
        var sprint = SprintOf(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        // Thursday 1 Oct to Tuesday 6 Oct: only Monday 5 and Tuesday 6 fall in the sprint
        var leave = new Leave(PersonId, LeaveType.Vacation, new DateTime(2026, 10, 1), new DateTime(2026, 10, 6));

        SprintAvailability.LeaveDays(sprint, new[] { leave }).Should().Be(2);
        SprintAvailability.Availability(sprint, new[] { leave }).Should().Be(0.8m);
    }

    [Fact]
    public void LeaveDays_OverlappingLeaves_CountEachDayOnce()
    {
        var sprint = SprintOf(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        var vacation = new Leave(PersonId, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 7));
        var holiday = new Leave(PersonId, LeaveType.PublicHoliday, new DateTime(2026, 10, 7), new DateTime(2026, 10, 7));

        SprintAvailability.LeaveDays(sprint, new[] { vacation, holiday }).Should().Be(3);
    }
}
