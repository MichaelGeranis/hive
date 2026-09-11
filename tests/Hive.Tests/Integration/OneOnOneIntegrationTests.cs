using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;

namespace Hive.Tests.Integration;

/// <summary>
/// End-to-end coverage of writing a 1:1 note through the real services and the in-memory
/// persistence stack, including the tag that links it to a person.
/// </summary>
public class OneOnOneIntegrationTests : IntegrationTestBase
{
    private readonly IOneOnOneMeetingService _meetingService;
    private readonly IDirectReportService _directReportService;

    public OneOnOneIntegrationTests()
    {
        _meetingService = GetService<IOneOnOneMeetingService>();
        _directReportService = GetService<IDirectReportService>();
    }

    [Fact]
    public async Task WritingA1on1_LinksItToThePersonNamedInItsTags()
    {
        // Arrange
        var report = await CreateReportAsync("Panagiotis", "Badredin");

        // Act
        var blank = await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#badredin" });
        var written = await _meetingService.UpdateContentAsync(blank.Id, new UpdateMeetingContentDto
        {
            Content = "Weekly sync\n- Talked about the migration"
        });

        // Assert
        written.Title.Should().Be("Weekly sync");
        written.DirectReportId.Should().Be(report.Id);
        written.DirectReportName.Should().Be("Panagiotis Badredin");

        var theirs = await _meetingService.GetByDirectReportIdAsync(report.Id);
        theirs.Should().ContainSingle().Which.Id.Should().Be(blank.Id);
    }

    [Fact]
    public async Task A1on1TaggedWithNobody_IsKeptAsUnlinked()
    {
        // Act
        var meeting = await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#whoisthis" });

        // Assert
        meeting.IsUnlinked.Should().BeTrue();

        var unlinked = await _meetingService.GetFilteredPagedAsync(new MeetingPaginationParams { UnlinkedOnly = true });
        unlinked.Items.Should().ContainSingle().Which.Id.Should().Be(meeting.Id);
    }

    [Fact]
    public async Task RetaggingA1on1_MovesItToTheOtherPerson()
    {
        // Arrange
        var alice = await CreateReportAsync("Alice", "Johnson");
        var bob = await CreateReportAsync("Bob", "Smith");
        var meeting = await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#alice" });
        meeting.DirectReportId.Should().Be(alice.Id);

        // Act
        var updated = await _meetingService.UpdateTagsAsync(meeting.Id, new UpdateMeetingTagsDto { Tags = "#smith" });

        // Assert
        updated.DirectReportId.Should().Be(bob.Id);
        (await _meetingService.GetByDirectReportIdAsync(alice.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task ADateTag_PlacesThe1on1OnThatDay()
    {
        // Arrange
        await CreateReportAsync("Alice", "Johnson");

        // Act
        var meeting = await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#alice #20260401" });

        // Assert
        meeting.MeetingDate.Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public async Task TheDashboardCount_CountsEvery1on1Logged()
    {
        // Arrange
        await CreateReportAsync("Alice", "Johnson");
        await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#alice" });
        await _meetingService.CreateBlankAsync(new CreateBlankMeetingDto());

        // Act
        var count = await _meetingService.GetTotalCountAsync();

        // Assert
        count.Should().Be(2);
    }

    private async Task<DirectReportDto> CreateReportAsync(string firstName, string lastName)
    {
        return await _directReportService.CreateAsync(new CreateDirectReportDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
            JobTitle = "Engineer",
            Department = "Engineering",
            HireDate = new DateTime(2024, 1, 1)
        });
    }
}
