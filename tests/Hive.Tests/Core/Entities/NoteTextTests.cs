using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class NoteTextTests
{
    [Theory]
    [InlineData("# Heading", "Heading")]
    [InlineData("- bullet", "bullet")]
    [InlineData("1. step", "step")]
    [InlineData("- [ ] task", "task")]
    [InlineData("> quote", "quote")]
    [InlineData("**bold**", "bold")]
    public void DeriveTitle_StripsMarkdownDecoration(string content, string expected)
    {
        NoteText.DeriveTitle(content, "fallback").Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeriveTitle_WithNoContent_ReturnsTheFallback(string? content)
    {
        NoteText.DeriveTitle(content, "fallback").Should().Be("fallback");
    }

    [Fact]
    public void DeriveTitle_SkipsBlankAndRuleOnlyLines()
    {
        NoteText.DeriveTitle("\n---\n\nActual title\n", "fallback").Should().Be("Actual title");
    }

    [Fact]
    public void DeriveTitle_TruncatesAVeryLongFirstLine()
    {
        NoteText.DeriveTitle(new string('a', 250), "fallback").Length.Should().Be(200);
    }

    [Fact]
    public void NormalizeTags_LowercasesSortsAndDeduplicates()
    {
        NoteText.NormalizeTags("Growth, #Bob; growth  #Bob").Should().Be("#bob,growth");
    }

    [Fact]
    public void SplitTags_ReturnsEachTag()
    {
        NoteText.SplitTags("#bob,growth").Should().BeEquivalentTo(new[] { "#bob", "growth" });
    }

    [Theory]
    [InlineData("#Badredin", "badredin")]
    [InlineData("O'Brien", "obrien")]
    [InlineData("van der Berg", "vanderberg")]
    [InlineData(null, "")]
    public void NormalizeNameToken_KeepsOnlyLettersAndDigits(string? value, string expected)
    {
        NoteText.NormalizeNameToken(value).Should().Be(expected);
    }

    [Fact]
    public void ParseDateTag_ReadsADateWrittenAsATag()
    {
        NoteText.ParseDateTag(new[] { "#bob", "#20260911" }).Should().Be(new DateOnly(2026, 9, 11));
    }

    [Fact]
    public void ParseDateTag_AcceptsATagWithoutTheHash()
    {
        NoteText.ParseDateTag(new[] { "20260911" }).Should().Be(new DateOnly(2026, 9, 11));
    }

    [Theory]
    [InlineData("#20261301")]
    [InlineData("#20260230")]
    [InlineData("#2026091")]
    [InlineData("#notadate")]
    public void ParseDateTag_IgnoresAnythingThatIsNotADate(string tag)
    {
        NoteText.ParseDateTag(new[] { tag }).Should().BeNull();
    }

    [Fact]
    public void ParseDateTag_WithNoTags_ReturnsNull()
    {
        NoteText.ParseDateTag(Array.Empty<string>()).Should().BeNull();
    }

    [Fact]
    public void ExtractSectionItems_ReadsTopLevelBulletsUntilTheNextHeading()
    {
        var content = "# Journal\n\n## hot 🔥\n- First\n* Second\n1. Third\n\n## cold 🧊\n- Not hot";

        NoteText.ExtractSectionItems(content, "hot").Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public void ExtractSectionItems_MatchesTheHeadingByItsFirstWordIgnoringCase()
    {
        NoteText.ExtractSectionItems("### HOT items\n- One", "hot").Should().Equal("One");
        NoteText.ExtractSectionItems("## Hotfixes\n- One", "hot").Should().BeEmpty();
        NoteText.ExtractSectionItems("## Not hot\n- One", "hot").Should().BeEmpty();
    }

    [Fact]
    public void ExtractSectionItems_KeepsSubheadingsInsideTheSection()
    {
        var content = "## hot\n- One\n### Today\n- Two\n# Next\n- Three";

        NoteText.ExtractSectionItems(content, "hot").Should().Equal("One", "Two");
    }

    [Fact]
    public void ExtractSectionItems_StopsAtAHorizontalRule()
    {
        var content = "## hot\n- One\n\n---\n\n- Two";

        NoteText.ExtractSectionItems(content, "hot").Should().Equal("One");
    }

    [Fact]
    public void ExtractSectionItems_LeavesOutNestedBulletsAndTickedTasks()
    {
        var content = "## hot\n- Parent\n  - Child\n- [x] Done\n- [ ] Open\nplain text";

        NoteText.ExtractSectionItems(content, "hot").Should().Equal("Parent", "Open");
    }

    [Fact]
    public void ExtractSectionItems_StripsInlineMarkdownAndKeepsTheRestAsWritten()
    {
        var content = "## hot\r\n- **MAR-537**: #communication Contact `Sofia`\r\n";

        NoteText.ExtractSectionItems(content, "hot").Should().Equal("MAR-537: #communication Contact Sofia");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("- A bullet with no heading")]
    public void ExtractSectionItems_WithoutTheSection_ReturnsNothing(string? content)
    {
        NoteText.ExtractSectionItems(content, "hot").Should().BeEmpty();
    }

    [Theory]
    [InlineData("beta", "Alpha", 1)]
    [InlineData("alpha", "ALPHA", 0)]
    [InlineData("2026-09-28", "2026-09-09", 1)]
    [InlineData("Zebra", "_note", 1)] // lowercased, as NOCASE does; upper-casing would reverse it
    public void TitleComparer_IsOrdinalIgnoringAsciiCase(string a, string b, int expectedSign)
    {
        Math.Sign(NoteText.TitleComparer.Compare(a, b)).Should().Be(expectedSign);
    }
}
