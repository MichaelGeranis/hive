using FluentAssertions;
using Hive.Application.Services;
using Hive.Core.Entities;

namespace Hive.Tests.Application.Services;

public class QuarterPlanTests
{
    private readonly Quarter _quarter = new(2026, 4);
    private readonly Sprint _s1 = new("MAR_4Q26_S1");
    private readonly Sprint _s2 = new("MAR_4Q26_S2");
    private readonly DirectReport _alice = new("Alice", "Smith", "alice@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
    private readonly DirectReport _bob = new("Bob", "Jones", "bob@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));

    private static readonly Dictionary<string, decimal> Sizes = new() { ["S"] = 0.5m, ["M"] = 1m, ["L"] = 2m };

    public QuarterPlanTests()
    {
        // Two sprints of ten working days each
        _s1.UpdateDates(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        _s2.UpdateDates(new DateTime(2026, 10, 19), new DateTime(2026, 10, 30));
    }

    private QuarterPlan Plan(
        IEnumerable<Initiative> initiatives,
        IEnumerable<Allocation>? allocations = null,
        IEnumerable<Leave>? leaves = null,
        IEnumerable<InitiativeMember>? members = null,
        int bobCapacity = 100) =>
        new(_quarter,
            new[] { _s2, _s1 },
            new[] { new RosterEntry(_alice, 100), new RosterEntry(_bob, bobCapacity) },
            initiatives,
            allocations ?? Array.Empty<Allocation>(),
            members ?? Array.Empty<InitiativeMember>(),
            leaves ?? Array.Empty<Leave>(),
            Sizes);

    // Sized options are ready for commitment: a size comes with readiness
    private Initiative Option(string name, string size, int rank, InitiativeKind kind = InitiativeKind.Option) =>
        new(_quarter.Id, name, "#8B5CF6", tshirtSize: size, kind: kind, rank: rank, status: InitiativeStatus.ReadyForCommitment);

    [Fact]
    public void Supply_LeaveAndPartTime_ReduceWhatPeopleBring()
    {
        var leave = new Leave(_alice.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9));

        var plan = Plan(Array.Empty<Initiative>(), leaves: new[] { leave }, bobCapacity: 50);

        plan.Sprints.Should().Equal(_s1, _s2);
        plan.Availability(_alice.Id, _s1.Id).Should().Be(0.5m);
        plan.LeaveDays(_alice.Id, _s1.Id).Should().Be(5);
        plan.Supply(_bob.Id, _s2.Id).Should().Be(0.5m);
        plan.TotalSupply.Should().Be(2.5m);
    }

    [Fact]
    public void Days_LeaveAndPartTime_AreSupplyTimesWorkingDays()
    {
        var leave = new Leave(_alice.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9));

        var plan = Plan(Array.Empty<Initiative>(), leaves: new[] { leave }, bobCapacity: 50);

        plan.PersonDays(_alice.Id).Should().Be(15m);
        plan.PersonDays(_bob.Id).Should().Be(10m);
        plan.TotalDays.Should().Be(25m);
    }

    [Fact]
    public void OptionCapacity_DefaultMix_IsSupplyLessTheLanesShare()
    {
        var plan = Plan(Array.Empty<Initiative>());

        plan.TotalSupply.Should().Be(4m);
        plan.Reservation(WorkType.Maintenance).Should().Be(1.2m);
        plan.Reservation(WorkType.TechRoadmap).Should().Be(0.8m);
        plan.OptionCapacity.Should().Be(2m);
    }

    [Fact]
    public void Waterline_LeftoverFirstThenRank_MarksWhatFits()
    {
        var first = Option("First", "M", 1);
        var second = Option("Second", "L", 2);
        var leftover = Option("Leftover", "S", 0, InitiativeKind.Leftover);
        var dropped = Option("Dropped", "S", 3);
        dropped.Drop();
        var lane = Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance);

        var waterline = Plan(new[] { second, first, leftover, dropped, lane }).Waterline();

        waterline.Capacity.Should().Be(2m);
        waterline.Rows.Select(r => r.Name).Should().Equal("Leftover", "First", "Second");
        waterline.Rows.Select(r => r.Cumulative).Should().Equal(0.5m, 1.5m, 3.5m);
        waterline.Rows.Select(r => r.AboveLine).Should().Equal(true, true, false);
    }

