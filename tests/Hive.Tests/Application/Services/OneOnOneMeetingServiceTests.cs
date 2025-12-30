using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Application.Services;

public class OneOnOneMeetingServiceTests
{
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IMeetingNoteRepository> _noteRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly OneOnOneMeetingService _service;

    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly DirectReport _testDirectReport;

    public OneOnOneMeetingServiceTests()
    {
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _noteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _service = new OneOnOneMeetingService(
            _meetingRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _directReportRepositoryMock.Object);

        _testDirectReport = CreateDirectReport();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMeetingRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OneOnOneMeetingService(
            null!,
            _noteRepositoryMock.Object,
            _directReportRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("meetingRepository");
    }

    [Fact]
    public void Constructor_WithNullNoteRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OneOnOneMeetingService(
            _meetingRepositoryMock.Object,
            null!,
            _directReportRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("noteRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OneOnOneMeetingService(
            _meetingRepositoryMock.Object,
            _noteRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var meeting = CreateMeeting();
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetByIdAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(meeting.Id);
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.Status.Should().Be(MeetingStatus.Scheduled);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetDetailsAsync Tests

    [Fact]
    public async Task GetDetailsAsync_WhenExists_ReturnsDetailsWithNotes()
    {
        // Arrange
        var meeting = CreateMeeting();
        var note = new MeetingNote(meeting.Id, "Test note", NoteCategory.Discussion);

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(meeting.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote> { note });

        // Act
        var result = await _service.GetDetailsAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Meeting.Should().NotBeNull();
        result.Notes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var result = await _service.GetDetailsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var meetings = new List<OneOnOneMeeting>
        {
            CreateMeeting(),
            CreateMeeting()
        };
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeeting>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByDirectReportIdAsync Tests

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var meetings = new List<OneOnOneMeeting>
        {
            CreateMeeting(),
            CreateMeeting()
        };
        _meetingRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetByDirectReportIdAsync(_testDirectReportId);

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.DirectReportId == _testDirectReportId).Should().BeTrue();
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var meetings = new List<OneOnOneMeeting> { CreateMeeting() };
        _meetingRepositoryMock.Setup(r => r.GetByStatusAsync(MeetingStatus.Scheduled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetByStatusAsync(MeetingStatus.Scheduled);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(MeetingStatus.Scheduled);
    }

    #endregion

    #region GetUpcomingAsync Tests

    [Fact]
    public async Task GetUpcomingAsync_ReturnsUpcomingMeetings()
    {
        // Arrange
        var meetings = new List<OneOnOneMeeting> { CreateMeeting() };
        _meetingRepositoryMock.Setup(r => r.GetUpcomingAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetUpcomingAsync(7);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUpcomingAsync_WithCustomDays_UsesProvidedValue()
    {
        // Arrange
        var meetings = new List<OneOnOneMeeting> { CreateMeeting() };
        _meetingRepositoryMock.Setup(r => r.GetUpcomingAsync(14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetUpcomingAsync(14);

        // Assert
        _meetingRepositoryMock.Verify(r => r.GetUpcomingAsync(14, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetNextMeetingAsync Tests

    [Fact]
    public async Task GetNextMeetingAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var meeting = CreateMeeting();
        _meetingRepositoryMock.Setup(r => r.GetNextMeetingAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.GetNextMeetingAsync(_testDirectReportId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(_testDirectReportId);
    }

    [Fact]
    public async Task GetNextMeetingAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetNextMeetingAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var result = await _service.GetNextMeetingAsync(_testDirectReportId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesMeeting()
    {
        // Arrange
        var dto = new CreateOneOnOneMeetingDto
        {
            DirectReportId = _testDirectReportId,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30,
            Location = "Conference Room",
            Agenda = "Weekly check-in"
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _meetingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneOnOneMeeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting m, CancellationToken ct) => m);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.DurationMinutes.Should().Be(30);
        result.Location.Should().Be("Conference Room");
        result.Agenda.Should().Be("Weekly check-in");
        result.Status.Should().Be(MeetingStatus.Scheduled);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDirectReport_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateOneOnOneMeetingDto
        {
            DirectReportId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"DirectReport with id {dto.DirectReportId} not found");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesMeeting()
    {
        // Arrange
        var meeting = CreateMeeting();
        var dto = new UpdateOneOnOneMeetingDto
        {
            ScheduledDate = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 60,
            Location = "Updated Location",
            Agenda = "Updated Agenda"
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.UpdateAsync(meeting.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.DurationMinutes.Should().Be(60);
        result.Location.Should().Be("Updated Location");
        result.Agenda.Should().Be("Updated Agenda");
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(meeting, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var dto = new UpdateOneOnOneMeetingDto
        {
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = () => _service.UpdateAsync(meetingId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"OneOnOneMeeting with id {meetingId} not found");
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_OnScheduledMeeting_CompletesMeeting()
    {
        // Arrange
        var meeting = CreateMeeting();
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.CompleteAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MeetingStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(meeting, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = () => _service.CompleteAsync(meetingId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_OnScheduledMeeting_CancelsMeeting()
    {
        // Arrange
        var meeting = CreateMeeting();
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.CancelAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MeetingStatus.Cancelled);
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(meeting, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = () => _service.CancelAsync(meetingId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region RescheduleAsync Tests

    [Fact]
    public async Task RescheduleAsync_OnScheduledMeeting_ReschedulesMeeting()
    {
        // Arrange
        var meeting = CreateMeeting();
        var newDate = DateTime.UtcNow.AddDays(7);
        var dto = new RescheduleMeetingDto { NewDate = newDate };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingNote>());

        // Act
        var result = await _service.RescheduleAsync(meeting.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MeetingStatus.Rescheduled);
        result.ScheduledDate.Should().Be(newDate);
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(meeting, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RescheduleAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var dto = new RescheduleMeetingDto { NewDate = DateTime.UtcNow.AddDays(7) };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = () => _service.RescheduleAsync(meetingId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesMeeting()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _meetingRepositoryMock.Setup(r => r.ExistsAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(meetingId);

        // Assert
        _meetingRepositoryMock.Verify(r => r.DeleteAsync(meetingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        _meetingRepositoryMock.Setup(r => r.ExistsAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(meetingId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Helper Methods

    private DirectReport CreateDirectReport()
    {
        var dr = new DirectReport("John", "Doe", "john.doe@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        SetPropertyValue(dr, "Id", _testDirectReportId);
        return dr;
    }

    private OneOnOneMeeting CreateMeeting()
    {
        return new OneOnOneMeeting(
            _testDirectReportId,
            DateTime.UtcNow.AddDays(1),
            30,
            "Conference Room",
            "Weekly check-in");
    }

    private static void SetPropertyValue(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property!.SetValue(obj, value);
    }

    #endregion
}
