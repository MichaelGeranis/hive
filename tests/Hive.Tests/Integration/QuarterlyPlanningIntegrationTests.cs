using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// A quarter planned end to end on the real services and the in-memory database: options,
/// knowledge, leave, the waterline, a draft, the commitment and the review.
/// </summary>
public class QuarterlyPlanningIntegrationTests : IntegrationTestBase
{
    private readonly IQuarterlyPlanningService _planning;
    private readonly IPlanningBoardService _board;
    private readonly DirectReport _alice = new("Alice", "Smith", "alice@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
    private readonly DirectReport _bob = new("Bob", "Jones", "bob@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
    private readonly Sprint _s1 = new("MAR_4Q26_S1");
    private readonly Sprint _s2 = new("MAR_4Q26_S2");

    public QuarterlyPlanningIntegrationTests()
    {
        _planning = GetService<IQuarterlyPlanningService>();
        _board = GetService<IPlanningBoardService>();

        _s1.UpdateDates(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        _s2.UpdateDates(new DateTime(2026, 10, 19), new DateTime(2026, 10, 30));
        DbContext.Sprints.TryAdd(_s1.Id, _s1);
        DbContext.Sprints.TryAdd(_s2.Id, _s2);
        DbContext.DirectReports.TryAdd(_alice.Id, _alice);
        DbContext.DirectReports.TryAdd(_bob.Id, _bob);
    }

    private async Task<Guid> CreateQuarterAsync() =>
        (await _planning.CreateQuarterAsync(new CreateQuarterDto { Year = 2026, QuarterNumber = 4 })).Id;

    [Fact]
    public async Task Board_NewQuarter_HasLanesRosterAndCapacity()
    {
        var quarterId = await CreateQuarterAsync();
        await GetService<ILeaveRepository>().AddAsync(new Leave(_alice.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9)));

        var board = await _board.GetBoardAsync(quarterId);

        board.Sprints.Select(s => s.Name).Should().Equal("MAR_4Q26_S1", "MAR_4Q26_S2");
        board.Initiatives.Should().HaveCount(2).And.OnlyContain(i => i.Kind == (int)InitiativeKind.Bucket);
        board.Roster.Should().HaveCount(2);
        board.Roster.First(r => r.DirectReportId == _alice.Id).Sprints[0].AvailablePercent.Should().Be(50);
        board.Capacity.TotalSupply.Should().Be(3.5m);
        board.Capacity.TotalDays.Should().Be(35m);
        board.Roster.First(r => r.DirectReportId == _alice.Id).TotalDays.Should().Be(15m);
        board.Capacity.OptionCapacity.Should().Be(1.75m);
        board.Checklist.HasRoster.Should().BeTrue();
    }

    [Fact]
    public async Task Board_OptionOnAProduct_SuggestsWhoKnowsIt()
    {
        var quarterId = await CreateQuarterAsync();
        var product = await GetService<IProductRepository>().AddAsync(new Product("Rights Manager"));
        var project = await GetService<IProjectRepository>().AddAsync(new Project("rights-api"));
        await GetService<IProjectProductRepository>().AddAsync(new ProjectProduct(project.Id, product.Id));
        await GetService<IProjectKnowledgeRepository>().AddAsync(new ProjectKnowledge(_bob.Id, project.Id, 4));
        await GetService<ITeamTaskRepository>().AddAsync(new TeamTask("Fix rights", assigneeId: _alice.Id, components: "Rights Manager"));

        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Rights overhaul", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "M" });
        await _planning.UpdateInitiativeAsync(optionId, new UpdateInitiativeDto { Name = "Rights overhaul", TshirtSize = "M", ProductIds = new[] { product.Id } });

        var option = (await _board.GetBoardAsync(quarterId)).Initiatives.First(i => i.Id == optionId);

        option.Products.Should().ContainSingle(p => p.Name == "Rights Manager");
        option.Suggestions.Select(s => s.Name).Should().Equal("Bob Jones", "Alice Smith");
        option.Suggestions[0].KnowledgeLevel.Should().Be(4);
        option.Suggestions[1].TaskCount.Should().Be(1);
    }

    [Fact]
    public async Task Draft_Applied_CoversOptionsAboveTheLineAndRaisesNoOverAllocation()
    {
        var quarterId = await CreateQuarterAsync();
        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Euterpe", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "M" });
        await _planning.AddInitiativeMemberAsync(optionId, new CreateInitiativeMemberDto { DirectReportId = _alice.Id, Role = (int)InitiativeRole.Lead });

        var preview = await _board.DraftAsync(quarterId, apply: false);
        (await _board.GetBoardAsync(quarterId)).Allocations.Should().BeEmpty();

        var applied = await _board.DraftAsync(quarterId, apply: true);
        var board = await _board.GetBoardAsync(quarterId);

        preview.Allocations.Should().HaveCount(applied.Allocations.Count);
        var option = board.Initiatives.First(i => i.Id == optionId);
        option.Forecast!.DeliveredInQuarter.Should().BeTrue();
        board.Allocations.Where(a => a.InitiativeId == optionId).Should().OnlyContain(a => a.DirectReportId == _alice.Id);
        board.Roster.SelectMany(r => r.Sprints).Should().OnlyContain(s => s.AllocatedPercent <= 100);
        board.Insights.Should().NotContain(i => i.Type == "NoLead");
    }

    [Fact]
    public async Task Board_CommittedOptionNotReadyWithOpenDependency_Warns()
    {
        var quarterId = await CreateQuarterAsync();
        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "MA Pubs Historicity", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "L" });
        await _planning.UpdateInitiativeAsync(optionId, new UpdateInitiativeDto { Name = "MA Pubs Historicity", Readiness = (int)ReadinessIssue.RequirementsPending });
        await _planning.AddExternalDependencyAsync(optionId, new SaveExternalDependencyDto { Team = "CORE", Need = "Release plan" });
        await _planning.ChangeInitiativeStatusAsync(optionId, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.Committed });

