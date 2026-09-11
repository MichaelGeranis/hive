using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class OneOnOneMeetingTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Constructor_WithDateOnly_CreatesAnEmptyUnlinked1on1()
    {
        // Act
        var meeting = new OneOnOneMeeting(Today);

        // Assert
        meeting.Id.Should().NotBeEmpty();
        meeting.MeetingDate.Should().Be(Today);
        meeting.Content.Should().BeEmpty();
        meeting.Title.Should().Be(OneOnOneMeeting.DefaultTitle);
        meeting.Tags.Should().BeEmpty();
        meeting.DirectReportId.Should().BeNull();
        meeting.IsUnlinked().Should().BeTrue();
        meeting.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        meeting.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithContent_DerivesTheTitleFromTheFirstLine()
    {
        // Act
        var meeting = new OneOnOneMeeting(Today, "Weekly sync\nTalked about the migration");

        // Assert
        meeting.Title.Should().Be("Weekly sync");
        meeting.Content.Should().Be("Weekly sync\nTalked about the migration");
    }

    [Fact]
    public void Constructor_NormalisesTags()
    {
        // Act
        var meeting = new OneOnOneMeeting(Today, null, "#Badredin, Growth");

        // Assert
        meeting.Tags.Should().Be("#badredin,growth");
        meeting.GetTagsList().Should().BeEquivalentTo(new[] { "#badredin", "growth" });
    }

    [Fact]
    public void Constructor_WithADateTag_TakesItsDateFromTheTag()
    {
        // Act
        var meeting = new OneOnOneMeeting(Today, null, "#panagiotis #20260401");

        // Assert
        meeting.MeetingDate.Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public void CreateBlank_OpensAnEmptyNoteForTheDay()
    {
        // Act
        var meeting = OneOnOneMeeting.CreateBlank(Today);

        // Assert
        meeting.Content.Should().BeEmpty();
        meeting.Title.Should().Be(OneOnOneMeeting.DefaultTitle);
        meeting.MeetingDate.Should().Be(Today);
    }

    [Fact]
    public void UpdateContent_TakesTheTitleFromTheFirstLine()
    {
        // Arrange
        var meeting = OneOnOneMeeting.CreateBlank(Today);

        // Act
        meeting.UpdateContent("# Career chat\nWants to move towards staff");

        // Assert
        meeting.Title.Should().Be("Career chat");
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_PreservesWhitespaceAsTyped()
    {
        // Arrange
        var meeting = OneOnOneMeeting.CreateBlank(Today);

        // Act
        meeting.UpdateContent("Notes\n\n  indented\n\n");

        // Assert
        meeting.Content.Should().Be("Notes\n\n  indented\n\n");
    }

    [Fact]
    public void UpdateContent_WithEmptyContent_FallsBackToThePlaceholderTitle()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today, "Something");

        // Act
        meeting.UpdateContent("   ");

        // Assert
        meeting.Title.Should().Be(OneOnOneMeeting.DefaultTitle);
    }

    [Fact]
    public void UpdateTags_ReplacesTheTags()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today, null, "#alice");

        // Act
        meeting.UpdateTags("#bob, growth");

        // Assert
        meeting.Tags.Should().Be("#bob,growth");
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateTags_WithADateTag_MovesTheMeeting()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);

        // Act
        meeting.UpdateTags("#bob #20251224");

        // Assert
        meeting.MeetingDate.Should().Be(new DateOnly(2025, 12, 24));
    }

    [Fact]
    public void UpdateTags_WithoutADateTag_LeavesTheDateAlone()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);

        // Act
        meeting.UpdateTags("#bob");

        // Assert
        meeting.MeetingDate.Should().Be(Today);
    }

    [Theory]
    [InlineData("#20261301")]  // month 13
    [InlineData("#20260230")]  // February 30th
    [InlineData("#2026041")]   // too short
    [InlineData("#notadate")]
    public void UpdateTags_WithAnInvalidDateTag_LeavesTheDateAlone(string tag)
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);

        // Act
        meeting.UpdateTags(tag);

        // Assert
        meeting.MeetingDate.Should().Be(Today);
    }

    [Fact]
    public void LinkTo_LinksTheMeetingToAPerson()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);
        var reportId = Guid.NewGuid();

        // Act
        meeting.LinkTo(reportId);

        // Assert
        meeting.DirectReportId.Should().Be(reportId);
        meeting.IsUnlinked().Should().BeFalse();
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void LinkTo_Null_LeavesTheMeetingUnlinked()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today, null, null, Guid.NewGuid());

        // Act
        meeting.LinkTo(null);

        // Assert
        meeting.DirectReportId.Should().BeNull();
        meeting.IsUnlinked().Should().BeTrue();
    }

    [Fact]
    public void LinkTo_TheSamePerson_ChangesNothing()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var meeting = new OneOnOneMeeting(Today, null, null, reportId);

        // Act
        meeting.LinkTo(reportId);

        // Assert
        meeting.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void SetMeetingDate_MovesTheMeeting()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);
        var earlier = Today.AddDays(-3);

        // Act
        meeting.SetMeetingDate(earlier);

        // Assert
        meeting.MeetingDate.Should().Be(earlier);
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetMeetingDate_WithTheSameDay_ChangesNothing()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today);

        // Act
        meeting.SetMeetingDate(Today);

        // Assert
        meeting.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void HasTag_FindsATagRegardlessOfCase()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today, null, "#Badredin");

        // Act & Assert
        meeting.HasTag("#badredin").Should().BeTrue();
        meeting.HasTag("#BADREDIN").Should().BeTrue();
        meeting.HasTag("#alice").Should().BeFalse();
    }
}
