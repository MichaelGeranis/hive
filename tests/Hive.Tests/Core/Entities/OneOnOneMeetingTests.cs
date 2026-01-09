using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class OneOnOneMeetingTests
{
    private readonly Guid _validDirectReportId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesMeeting()
    {
        // Arrange
        var meetingDate = DateTime.UtcNow.AddDays(1);
        var durationMinutes = 30;
        var location = "Zoom";
        var agenda = "Weekly sync";

        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, meetingDate, durationMinutes, location, agenda);

        // Assert
        meeting.Id.Should().NotBeEmpty();
        meeting.DirectReportId.Should().Be(_validDirectReportId);
        meeting.MeetingDate.Should().Be(meetingDate);
        meeting.DurationMinutes.Should().Be(durationMinutes);
        meeting.Location.Should().Be(location);
        meeting.Agenda.Should().Be(agenda);
        meeting.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithDefaultParams_UsesDefaults()
    {
        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow.AddDays(1));

        // Assert
        meeting.DurationMinutes.Should().Be(30);
        meeting.Location.Should().BeEmpty();
        meeting.Agenda.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new OneOnOneMeeting(Guid.Empty, DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithDurationTooShort_ThrowsArgumentException(int duration)
    {
        // Act
        var act = () => new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow, duration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("durationMinutes");
    }

    [Fact]
    public void Constructor_WithDurationTooLong_ThrowsArgumentException()
    {
        // Act
        var act = () => new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow, 481);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("durationMinutes");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(480)]
    public void Constructor_WithValidDuration_Succeeds(int duration)
    {
        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow, duration);

        // Assert
        meeting.DurationMinutes.Should().Be(duration);
    }

    [Fact]
    public void Update_UpdatesProperties()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        var newDate = DateTime.UtcNow.AddDays(7);
        var newDirectReportId = Guid.NewGuid();

        // Act
        meeting.Update(newDirectReportId, newDate, 45, "Conference Room", "New agenda");

        // Assert
        meeting.DirectReportId.Should().Be(newDirectReportId);
        meeting.MeetingDate.Should().Be(newDate);
        meeting.DurationMinutes.Should().Be(45);
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("New agenda");
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithInvalidDuration_ThrowsArgumentException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);

        // Act
        var act = () => meeting.Update(_validDirectReportId, DateTime.UtcNow, 3, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("durationMinutes");
    }

    [Fact]
    public void Update_WithNullLocation_SetsEmptyString()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow, 30, "Initial Location");

        // Act
        meeting.Update(_validDirectReportId, DateTime.UtcNow, 30, null, null);

        // Assert
        meeting.Location.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithNullAgenda_SetsEmptyString()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow, 30, null, "Initial Agenda");

        // Act
        meeting.Update(_validDirectReportId, DateTime.UtcNow, 30, null, null);

        // Assert
        meeting.Agenda.Should().BeEmpty();
    }

    [Fact]
    public void Update_TrimsWhitespace()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);

        // Act
        meeting.Update(_validDirectReportId, DateTime.UtcNow, 30, "  Conference Room  ", "  Weekly sync  ");

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
            DateTime.UtcNow,
            30,
            "  Conference Room  ",
            "  Weekly sync  ");

        // Assert
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("Weekly sync");
    }
}