    [Fact]
    public void Waterline_UnsizedOption_NeverFitsAndAddsNothing()
    {
        var unsized = new Initiative(_quarter.Id, "In discovery", "#8B5CF6", rank: 1);
        var sized = Option("Sized", "M", 2);

        var rows = Plan(new[] { unsized, sized }).Waterline().Rows;

        rows.Select(r => (r.Name, r.AboveLine, r.Cumulative)).Should().Equal(("In discovery", false, 0m), ("Sized", true, 1m));
    }

    [Fact]
    public void Estimate_SizeWithoutMapping_IsUnknown()
    {
        var plan = Plan(Array.Empty<Initiative>());

        plan.Estimate(Option("Huge", "XXL", 1)).Should().BeNull();
        plan.Estimate(Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance)).Should().BeNull();
    }

    [Fact]
    public void Forecast_AllocationReachesSize_FinishesInThatSprint()
    {
        var option = Option("Euterpe", "L", 1);
        var leave = new Leave(_alice.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9));
        var allocations = new[]
        {
            new Allocation(option.Id, _alice.Id, _s1.Id),   // worth 0.5: half the sprint on leave
            new Allocation(option.Id, _bob.Id, _s1.Id),     // 1
            new Allocation(option.Id, _alice.Id, _s2.Id, 50) // 0.5
        };

        var forecast = Plan(new[] { option }, allocations, new[] { leave }).Forecast(option);

        forecast.Estimate.Should().Be(2m);
        forecast.Allocated.Should().Be(2m);
        forecast.CoveredPercent.Should().Be(100);
        forecast.FirstSprintId.Should().Be(_s1.Id);
        forecast.DoneSprintId.Should().Be(_s2.Id);
        forecast.DeliveredInQuarter.Should().BeTrue();
    }

    [Fact]
    public void Forecast_TooLittleAllocated_IsNotDeliveredInQuarter()
    {
        var option = Option("YT Consolidation", "L", 1);

        var forecast = Plan(new[] { option }, new[] { new Allocation(option.Id, _alice.Id, _s1.Id) }).Forecast(option);

        forecast.CoveredPercent.Should().Be(50);
        forecast.DeliveredInQuarter.Should().BeFalse();
        forecast.DoneSprintId.Should().BeNull();
    }

    [Fact]
    public void PlannedMix_ByEffort_AddsUpToHundred()
    {
        var option = Option("Euterpe", "L", 1);
        var maintenance = Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance);
        var allocations = new[]
        {
            new Allocation(option.Id, _alice.Id, _s1.Id),
            new Allocation(maintenance.Id, _bob.Id, _s1.Id)
        };

        var mix = Plan(new[] { option, maintenance }, allocations).PlannedMix();

        mix.ProductRoadmap.Should().Be(50);
        mix.Maintenance.Should().Be(50);
        mix.TechRoadmap.Should().Be(0);
    }

    [Fact]
    public void WithScenario_PersonAway_TakesTheirSupplyAndAllocation()
    {
        var option = Option("Euterpe", "M", 1);
        var plan = Plan(new[] { option }, new[] { new Allocation(option.Id, _alice.Id, _s2.Id) });

        var after = plan.WithScenario(new[] { (_alice.Id, _s2.Id) }, Array.Empty<(Guid, Guid)>());

        after.TotalSupply.Should().Be(3m);
        after.Forecast(option).Allocated.Should().Be(0m);
        plan.Forecast(option).Allocated.Should().Be(1m);
    }

    [Fact]
    public void WithScenario_OptionHeldBack_IgnoresItsEarlierAllocations()
    {
        var option = Option("Euterpe", "M", 1);
        var plan = Plan(new[] { option }, new[] { new Allocation(option.Id, _alice.Id, _s1.Id), new Allocation(option.Id, _alice.Id, _s2.Id, 50) });

        var after = plan.WithScenario(Array.Empty<(Guid, Guid)>(), new[] { (option.Id, _s2.Id) });

        after.Forecast(option).Allocated.Should().Be(0.5m);
    }

    [Fact]
    public void Draft_OptionWithLead_PlansLeadFirstFromEarliestSprint()
    {
        var option = Option("Euterpe", "M", 1);
        option.SetEarliestSprint(_s2.Id);
        var members = new[] { new InitiativeMember(option.Id, _bob.Id, InitiativeRole.Lead) };

        var drafted = Plan(new[] { option }, members: members).Draft();

        drafted.Should().ContainSingle(a => a.InitiativeId == option.Id)
            .Which.Should().Match<Allocation>(a => a.DirectReportId == _bob.Id && a.SprintId == _s2.Id && a.Percent == 100);
    }

    [Fact]
    public void Draft_OptionsThenLanes_NeverBeyondAWholeSprintOrTheOptionShare()
    {
        var option = Option("Big", "L", 1);
        var maintenance = Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance);
        var tech = Initiative.CreateBucket(_quarter.Id, WorkType.TechRoadmap);
        var existing = new Allocation(maintenance.Id, _alice.Id, _s1.Id, 25);

        var plan = Plan(new[] { option, maintenance, tech }, new[] { existing });
        var drafted = plan.Draft();
        var after = plan.WithAllocations(drafted);

        foreach (var cell in after.Allocations.GroupBy(a => (a.DirectReportId, a.SprintId)))
        {
            cell.Sum(a => a.Percent).Should().BeLessThanOrEqualTo(100);
        }

        // Each sprint gives options at most half its supply (the product share)
        foreach (var sprint in new[] { _s1, _s2 })
        {
            after.Allocations.Where(a => a.InitiativeId == option.Id && a.SprintId == sprint.Id).Sum(after.Effort)
                .Should().BeLessThanOrEqualTo(after.SprintSupply(sprint.Id) * 0.5m + 0.001m);
        }

        after.AllocatedEffort(option.Id).Should().Be(2m);
        drafted.Should().Contain(a => a.InitiativeId == maintenance.Id);
        drafted.Should().Contain(a => a.InitiativeId == tech.Id);
        drafted.Should().NotContain(a => a.Id == existing.Id);
    }

    [Fact]
    public void Draft_OptionBelowLineAndUncommitted_IsLeftOut()
    {
        var fits = Option("Fits", "L", 1);
        var tooMuch = Option("Too much", "L", 2);

        var drafted = Plan(new[] { fits, tooMuch }).Draft();

        drafted.Should().NotContain(a => a.InitiativeId == tooMuch.Id);
    }

    [Fact]
    public void ResolveRoster_NoRows_IsEveryDirectReportAtFullCapacity()
    {
        var skip = new DirectReport("Sam", "Skip", "sam@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1), isDirect: false);

        var roster = QuarterPlan.ResolveRoster(Array.Empty<QuarterMember>(), new[] { _bob, skip, _alice });

        roster.Select(r => r.Person.FirstName).Should().Equal("Alice", "Bob");
        roster.Should().OnlyContain(r => r.CapacityPercent == 100);
    }

    [Fact]
    public void ResolveRoster_Rows_AreTheQuarterOwnRoster()
    {
        var skip = new DirectReport("Sam", "Skip", "sam@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1), isDirect: false);

        var roster = QuarterPlan.ResolveRoster(new[] { new QuarterMember(_quarter.Id, skip.Id, 40) }, new[] { _alice, skip });

        roster.Should().ContainSingle().Which.Should().Match<RosterEntry>(r => r.Person == skip && r.CapacityPercent == 40);
    }
}
