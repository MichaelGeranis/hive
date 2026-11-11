using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class RetrospectiveTests
{
    private static ManagerNote Retro(string content)
    {
        var note = ManagerNote.CreateBlank();
        note.UpdateContent(content);
        return note;
    }

    [Theory]
    [InlineData("# 2026-09-28", 2026, 9, 28)]
    [InlineData("2026-09-28 Sprint 4 retro", 2026, 9, 28)]
    [InlineData("28/09/2026", 2026, 9, 28)]
    public void DateOf_TitleStartsWithADate_ReadsIt(string content, int year, int month, int day)
    {
        Retrospective.DateOf(Retro(content)).Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void DateOf_TitleIsNotADate_IsTheDayTheNoteWasCreated()
    {
        var note = Retro("Sprint 4 retro");

        Retrospective.DateOf(note).Should().Be(DateOnly.FromDateTime(note.CreatedAt));
    }

    [Fact]
    public void Latest_NewestFirst_AtMostTheCount()
    {
        var notes = new[] { Retro("2026-07-01"), Retro("2026-09-01"), Retro("2026-08-01") };

        var latest = Retrospective.Latest(notes, 2);

        latest.Select(r => r.Date).Should().Equal(new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Latest_SkipsEmptyNotes()
    {
        var notes = new[] { ManagerNote.CreateBlank(), Retro("2026-09-01") };

        Retrospective.Latest(notes, 6).Should().ContainSingle();
    }

    [Fact]
    public void Fingerprint_SameRetros_IsTheSame()
    {
        var retros = Retrospective.Latest([Retro("2026-09-01"), Retro("2026-08-01")], 6);

        Retrospective.Fingerprint(retros).Should().Be(Retrospective.Fingerprint(retros.Reverse()));
    }

    [Fact]
    public void Fingerprint_RetroEdited_Changes()
    {
        var note = Retro("2026-09-01");
        var before = Retrospective.Fingerprint(Retrospective.Latest([note], 6));

        note.UpdateContent("2026-09-01\n- more");

        Retrospective.Fingerprint(Retrospective.Latest([note], 6)).Should().NotBe(before);
    }

    [Fact]
    public void Fingerprint_RetroAdded_Changes()
    {
        var first = Retro("2026-08-01");
        var before = Retrospective.Fingerprint(Retrospective.Latest([first], 6));

        var after = Retrospective.Fingerprint(Retrospective.Latest([first, Retro("2026-09-01")], 6));

        after.Should().NotBe(before);
    }
}
