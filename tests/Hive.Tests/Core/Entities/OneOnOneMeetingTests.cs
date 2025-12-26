using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class OneOnOneMeetingTests
{
    private readonly Guid _validDirectReportId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesMeeting()
    {
        // Arrange
        var scheduledDate = DateTime.UtcNow.AddDays(1);
        var durationMinutes = 30;
        var location = "Zoom";
        var agenda = "Weekly sync";

        // Act
        var meeting = new OneOnOneMeeting(_validDirectReportId, scheduledDate, durationMinutes, location, agenda);

        // Assert
        meeting.Id.Should().NotBeEmpty();
        meeting.DirectReportId.Should().Be(_validDirectReportId);
        meeting.ScheduledDate.Should().Be(scheduledDate);
        meeting.DurationMinutes.Should().Be(durationMinutes);
        meeting.Location.Should().Be(location);
        meeting.Agenda.Should().Be(agenda);
        meeting.Status.Should().Be(MeetingStatus.Scheduled);
        meeting.CompletedAt.Should().BeNull();
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
    public void UpdateDetails_WhenScheduled_UpdatesProperties()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        var newDate = DateTime.UtcNow.AddDays(7);

        // Act
        meeting.UpdateDetails(newDate, 45, "Conference Room", "New agenda");

        // Assert
        meeting.ScheduledDate.Should().Be(newDate);
        meeting.DurationMinutes.Should().Be(45);
        meeting.Location.Should().Be("Conference Room");
        meeting.Agenda.Should().Be("New agenda");
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDetails_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Complete();

        // Act
        var act = () => meeting.UpdateDetails(DateTime.UtcNow, 30, null, null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void UpdateDetails_WhenCancelled_ChangesToRescheduled()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Cancel();

        // Act
        meeting.UpdateDetails(DateTime.UtcNow.AddDays(1), 30, null, null);

        // Assert
        meeting.Status.Should().Be(MeetingStatus.Rescheduled);
    }

    [Fact]
    public void Complete_WhenScheduled_ChangesStatusToCompleted()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);

        // Act
        meeting.Complete();

        // Assert
        meeting.Status.Should().Be(MeetingStatus.Completed);
        meeting.CompletedAt.Should().NotBeNull();
        meeting.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Complete();

        // Act
        var act = () => meeting.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Cancel();

        // Act
        var act = () => meeting.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void Cancel_WhenScheduled_ChangesStatusToCancelled()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);

        // Act
        meeting.Cancel();

        // Assert
        meeting.Status.Should().Be(MeetingStatus.Cancelled);
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Complete();

        // Act
        var act = () => meeting.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Reschedule_WhenScheduled_ChangesDateAndStatus()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        var newDate = DateTime.UtcNow.AddDays(5);

        // Act
        meeting.Reschedule(newDate);

        // Assert
        meeting.ScheduledDate.Should().Be(newDate);
        meeting.Status.Should().Be(MeetingStatus.Rescheduled);
        meeting.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reschedule_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Complete();

        // Act
        var act = () => meeting.Reschedule(DateTime.UtcNow.AddDays(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Reschedule_WhenCancelled_ChangesToRescheduled()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_validDirectReportId, DateTime.UtcNow);
        meeting.Cancel();

        // Act
        meeting.Reschedule(DateTime.UtcNow.AddDays(1));

        // Assert
        meeting.Status.Should().Be(MeetingStatus.Rescheduled);
    }
}
