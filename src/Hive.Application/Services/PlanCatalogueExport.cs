using System.Drawing;
using System.Globalization;
using ClosedXML.Excel;
using Hive.Core.Entities;

namespace Hive.Application.Services;

/// <summary>
/// The quarter's plan as a Plan-Catalogue spreadsheet, the way managers keep it by hand: the
/// options with priority, size and dependencies; each person's sprints with their share; then
/// capacity, mix and notes per sprint, with a legend beside it saying what the numbers mean.
/// </summary>
internal static class PlanCatalogueExport
{
    private static readonly Color Navy = Color.FromArgb(31, 56, 100);
    private static readonly Color Header = Color.FromArgb(184, 214, 236);
    private static readonly Color Shade = Color.FromArgb(242, 242, 242);
    private static readonly Color AwayFill = Color.FromArgb(217, 217, 217);
    private static readonly Color GridLine = Color.FromArgb(217, 217, 217);
    private static readonly Color Muted = Color.FromArgb(118, 118, 118);
    private static readonly Color Link = Color.FromArgb(17, 85, 204);
    private static readonly Color PartlyAway = Color.FromArgb(166, 118, 0);
    private static readonly Color MostlyAway = Color.FromArgb(192, 0, 0);

    // The lanes keep their colour whatever the initiative says: Maintenance red, Tech roadmap blue
    internal static readonly Color MaintenanceColor = Color.FromArgb(240, 188, 188);
    internal static readonly Color TechRoadmapColor = Color.FromArgb(184, 206, 245);

    internal static readonly Color CommittedColor = Color.FromArgb(200, 226, 190);
    internal static readonly Color ReadyColor = Color.FromArgb(255, 231, 168);

    private const int OptionColumns = 7;
    private const double NameWidth = 18;
    private const double ColumnWidth = 32;
    private const double LegendTermWidth = 24;
    private const double LegendWidth = 64;
    private const double LineHeight = 15;
    private const double HeaderHeight = LineHeight + 5;

    // Every row of the tables is one height, three lines; the sprint notes get room to breathe
    private const double RowHeight = LineHeight * 3;
    private const double NotesHeight = LineHeight * 6;

    public static byte[] Build(
        QuarterPlan plan,
        IReadOnlyList<DirectReport> people,
        IReadOnlyList<ExternalDependency> externalDependencies,
        IReadOnlyList<SprintGoal> sprintNotes)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add($"{plan.Quarter.Name} Plan-Catalogue");
        sheet.ShowGridLines = false;
        var sprintCount = Math.Max(plan.Sprints.Count, 6);
        var lastColumn = sprintCount + 1;
        var initiatives = plan.Initiatives.ToDictionary(i => i.Id);

        sheet.Column(1).Width = NameWidth;
        for (var c = 2; c <= lastColumn; c++) sheet.Column(c).Width = ColumnWidth;

        // Title and what the quarter holds
        var row = 1;
        var title = sheet.Range(row, 1, row, lastColumn).Merge();
        title.FirstCell().Value = $"{plan.Quarter.Name} plan";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = XLColor.White;
        Fill(title, Navy);
        title.Style.Alignment.Indent = 1;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(row).Height = 30;
        row++;

        // Options
        string[] headers = { "Priority", "Option", "Size", "Status", "Dependencies", "Notes" };
        for (var c = 0; c < headers.Length; c++) sheet.Cell(row, c + 1).Value = headers[c];
        sheet.Range(row, 6, row, OptionColumns).Merge();
        StyleHeader(sheet.Range(row, 1, row, OptionColumns));
        var optionsTop = row;
        row++;

