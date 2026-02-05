using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class OneOnOneMeetingRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly OneOnOneMeetingRepository _repository;
    private readonly Guid _directReportId;

    public OneOnOneMeetingRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new OneOnOneMeetingRepository(_context);
        _directReportId = Guid.NewGuid();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OneOnOneMeetingRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var meeting = CreateAndAddMeeting();

        // Act
        var result = await _repository.GetByIdAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(meeting.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMeetings()
    {
        // Arrange
        CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
        CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByMeetingDateDescending()
    {
        // Arrange
        var meeting1 = CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
        var meeting2 = CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        var meeting3 = CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(meeting3.Id); // Most recent first
        result[1].Id.Should().Be(meeting2.Id);
        result[2].Id.Should().Be(meeting1.Id);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingMeetings()
    {
        // Arrange
        var otherReportId = Guid.NewGuid();
        CreateAndAddMeeting();
        CreateAndAddMeeting();
        CreateAndAddMeeting(directReportId: otherReportId);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(_directReportId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.DirectReportId.Should().Be(_directReportId));
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_OrdersByMeetingDateDescending()
    {
        // Arrange
        var meeting1 = CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)));
        var meeting2 = CreateAndAddMeeting(meetingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));

        // Act
        var result = await _repository.GetByDirectReportIdAsync(_directReportId);

        // Assert
        result[0].Id.Should().Be(meeting2.Id);
        result[1].Id.Should().Be(meeting1.Id);
    }

    [Fact]
    public async Task AddAsync_AddsMeetingToContext()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_directReportId, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = await _repository.AddAsync(meeting);

        // Assert
        result.Should().Be(meeting);
        _context.OneOnOneMeetings.Should().ContainKey(meeting.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = CreateAndAddMeeting();

        // Act
        var act = () => _repository.AddAsync(meeting);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMeetingInContext()
    {
        // Arrange
        var meeting = CreateAndAddMeeting();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        meeting.Update(_directReportId, newDate, "New location", "New agenda");

        // Act
        await _repository.UpdateAsync(meeting);

        // Assert
        var stored = _context.OneOnOneMeetings[meeting.Id];
        stored.MeetingDate.Should().Be(newDate);
        stored.Location.Should().Be("New location");
        stored.Agenda.Should().Be("New agenda");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentMeeting_ThrowsInvalidOperationException()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(_directReportId, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var act = () => _repository.UpdateAsync(meeting);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesMeetingFromContext()
    {
        // Arrange
        var meeting = CreateAndAddMeeting();

        // Act
        await _repository.DeleteAsync(meeting.Id);

        // Assert
        _context.OneOnOneMeetings.Should().NotContainKey(meeting.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var meeting = CreateAndAddMeeting();

        // Act
        var result = await _repository.ExistsAsync(meeting.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    private OneOnOneMeeting CreateAndAddMeeting(
        Guid? directReportId = null,
        DateOnly? meetingDate = null)
    {
        var meeting = new OneOnOneMeeting(
            directReportId ?? _directReportId,
            meetingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            "Office",
            "Weekly sync");
        _context.OneOnOneMeetings.TryAdd(meeting.Id, meeting);
        return meeting;
    }
}
