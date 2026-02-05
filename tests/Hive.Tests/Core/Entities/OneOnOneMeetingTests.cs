using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class OneOnOneMeetingTests
{
    private readonly Guid _validDirectReportId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesMeeting()
    {
        // Arrange
        var meetingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var location = "Zoom";
        var agenda = "Weekly sync";

        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, meetingDate, location, agenda);

        // Assert
        meeting.Id.Should().NotBeEmpty();
        meeting.DirectReportId.Should().Be(_validDirectReportId);
        meeting.MeetingDate.Should().Be(meetingDate);
        meeting.Location.Should().Be(location);
        meeting.Agenda.Should().Be(agenda);
        meeting.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithDefaultParams_UsesDefaults()
    {
        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

        // Assert
        meeting.Location.Should().BeEmpty();
        meeting.Agenda.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new OneOnOneMeeting(Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Fact]
    public void Update_UpdatesProperties()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow));
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var newDirectReportId = Guid.NewGuid();

        // Act
        meeting.Update(newDirectReportId, newDate, "Conference Room", "New agenda");

        // Assert
        meeting.DirectReportId.Should().Be(newDirectReportId);
        meeting.MeetingDate.Should().Be(newDate);
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("New agenda");
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithNullLocation_SetsEmptyString()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow), "Initial Location");

        // Act
        meeting.Update(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow), null, null);

        // Assert
        meeting.Location.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithNullAgenda_SetsEmptyString()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow), null, "Initial Agenda");

        // Act
        meeting.Update(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow), null, null);

        // Assert
        meeting.Agenda.Should().BeEmpty();
    }

    [Fact]
    public void Update_TrimsWhitespace()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        meeting.Update(_validDirectReportId, DateOnly.FromDateTime(DateTime.UtcNow), "  Conference Room  ", "  Weekly sync  ");

        // Assert
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("Weekly sync");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var meeting = new OneOnOneMeeting(
            _validDirectReportId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "  Conference Room  ",
            "  Weekly sync  ");

        // Assert
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("Weekly sync");
    }
}