        foreach (var option in plan.PriorityOrder()
                     .Concat(plan.Initiatives.Where(i => i.Status == InitiativeStatus.Dropped && !i.IsBucket))
                     .Concat(plan.Initiatives.Where(i => i.IsBucket)))
        {
            sheet.Cell(row, 1).Value = option.IsBucket ? "-" : option.Kind == InitiativeKind.Leftover ? "Carry-over" : option.ArrivedOn.HasValue ? "NEW" : option.Rank.ToString(CultureInfo.InvariantCulture);

            var name = sheet.Cell(row, 2);
            name.Value = option.Name;
            Fill(name.AsRange(), ColorOf(option));
            if (Uri.TryCreate(option.Url, UriKind.Absolute, out var url))
            {
                name.SetHyperlink(new XLHyperlink(url));
                name.Style.Font.Underline = XLFontUnderlineValues.Single;
                name.Style.Font.FontColor = Xl(Link);
            }

            sheet.Cell(row, 3).Value = option.IsBucket ? $"{plan.Quarter.GetMixPercent(option.WorkType)}%" : Size(option.TshirtSize, plan.Estimate(option));

            var status = sheet.Cell(row, 4);
            status.Value = option.IsBucket ? "Lane" : StatusLabel(option.Status);
            Fill(status.AsRange(), option.IsBucket ? Shade : StatusColor(option.Status));
            if (option.Status == InitiativeStatus.Dropped) sheet.Range(row, 1, row, OptionColumns).Style.Font.FontColor = Xl(Muted);

            sheet.Cell(row, 5).Value = string.Join(", ", externalDependencies.Where(d => d.InitiativeId == option.Id).Select(d => d.Team).Distinct());

            var notes = new List<string>();
            if (option.HasReadinessIssues) notes.Add($"Pending: {PlanningInsights.DescribeReadiness(option.Readiness)}");
            notes.AddRange(externalDependencies.Where(d => d.InitiativeId == option.Id && d.IsOpen)
                .Select(d => $"Needs coordination with {d.Team}{(string.IsNullOrEmpty(d.Need) ? "" : $": {d.Need}")}"));
            if (!string.IsNullOrWhiteSpace(option.DecisionNote)) notes.Add(option.DecisionNote);
            if (!string.IsNullOrWhiteSpace(option.Description)) notes.Add(option.Description);
            var notesCell = sheet.Range(row, 6, row, OptionColumns).Merge();
            notesCell.FirstCell().Value = string.Join("\n", notes);
            Fill(sheet.Range(row, 5, row, OptionColumns), Shade);

            row++;
        }

        Grid(sheet.Range(optionsTop, 1, row - 1, OptionColumns));
        // Room for the options' legend before the people table starts
        row = Math.Max(row + 1, optionsTop + OptionsLegend.Length + 2);

        // By person
        var gridTop = row;
        sheet.Cell(row, 1).Value = "Sprint";
        for (var i = 0; i < plan.Sprints.Count; i++)
        {
            var sprint = plan.Sprints[i];
            var cell = sheet.Cell(row, i + 2);
            if (sprint.StartDate.HasValue && sprint.EndDate.HasValue)
            {
                var text = cell.CreateRichText();
                text.AddText($"Sprint {sprint.SprintNumber}").SetBold(true);
                text.AddText($"  {sprint.StartDate.Value.ToString("dd/MM", CultureInfo.InvariantCulture)}–{sprint.EndDate.Value.ToString("dd/MM", CultureInfo.InvariantCulture)}")
                    .SetBold(false)
                    .SetFontSize(9)
                    .SetFontColor(Xl(Muted));
            }
            else
            {
                cell.Value = sprint.Name;
            }
        }

        StyleHeader(sheet.Range(row, 1, row, lastColumn));
        row++;

        // Someone away for the whole quarter brings nothing to it, so they have no row; the
        // people who are not direct reports come after the direct reports
        var names = ShortNames(plan.Roster.Select(r => r.Person).ToList());
        foreach (var entry in plan.Roster
                     .Where(r => plan.Sprints.Count == 0 || plan.Sprints.Any(sp => plan.Availability(r.Person.Id, sp.Id) > 0))
                     .OrderBy(r => r.Person.IsDirect ? 0 : 1)
                     .ThenBy(r => r.Person.FullName))
        {
            var name = names[entry.Person.Id];
            sheet.Cell(row, 1).Value = entry.CapacityPercent < 100 ? $"{name} ({entry.CapacityPercent}%)" : name;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            for (var i = 0; i < plan.Sprints.Count; i++)
            {
                var sprint = plan.Sprints[i];
                var cell = sheet.Cell(row, i + 2);
                var shares = plan.Allocations
                    .Where(a => a.DirectReportId == entry.Person.Id && a.SprintId == sprint.Id && initiatives.ContainsKey(a.InitiativeId))
                    .OrderByDescending(a => a.Percent)
                    .ToList();
                var available = (int)Math.Round(plan.Availability(entry.Person.Id, sprint.Id) * 100);

                var parts = shares.Select(a => shares.Count > 1 ? $"{initiatives[a.InitiativeId].Name} {a.Percent}%" : initiatives[a.InitiativeId].Name).ToList();
                if (parts.Count > 0 || available < 100)
                {
                    var text = cell.CreateRichText();
                    for (var p = 0; p < parts.Count; p++) text.AddText(p == 0 ? parts[p] : $"\n{parts[p]}");
                    if (available < 100)
                    {
                        text.AddText($"{(parts.Count > 0 ? "\n" : "")}{(available == 0 ? "Away" : $"{available}% available")}")
                            .SetFontSize(9)
                            .SetItalic(true)
                            .SetFontColor(Xl(available == 0 ? Muted : available < 50 ? MostlyAway : PartlyAway));
                    }
                }

                if (available == 0) Fill(cell.AsRange(), AwayFill);
                else if (shares.Count > 0) Fill(cell.AsRange(), ColorOf(initiatives[shares[0].InitiativeId]));
            }

            row++;
        }

