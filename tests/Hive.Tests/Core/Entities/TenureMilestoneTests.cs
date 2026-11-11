using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class TenureMilestoneTests
{
    private static readonly DateOnly Today = new(2026, 9, 28);

    [Fact]
    public void Next_ForANewcomer_IsTheSixMonthMark()
    {
        var milestone = TenureMilestone.Next(new DateOnly(2026, 4, 1), Today);

        milestone.Should().Be(new TenureMilestone(TenureMilestoneKind.SixMonths, 0, new DateOnly(2026, 10, 1)));
    }

    [Fact]
    public void Next_OnTheSixMonthDay_IsThatDay()
    {
        var milestone = TenureMilestone.Next(new DateOnly(2026, 3, 28), Today);

        milestone!.Kind.Should().Be(TenureMilestoneKind.SixMonths);
        milestone.DaysUntil(Today).Should().Be(0);
    }

    [Fact]
    public void Next_AfterSixMonths_IsTheFirstAnniversary()
    {
        var milestone = TenureMilestone.Next(new DateOnly(2026, 1, 15), Today);

        milestone.Should().Be(new TenureMilestone(TenureMilestoneKind.Anniversary, 1, new DateOnly(2027, 1, 15)));
    }

    [Fact]
    public void Next_ForSomeoneLongServing_IsTheComingAnniversary()
    {
        var milestone = TenureMilestone.Next(new DateOnly(2021, 10, 2), Today);

        milestone.Should().Be(new TenureMilestone(TenureMilestoneKind.Anniversary, 5, new DateOnly(2026, 10, 2)));
    }

    [Fact]
    public void Next_WhenThisYearsAnniversaryHasPassed_IsNextYears()
    {
        var milestone = TenureMilestone.Next(new DateOnly(2021, 9, 1), Today);

        milestone.Should().Be(new TenureMilestone(TenureMilestoneKind.Anniversary, 6, new DateOnly(2027, 9, 1)));
    }

    [Fact]
    public void Next_ForSomeoneNotStartedYet_IsNone()
    {
        TenureMilestone.Next(new DateOnly(2026, 10, 5), Today).Should().BeNull();
    }

    [Theory]
    [InlineData(2021, 9, 28, true)]   // the day itself
    [InlineData(2021, 10, 4, true)]   // six days away
    [InlineData(2021, 10, 5, false)]  // a week away
    [InlineData(2021, 9, 27, false)]  // yesterday: gone
    public void Visible_OnlyInTheWeekUpToTheDay(int year, int month, int day, bool expected)
    {
        var visible = TenureMilestone.Visible(new DateOnly(year, month, day), Today);

        (visible != null).Should().Be(expected);
    }
}
