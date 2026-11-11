using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class RetroSummaryTests
{
    private static RetroSummary Create(string summary = "The team shipped.", int retros = 3, string fingerprint = "ABC") =>
        new(summary, "[\"w\"]", "[]", "[\"s\"]", "llama3.1:8b", retros, new DateOnly(2026, 9, 28), fingerprint);

    [Fact]
    public void Constructor_ValidInput_SetsEverything()
    {
        var summary = Create();

        summary.Id.Should().NotBeEmpty();
        summary.Summary.Should().Be("The team shipped.");
        summary.WarningsJson.Should().Be("[\"w\"]");
        summary.SuggestionsJson.Should().Be("[\"s\"]");
        summary.Model.Should().Be("llama3.1:8b");
        summary.RetrosAnalyzed.Should().Be(3);
        summary.LatestRetroDate.Should().Be(new DateOnly(2026, 9, 28));
        summary.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptySummary_Throws(string text)
    {
        var act = () => Create(summary: text);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NoRetros_Throws()
    {
        var act = () => Create(retros: 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NoFingerprint_Throws()
    {
        var act = () => Create(fingerprint: " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LongSummary_IsTruncated()
    {
        var summary = Create(summary: new string('a', RetroSummary.MaxSummaryLength + 100));

        summary.Summary.Length.Should().Be(RetroSummary.MaxSummaryLength);
        summary.Summary.Should().EndWith("…");
    }

    [Fact]
    public void Constructor_ListTooLong_Throws()
    {
        var act = () => new RetroSummary(
            "ok", new string('x', RetroSummary.MaxJsonLength + 1), "[]", "[]", "m", 1, new DateOnly(2026, 9, 28), "ABC");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_BlankList_IsAnEmptyList()
    {
        var summary = new RetroSummary("ok", "", " ", "", "m", 1, new DateOnly(2026, 9, 28), "ABC");

        summary.WarningsJson.Should().Be("[]");
        summary.ActionItemsJson.Should().Be("[]");
        summary.SuggestionsJson.Should().Be("[]");
    }

    [Fact]
    public void Update_ReplacesTheAnalysis_KeepsTheIdentity()
    {
        var summary = Create();
        var id = summary.Id;

        summary.Update("New reading.", "[]", "[]", "[]", "Claude", 5, new DateOnly(2026, 10, 12), "DEF");

        summary.Id.Should().Be(id);
        summary.Summary.Should().Be("New reading.");
        summary.Model.Should().Be("Claude");
        summary.RetrosAnalyzed.Should().Be(5);
        summary.SourceFingerprint.Should().Be("DEF");
    }

    [Fact]
    public void IsStaleFor_ComparesTheFingerprint()
    {
        var summary = Create(fingerprint: "ABC");

        summary.IsStaleFor("ABC").Should().BeFalse();
        summary.IsStaleFor("DEF").Should().BeTrue();
    }
}