        sheet.Cell(row, 1).Value = "Capacity";
        sheet.Cell(row + 1, 1).Value = "Mix (P / T / M)";
        sheet.Cell(row + 2, 1).Value = "Notes";
        for (var i = 0; i < plan.Sprints.Count; i++)
        {
            var sprint = plan.Sprints[i];
            var supply = plan.SprintSupply(sprint.Id);
            var full = plan.SprintFullSupply();
            sheet.Cell(row, i + 2).Value = full > 0 ? $"{supply:0.#} of {full:0.#} ({supply / full:P0})" : "";
            var mix = plan.PlannedMix(sprint.Id);
            sheet.Cell(row + 1, i + 2).Value = $"P {mix.ProductRoadmap}% · T {mix.TechRoadmap}% · M {mix.Maintenance}%";
            var notes = sprintNotes.FirstOrDefault(n => n.SprintId == sprint.Id)?.Notes ?? string.Empty;
            sheet.Cell(row + 2, i + 2).Value = notes;
        }

        sheet.Range(row, 1, row + 2, 1).Style.Font.Bold = true;
        sheet.Range(row, 1, row + 2, 1).Style.Font.FontColor = Xl(Muted);
        Fill(sheet.Range(row, 1, row + 2, lastColumn), Shade);
        var notesRow = row + 2;
        Grid(sheet.Range(gridTop, 1, notesRow, lastColumn));

        for (var r = optionsTop; r <= notesRow; r++)
        {
            sheet.Row(r).Height =
                r == notesRow ? NotesHeight :
                r == optionsTop || r == gridTop ? HeaderHeight :
                r == gridTop - 1 ? LineHeight + 3 :
                RowHeight;
        }

        // The legend sits beside the table it explains
        var legendColumn = lastColumn + 2;
        sheet.Column(legendColumn).Width = LegendTermWidth;
        sheet.Column(legendColumn + 1).Width = LegendWidth;
        var legendBottom = Math.Max(
            Legend(sheet, optionsTop, legendColumn, "Options", OptionsLegend),
            Legend(sheet, gridTop, legendColumn, "People and sprints", SprintsLegend));
        for (var r = optionsTop; r <= legendBottom; r++)
        {
            // A short table leaves its legend running into a thin row: give that row room
            if (r != optionsTop && r != gridTop && sheet.Row(r).Height < RowHeight && sheet.Cell(r, legendColumn).GetString().Length > 0)
            {
                sheet.Row(r).Height = RowHeight;
            }
        }

        // Text reads from the top-left of every cell, wrapped
        var body = sheet.Range(optionsTop, 1, Math.Max(notesRow, legendBottom), lastColumn + 3);
        body.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        body.Style.Alignment.WrapText = true;
        sheet.Range(gridTop, 2, gridTop, lastColumn).Style.Alignment.WrapText = false;

