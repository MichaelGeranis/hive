using Hive.Application.DTOs;
using Hive.Application.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// The dashboard's focus panel through the real DI container: a 1:1 written and tagged on
/// the Meetings page is what moves a person off the overdue list.
/// </summary>
public class DashboardFocusIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetDashboardFocus_ReflectsTheLatestTaggedOneOnOne()
    {
        // Arrange
        var person = await GetService<IDirectReportService>().CreateAsync(new CreateDirectReportDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@test.com",
            JobTitle = "Engineer",
            Department = "Engineering",
            HireDate = new DateTime(2024, 1, 1)
        });
        await GetService<IAppSettingsService>().UpdateAsync(new UpdateAppSettingsDto { OneOnOneCadenceDays = 7 });
        var reporting = GetService<IReportingService>();

        var before = await reporting.GetDashboardFocusAsync();

        // Act
        await GetService<IOneOnOneMeetingService>().CreateBlankAsync(new CreateBlankMeetingDto
        {
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            Tags = "#ada"
        });
        var after = await reporting.GetDashboardFocusAsync();

        // Assert
        before.OneOnOnes.People.Should().ContainSingle(p => p.DirectReportId == person.Id)
            .Which.Status.Should().Be("NeverMet");
        after.OneOnOnes.CadenceDays.Should().Be(7);
        var entry = after.OneOnOnes.People.Should().ContainSingle(p => p.DirectReportId == person.Id).Subject;
        entry.Status.Should().Be("OnTrack");
        entry.DaysUntilDue.Should().Be(6);
        after.OneOnOnes.NeedsAttentionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardFocus_ListsTheHotItemsOfTheLatestJournalEntry()
    {
        // Arrange
        var notes = GetService<IManagerNoteService>();
        var journal = await GetService<INoteFolderService>().CreateAsync(new CreateNoteFolderDto { Name = "Journal" });
        var older = await notes.CreateBlankAsync(new CreateBlankNoteDto { FolderId = journal.Id });
        await notes.UpdateContentAsync(older.Id, new UpdateNoteContentDto { Content = "2026-09-27\n\n## hot\n- Yesterday" });
        var latest = await notes.CreateBlankAsync(new CreateBlankNoteDto { FolderId = journal.Id });
        await notes.UpdateContentAsync(latest.Id, new UpdateNoteContentDto
        {
            Content = "2026-09-28\n\n## hot 🔥\n- Write a downtime document\n\n## cold 🧊\n- Write a proposal"
        });
        var elsewhere = await notes.CreateBlankAsync(new CreateBlankNoteDto());
        await notes.UpdateContentAsync(elsewhere.Id, new UpdateNoteContentDto { Content = "2099-01-01\n\n## hot\n- Not in the journal" });

        // Act
        var focus = await GetService<IReportingService>().GetDashboardFocusAsync();

        // Assert
        focus.Todos.HasJournal.Should().BeTrue();
        focus.Todos.NoteId.Should().Be(latest.Id);
        focus.Todos.NoteTitle.Should().Be("2026-09-28");
        focus.Todos.Items.Should().Equal("Write a downtime document");
    }
}