        var board = await _board.GetBoardAsync(quarterId);

        board.Insights.Select(i => i.Type).Should().Contain(new[] { "NotReady", "OpenDependency", "NoLead", "Unallocated" });
        board.Insights.First(i => i.Type == "NotReady").Severity.Should().Be(InsightSeverity.Critical);
        board.Checklist.NotReadyCount.Should().Be(1);
        board.Checklist.OpenDependencyCount.Should().Be(1);
    }

    [Fact]
    public async Task Board_PastWork_OffersItsMixByStoryPointsFromTheWorkCategories()
    {
        var parents = GetService<IParentRepository>();
        var tasks = GetService<ITeamTaskRepository>();
        foreach (var (key, epicName, points) in new[] { ("MAR-1", "Tech Maintenance", 40), ("MAR-2", "System Improvement", 10), ("MAR-3", "Tech Debt", 10), ("MAR-4", "Tech Roadmap", 10) })
        {
            var epic = await parents.AddAsync(Parent.FromJira(key, "Epic", epicName));
            await tasks.AddAsync(new TeamTask($"{epicName} work", storyPoints: points, parentId: epic.Id));
        }
        await tasks.AddAsync(new TeamTask("Feature", storyPoints: 30));
        var quarterId = await CreateQuarterAsync();

        var mix = (await _board.GetBoardAsync(quarterId)).Mix;

        mix.PastWorkStoryPoints.Should().Be(100);
        mix.PastWork.Should().BeEquivalentTo(new MixSharesDto { ProductRoadmap = 40, TechRoadmap = 20, Maintenance = 40 });
    }

    [Fact]
    public async Task Board_NoStoryPoints_OffersNoPastWorkMix()
    {
        await GetService<ITeamTaskRepository>().AddAsync(new TeamTask("Unestimated"));
        var quarterId = await CreateQuarterAsync();

        var mix = (await _board.GetBoardAsync(quarterId)).Mix;

        mix.PastWork.Should().BeNull();
        mix.PastWorkStoryPoints.Should().Be(0);
    }

    [Fact]
    public async Task Scenario_LeadAway_ChangesForecastWithoutSaving()
    {
        var quarterId = await CreateQuarterAsync();
        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Euterpe", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "S" });
        await _planning.SetCellsAsync(quarterId, new SetCellsDto
        {
            Cells = new[] { new CellDto { DirectReportId = _alice.Id, SprintId = _s2.Id, Shares = new[] { new CellShareDto { InitiativeId = optionId, Percent = 100 } } } }
        });

        var scenario = await _board.RunScenarioAsync(quarterId, new ScenarioRequestDto
        {
            Absences = new[] { new ScenarioAbsenceDto { DirectReportId = _alice.Id, SprintIds = new[] { _s2.Id } } }
        });

        scenario.SupplyAfter.Should().Be(scenario.SupplyBefore - 1);
        var option = scenario.Options.Single(o => o.InitiativeId == optionId);
        option.Before.DeliveredInQuarter.Should().BeTrue();
        option.After.DeliveredInQuarter.Should().BeFalse();
        (await _board.GetBoardAsync(quarterId)).Capacity.TotalSupply.Should().Be(scenario.SupplyBefore);
    }

    [Fact]
    public async Task Commit_ThenReview_ComparesWithTheBaseline()
    {
        var quarterId = await CreateQuarterAsync();
        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Euterpe", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "M" });
        await _planning.ChangeInitiativeStatusAsync(optionId, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.Committed, Note = "Top priority" });
        await _board.DraftAsync(quarterId, apply: true);

        var quarter = await _board.CommitAsync(quarterId);
        var review = await _board.GetReviewAsync(quarterId);

        quarter.CommittedAt.Should().NotBeNull();
        var option = review.Options.Single(o => o.InitiativeId == optionId);
        option.AtCommit!.Status.Should().Be((int)InitiativeStatus.Committed);
        option.AtCommit.AboveLine.Should().BeTrue();
        review.Summary.Should().Contain("Committed:").And.Contain("Euterpe (M)").And.Contain("Top priority");
    }

    [Fact]
    public async Task Calibration_DeliveredOption_ReportsActualAgainstEstimate()
    {
        var quarterId = await CreateQuarterAsync();
        var optionId = await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Euterpe", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "S" });
        await _planning.SetCellsAsync(quarterId, new SetCellsDto
        {
            Cells = new[]
            {
                new CellDto { DirectReportId = _alice.Id, SprintId = _s1.Id, Shares = new[] { new CellShareDto { InitiativeId = optionId, Percent = 100 } } },
                new CellDto { DirectReportId = _alice.Id, SprintId = _s2.Id, Shares = new[] { new CellShareDto { InitiativeId = optionId, Percent = 50 } } }
            }
        });
        await _planning.ChangeInitiativeStatusAsync(optionId, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.Committed });
        await _planning.ChangeInitiativeStatusAsync(optionId, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.Delivered });

        var calibration = await _board.GetCalibrationAsync();

        var small = calibration.Sizes.Single(s => s.Size == "S");
        small.DeliveredCount.Should().Be(1);
        small.AverageActual.Should().Be(1.5m);
        small.Ratio.Should().Be(1.5m);
    }

    [Fact]
    public async Task Export_PlannedQuarter_ProducesASpreadsheet()
    {
        var quarterId = await CreateQuarterAsync();
        await _planning.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarterId, Name = "Euterpe", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "L+", Url = "https://example.com/design" });
        await _board.DraftAsync(quarterId, apply: true);

        var bytes = await _board.ExportToExcelAsync(quarterId);

        bytes.Should().NotBeEmpty();
    }
}