        sheet.SheetView.Freeze(1, 1);

        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 1);
        sheet.PageSetup.SetColumnsToRepeatAtLeft(1, 1);
        sheet.PageSetup.Margins.Left = sheet.PageSetup.Margins.Right = 0.4;
        sheet.PageSetup.Margins.Top = sheet.PageSetup.Margins.Bottom = 0.5;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static readonly (string Term, string Meaning)[] OptionsLegend =
    {
        ("Priority", "The PM's order: 1 matters most. Carry-over: left from last quarter. NEW: arrived during the quarter. -: a standing lane."),
        ("Size", "T-shirt size and the dev-sprints it stands for. A dev-sprint is one person for one whole sprint."),
        ("Lane %", "On the Maintenance and Tech roadmap rows: the share of the team's supply the lane holds back."),
    };

    private static readonly (string Term, string Meaning)[] SprintsLegend =
    {
        ("Engineer (75%)", "Planned capacity: the share of their time the plan counts on. No % means 100%."),
        ("Product option 50%", "The share of that person's sprint given to the option. An option on its own takes the whole sprint."),
        ("80% available", "The share of the sprint's working days they are there, after leave and public holidays. Amber below 100%, red below 50%."),
        ("Cell colour", "The colour of the option with the biggest share. Maintenance is always red, Tech roadmap always blue."),
        ("Capacity", "Dev-sprints the team brings to the sprint after leave (availability × planned capacity, added up), of what it would bring with nobody away (planned capacities added up)."),
        ("Mix (P / T / M)", "How the sprint's planned effort splits between Productisation, Tech Optimisation and Maintenance."),
    };

    /// <summary>What the numbers mean: a header and one row per term. Returns the last row it uses.</summary>
    private static int Legend(IXLWorksheet sheet, int top, int column, string title, (string Term, string Meaning)[] entries)
    {
        sheet.Cell(top, column).Value = title;
        sheet.Cell(top, column + 1).Value = "What it means";
        StyleHeader(sheet.Range(top, column, top, column + 1));

        var row = top + 1;
        foreach (var (term, meaning) in entries)
        {
            sheet.Cell(row, column).Value = term;
            sheet.Cell(row, column).Style.Font.Bold = true;
            sheet.Cell(row, column + 1).Value = meaning;
            row++;
        }

        Grid(sheet.Range(top, column, row - 1, column + 1));
        return row - 1;
    }

    /// <summary>
    /// First names, with the surname's initial for anyone whose first name another person on the
    /// plan shares.
    /// </summary>
    internal static Dictionary<Guid, string> ShortNames(IReadOnlyList<DirectReport> people)
    {
        var shared = people.GroupBy(p => p.FirstName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return people.ToDictionary(
            p => p.Id,
            p => shared.Contains(p.FirstName) && p.LastName.Length > 0 ? $"{p.FirstName} {p.LastName[0]}." : p.FirstName);
    }

    /// <summary>A size with the dev-sprints it stands for: "M (2 sprints)".</summary>
    internal static string Size(string? tshirtSize, decimal? estimate) =>
        tshirtSize is null ? string.Empty :
        estimate is null ? tshirtSize :
        $"{tshirtSize} ({estimate:0.#} {(estimate == 1 ? "sprint" : "sprints")})";

    internal static string StatusLabel(InitiativeStatus status) => status switch
    {
        InitiativeStatus.InDiscovery => "In discovery",
        InitiativeStatus.InSolutionDesign => "In solution design",
        InitiativeStatus.ReadyForCommitment => "Ready for commitment",
        _ => status.ToString()
    };

    /// <summary>Committed work green, ready or stretch amber, the rest the column's grey.</summary>
    internal static Color StatusColor(InitiativeStatus status) => status switch
    {
        InitiativeStatus.Committed or InitiativeStatus.Delivered => CommittedColor,
        InitiativeStatus.ReadyForCommitment or InitiativeStatus.Stretch => ReadyColor,
        _ => Shade
    };

    /// <summary>The pastel an initiative shows in, in the options and the allocations alike.</summary>
    internal static Color ColorOf(Initiative initiative) => initiative switch
    {
        { IsBucket: true, WorkType: WorkType.Maintenance } => MaintenanceColor,
        { IsBucket: true, WorkType: WorkType.TechRoadmap } => TechRoadmapColor,
        _ => Pastel(ColorTranslator.FromHtml(initiative.Color))
    };

    private static Color Pastel(Color color) =>
        Color.FromArgb(
            color.R + (255 - color.R) * 62 / 100,
            color.G + (255 - color.G) * 62 / 100,
            color.B + (255 - color.B) * 62 / 100);

    private static XLColor Xl(Color color) => XLColor.FromArgb(color.A, color.R, color.G, color.B);

    private static void Fill(IXLRange range, Color color) => range.Style.Fill.BackgroundColor = Xl(color);

    private static void Grid(IXLRange range)
    {
        var border = range.Style.Border;
        border.TopBorder = border.BottomBorder = border.LeftBorder = border.RightBorder = XLBorderStyleValues.Thin;
        border.TopBorderColor = border.BottomBorderColor = border.LeftBorderColor = border.RightBorderColor = Xl(GridLine);
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = Xl(Navy);
        Fill(range, Header);
    }
}
