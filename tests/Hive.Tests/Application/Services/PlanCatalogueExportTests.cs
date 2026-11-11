using System.Drawing;
using ClosedXML.Excel;
using FluentAssertions;
using Hive.Application.Services;
using Hive.Core.Entities;

namespace Hive.Tests.Application.Services;

public class PlanCatalogueExportTests
{
    private const string Grey = "FFF2F2F2";

    private readonly Quarter _quarter = new(2026, 4);
    private readonly Sprint _s1 = new("MAR_4Q26_S1");
    private readonly Sprint _s2 = new("MAR_4Q26_S2");
    private readonly DirectReport _alice = new("Alice", "Smith", "alice@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
    private readonly DirectReport _bob = new("Bob", "Jones", "bob@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
    private readonly List<RosterEntry> _roster;
    private readonly Initiative _search;
    private readonly Initiative _handover;
    private readonly Initiative _maintenance;
    private readonly Initiative _techRoadmap;

    private readonly List<Leave> _leaves = new();

    private static readonly Dictionary<string, decimal> Sizes = new() { ["S"] = 1m, ["M"] = 2m };

    public PlanCatalogueExportTests()
    {
        _roster = new List<RosterEntry> { new(_alice, 100), new(_bob, 100) };
        _s1.UpdateDates(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        _s2.UpdateDates(new DateTime(2026, 10, 19), new DateTime(2026, 10, 30));
        _leaves.Add(new Leave(_alice.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 9)));
        _leaves.Add(new Leave(_bob.Id, LeaveType.Vacation, new DateTime(2026, 10, 19), new DateTime(2026, 10, 30)));
        _search = new Initiative(_quarter.Id, "Search v2", "#8B5CF6", tshirtSize: "S", rank: 1, status: InitiativeStatus.Committed);
        _handover = new Initiative(_quarter.Id, "Billing handover", "#10B981", tshirtSize: "M", rank: 2, status: InitiativeStatus.ReadyForCommitment);
        // Lanes stored in colours other than their own still export red and blue
        _maintenance = new Initiative(_quarter.Id, "Maintenance", "#00FF00", workType: WorkType.Maintenance, kind: InitiativeKind.Bucket);
        _techRoadmap = new Initiative(_quarter.Id, "Tech roadmap", "#FF0000", workType: WorkType.TechRoadmap, kind: InitiativeKind.Bucket);
    }

    /// <summary>Alice is away half of sprint 1 and Bob all of sprint 2.</summary>
    private IXLWorksheet Export()
    {
        var plan = new QuarterPlan(
            _quarter,
            new[] { _s1, _s2 },
            _roster,
            new[] { _search, _handover, _maintenance, _techRoadmap },
            new[]
            {
                new Allocation(_search.Id, _alice.Id, _s1.Id, 50),
                new Allocation(_maintenance.Id, _alice.Id, _s1.Id, 50),
                new Allocation(_handover.Id, _alice.Id, _s2.Id)
            },
            Array.Empty<InitiativeMember>(),
            _leaves,
            Sizes);

        var bytes = PlanCatalogueExport.Build(plan, _roster.Select(r => r.Person).ToList(), Array.Empty<ExternalDependency>(), Array.Empty<SprintGoal>());
        return new XLWorkbook(new MemoryStream(bytes)).Worksheet(1);
    }

    private static int LastRow(IXLWorksheet sheet) => sheet.LastRowUsed()!.RowNumber();

    private static int RowOf(IXLWorksheet sheet, int column, string text) =>
        Enumerable.Range(1, LastRow(sheet)).First(r => sheet.Cell(r, column).GetString() == text);

    private static string FillOf(IXLCell cell) =>
        cell.Style.Fill.PatternType == XLFillPatternValues.None ? string.Empty : Argb(cell.Style.Fill.BackgroundColor.Color);

    private static string Argb(Color color) => color.ToArgb().ToString("X8");

    [Fact]
    public void Build_Options_SizeCarriesItsSprintsAndPriorityStaysWhite()
    {
        var sheet = Export();
        var header = RowOf(sheet, 1, "Priority");
        var search = RowOf(sheet, 2, "Search v2");

        Enumerable.Range(1, 6).Select(c => sheet.Cell(header, c).GetString())
            .Should().Equal("Priority", "Option", "Size", "Status", "Dependencies", "Notes");
        sheet.Cell(search, 3).GetString().Should().Be("S (1 sprint)");
        FillOf(sheet.Cell(search, 1)).Should().BeNullOrEmpty();
    }

    [Fact]
    public void Build_Statuses_ReadAsWordsInTheirColour()
    {
        var sheet = Export();
        var search = RowOf(sheet, 2, "Search v2");
        var handover = RowOf(sheet, 2, "Billing handover");

        sheet.Cell(search, 4).GetString().Should().Be("Committed");
        FillOf(sheet.Cell(search, 4)).Should().Be(Argb(PlanCatalogueExport.CommittedColor));
        sheet.Cell(handover, 4).GetString().Should().Be("Ready for commitment");
        FillOf(sheet.Cell(handover, 4)).Should().Be(Argb(PlanCatalogueExport.ReadyColor));
    }

    [Fact]
    public void Build_Options_FollowEachOtherWithNoCommitLine()
    {
        var sheet = Export();

        sheet.Cell(RowOf(sheet, 2, "Search v2") + 1, 2).GetString().Should().Be("Billing handover");
    }

    [Fact]
    public void Build_Option_HasTheSameColourAsItsAllocations()
    {
        var sheet = Export();

        var allocation = sheet.Cell(RowOf(sheet, 1, "Alice"), 3);
        allocation.GetString().Should().Be("Billing handover");
        FillOf(sheet.Cell(RowOf(sheet, 2, "Billing handover"), 2)).Should().Be(FillOf(allocation)).And.Be(Argb(PlanCatalogueExport.ColorOf(_handover)));
    }

    [Fact]
    public void Build_SharedSprint_PutsEachOptionOnItsOwnLineAndAvailabilityUnder()
    {
        var sheet = Export();
        var alice = RowOf(sheet, 1, "Alice");
        var bob = RowOf(sheet, 1, "Bob");

        sheet.Cell(alice, 2).GetString().Should().Be("Search v2 50%\nMaintenance 50%\n50% available");
        sheet.Cell(alice, 2).Style.Border.BottomBorderColor.Color.ToArgb().ToString("X8").Should().NotBe("FFFF0000");
        sheet.Cell(bob, 3).GetString().Should().Be("Away");
    }

    [Fact]
    public void Build_Lanes_AreAlwaysRedAndBlue()
    {
        var sheet = Export();

        FillOf(sheet.Cell(RowOf(sheet, 2, "Maintenance"), 2)).Should().Be(Argb(PlanCatalogueExport.MaintenanceColor));
        FillOf(sheet.Cell(RowOf(sheet, 2, "Tech roadmap"), 2)).Should().Be(Argb(PlanCatalogueExport.TechRoadmapColor));
    }

    [Fact]
    public void Build_SprintSummaryRows_ReadPlainlyOnGrey()
    {
        var sheet = Export();
        var capacity = RowOf(sheet, 1, "Capacity");

        sheet.Cell(capacity, 3).GetString().Should().MatchRegex(@"^1 of 2 \(50\s?%\)$");
        sheet.Cell(capacity + 1, 1).GetString().Should().Be("Mix (P / T / M)");
        sheet.Cell(capacity + 1, 3).GetString().Should().Be("P 100% · T 0% · M 0%");
        sheet.Cell(capacity + 2, 1).GetString().Should().Be("Notes");
        new[] { capacity, capacity + 1, capacity + 2 }.Select(r => FillOf(sheet.Cell(r, 7))).Should().OnlyContain(f => f == Grey);
        FillOf(sheet.Cell(RowOf(sheet, 2, "Search v2"), 5)).Should().Be(Grey);
    }

    [Fact]
    public void Build_Sheet_HasNoGridlinesFrozenHeadingsAndPrintsLandscapeOnePageWide()
    {
        var sheet = Export();

        sheet.ShowGridLines.Should().BeFalse();
        sheet.SheetView.SplitRow.Should().Be(1);
        sheet.SheetView.SplitColumn.Should().Be(1);
        sheet.PageSetup.PageOrientation.Should().Be(XLPageOrientation.Landscape);
        sheet.PageSetup.PagesWide.Should().Be(1);
    }

    [Fact]
    public void Build_Rows_ShareOneHeightButTheHeadersAndNotes()
    {
        var sheet = Export();
        var sprints = RowOf(sheet, 1, "Sprint");
        var body = sheet.Row(RowOf(sheet, 2, "Search v2")).Height;

        sheet.Cell(2, 1).GetString().Should().Be("Priority");
        new[] { RowOf(sheet, 1, "Alice"), RowOf(sheet, 1, "Capacity") }.Select(r => sheet.Row(r).Height).Should().OnlyContain(h => h == body);
        sheet.Row(2).Height.Should().BeLessThan(body);
        sheet.Row(sprints).Height.Should().Be(sheet.Row(2).Height);
        sheet.Cell(sprints, 2).GetString().Should().Be("Sprint 1  05/10–16/10");
        sheet.Row(RowOf(sheet, 1, "Notes")).Height.Should().BeGreaterThan(body);
    }

    [Fact]
    public void Build_Columns_NameNarrowerThanTheSprintsWhichShareOneWidth()
    {
        var sheet = Export();

        new[] { 2, 3, 4, 5, 6, 7 }.Select(c => sheet.Column(c).Width).Distinct().Should().ContainSingle();
        sheet.Column(1).Width.Should().BeLessThan(sheet.Column(2).Width);
    }

    [Fact]
    public void Build_People_ShowTheirFirstNameAndCapacityWithOthersAfterDirectReports()
    {
        var aaron = new DirectReport("Aaron", "Zed", "aaron@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1), isDirect: false);
        _roster.Add(new RosterEntry(aaron, 80));

        var sheet = Export();
        var first = RowOf(sheet, 1, "Sprint") + 1;

        Enumerable.Range(first, 3).Select(r => sheet.Cell(r, 1).GetString()).Should().Equal("Alice", "Bob", "Aaron (80%)");
    }

    [Fact]
    public void Build_Legend_SitsBesideEachTable()
    {
        var sheet = Export();
        const int legend = 9;

        RowOf(sheet, legend, "Options").Should().Be(RowOf(sheet, 1, "Priority"));
        RowOf(sheet, legend, "People and sprints").Should().Be(RowOf(sheet, 1, "Sprint"));
        sheet.Cell(RowOf(sheet, legend, "Capacity"), legend + 1).GetString().Should().Contain("nobody away");
    }

    [Fact]
    public void Build_PersonAwayAllQuarter_HasNoRow()
    {
        _leaves.Add(new Leave(_bob.Id, LeaveType.Vacation, new DateTime(2026, 10, 5), new DateTime(2026, 10, 16)));

        var sheet = Export();

        Enumerable.Range(1, LastRow(sheet)).Select(r => sheet.Cell(r, 1).GetString()).Should().NotContain("Bob");
        RowOf(sheet, 1, "Alice").Should().BePositive();
    }

    [Fact]
    public void ShortNames_SharedFirstName_AddsTheSurnameInitial()
    {
        var other = new DirectReport("Alice", "Brown", "alice.b@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));

        var names = PlanCatalogueExport.ShortNames(new[] { _alice, other, _bob });

        names.Values.Should().BeEquivalentTo("Alice S.", "Alice B.", "Bob");
    }

    [Theory]
    [InlineData("M", 2.0, "M (2 sprints)")]
    [InlineData("S", 1.0, "S (1 sprint)")]
    [InlineData("XL", null, "XL")]
    [InlineData(null, null, "")]
    public void Size_WithAndWithoutMapping_ReadsAsSizeAndSprints(string? size, double? sprints, string expected) =>
        PlanCatalogueExport.Size(size, (decimal?)sprints).Should().Be(expected);
}
