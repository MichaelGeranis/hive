using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class OneOnOneMeetingServiceTests
{
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly OneOnOneMeetingService _service;

    private readonly DirectReport _panagiotis =
        new("Panagiotis", "Badredin", "panagiotis@example.com", "Engineer", "Engineering", new DateTime(2024, 1, 1));

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    public OneOnOneMeetingServiceTests()
    {
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new OneOnOneMeetingService(
            _meetingRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object);

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _panagiotis });
        _meetingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneOnOneMeeting>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting m, CancellationToken _) => m);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var act = () => new OneOnOneMeetingService(null!, _directReportRepositoryMock.Object, _activityServiceMock.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("#badredin")]
    [InlineData("#panagiotis")]
    [InlineData("#panagiotisbadredin")]
    [InlineData("#badredinpanagiotis")]
    [InlineData("Badredin")]
    public async Task CreateBlankAsync_LinksThePersonNamedByTheTag(string tag)
    {
        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = tag });

        // Assert
        result.DirectReportId.Should().Be(_panagiotis.Id);
        result.DirectReportName.Should().Be("Panagiotis Badredin");
        result.IsUnlinked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBlankAsync_WithoutTags_LeavesThe1on1Unlinked()
    {
        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto());

        // Assert
        result.DirectReportId.Should().BeNull();
        result.IsUnlinked.Should().BeTrue();
        result.MeetingDate.Should().Be(Today);
    }

    [Fact]
    public async Task CreateBlankAsync_WithATagNamingNobody_LeavesThe1on1Unlinked()
    {
        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#nobodyhere" });

        // Assert
        result.IsUnlinked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBlankAsync_WhenATagIsAmbiguous_LeavesThe1on1Unlinked()
    {
        // Arrange - two people share a first name
        var otherPanagiotis =
            new DirectReport("Panagiotis", "Nikolaou", "p.nikolaou@example.com", "Engineer", "Engineering", new DateTime(2024, 1, 1));
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _panagiotis, otherPanagiotis });

        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#panagiotis" });

        // Assert
        result.IsUnlinked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBlankAsync_WhenTagsNameTwoDifferentPeople_LeavesThe1on1Unlinked()
    {
        // Arrange
        var alice = new DirectReport("Alice", "Johnson", "alice@example.com", "Engineer", "Engineering", new DateTime(2024, 1, 1));
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _panagiotis, alice });

        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#badredin #alice" });

        // Assert
        result.IsUnlinked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBlankAsync_WithADateTag_PlacesThe1on1OnThatDay()
    {
        // Act
        var result = await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#badredin #20260401" });

        // Assert
        result.MeetingDate.Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public async Task CreateBlankAsync_LogsAnActivity()
    {
        // Act
        await _service.CreateBlankAsync(new CreateBlankMeetingDto { Tags = "#badredin" });

        // Assert
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created,
            EntityType.Meeting,
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContentAsync_SavesTheBodyAndDerivesTheTitle()
    {
        // Arrange
        var meeting = SetupExistingMeeting();

        // Act
        var result = await _service.UpdateContentAsync(meeting.Id, new UpdateMeetingContentDto
        {
            Content = "# Career chat\nWants to move towards staff"
        });

        // Assert
        result.Title.Should().Be("Career chat");
        result.Snippet.Should().Be("Wants to move towards staff");
        _meetingRepositoryMock.Verify(r => r.UpdateAsync(meeting, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContentAsync_DoesNotLogAnActivityForEveryAutosave()
    {
        // Arrange
        var meeting = SetupExistingMeeting();

        // Act
        await _service.UpdateContentAsync(meeting.Id, new UpdateMeetingContentDto { Content = "typing" });

        // Assert
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            It.IsAny<ActivityType>(),
            It.IsAny<EntityType>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContentAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = async () => await _service.UpdateContentAsync(Guid.NewGuid(), new UpdateMeetingContentDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateTagsAsync_RelinksThePerson()
    {
        // Arrange
        var meeting = SetupExistingMeeting();

        // Act
        var result = await _service.UpdateTagsAsync(meeting.Id, new UpdateMeetingTagsDto { Tags = "#badredin" });

        // Assert
        result.DirectReportId.Should().Be(_panagiotis.Id);
        result.Tags.Should().Be("#badredin");
    }

    [Fact]
    public async Task UpdateTagsAsync_RemovingThePersonTag_UnlinksThe1on1()
    {
        // Arrange
        var meeting = SetupExistingMeeting(tags: "#badredin", directReportId: _panagiotis.Id);

        // Act
        var result = await _service.UpdateTagsAsync(meeting.Id, new UpdateMeetingTagsDto { Tags = "growth" });

        // Assert
        result.IsUnlinked.Should().BeTrue();
        result.DirectReportName.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDateAsync_MovesThe1on1()
    {
        // Arrange
        var meeting = SetupExistingMeeting();
        var newDate = Today.AddDays(-2);

        // Act
        var result = await _service.UpdateDateAsync(meeting.Id, new UpdateMeetingDateDto { MeetingDate = newDate });

        // Assert
        result.MeetingDate.Should().Be(newDate);
    }

    [Fact]
    public async Task GetCountsAsync_ReturnsTheCountPerPerson()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetCountsByDirectReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ((Guid?)_panagiotis.Id, 4), ((Guid?)null, 2) });

        // Act
        var result = await _service.GetCountsAsync();

        // Assert
        result.Single(c => c.DirectReportId == _panagiotis.Id).Count.Should().Be(4);
        result.Single(c => c.DirectReportId == null).Count.Should().Be(2);
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsHowMany1on1sWereLogged()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        // Act
        var result = await _service.GetTotalCountAsync();

        // Assert
        result.Should().Be(12);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_ResolvesEachPersonsName()
    {
        // Arrange
        var meeting = new OneOnOneMeeting(Today, "Weekly sync", "#badredin", _panagiotis.Id);
        _meetingRepositoryMock.Setup(r => r.GetFilteredPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OneOnOneMeeting> { meeting }, 1));

        // Act
        var result = await _service.GetFilteredPagedAsync(new MeetingPaginationParams());

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].DirectReportName.Should().Be("Panagiotis Badredin");
    }

    [Fact]
    public async Task DeleteAsync_RemovesThe1on1AndLogsIt()
    {
        // Arrange
        var meeting = SetupExistingMeeting();

        // Act
        await _service.DeleteAsync(meeting.Id);

        // Assert
        _meetingRepositoryMock.Verify(r => r.DeleteAsync(meeting.Id, It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Meeting,
            meeting.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private OneOnOneMeeting SetupExistingMeeting(string? tags = null, Guid? directReportId = null)
    {
        var meeting = new OneOnOneMeeting(Today, "Weekly sync", tags, directReportId);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meeting);
        return meeting;
    }
}
