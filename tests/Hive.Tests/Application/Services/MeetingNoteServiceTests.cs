using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for MeetingNoteService.
/// </summary>
public class MeetingNoteServiceTests
{
    private readonly Mock<IMeetingNoteRepository> _noteRepositoryMock;
    private readonly Mock<IOneOnOneMeetingRepository> _meetingRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly MeetingNoteService _service;

    private readonly Guid _testNoteId = Guid.NewGuid();
    private readonly Guid _testMeetingId = Guid.NewGuid();
    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly OneOnOneMeeting _testMeeting;
    private readonly DirectReport _testDirectReport;

    public MeetingNoteServiceTests()
    {
        _noteRepositoryMock = new Mock<IMeetingNoteRepository>();
        _meetingRepositoryMock = new Mock<IOneOnOneMeetingRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();

        _service = new MeetingNoteService(
            _noteRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object);

        _testDirectReport = new DirectReport(
            "Jane",
            "Smith",
            "jane.smith@test.com",
            "Senior Developer",
            "Engineering",
            new DateTime(2020, 1, 1));

        var directReportIdProperty = typeof(DirectReport).GetProperty("Id");
        directReportIdProperty!.SetValue(_testDirectReport, _testDirectReportId);

        _testMeeting = new OneOnOneMeeting(
            _testDirectReportId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            null,
            "Test Agenda");

        var meetingIdProperty = typeof(OneOnOneMeeting).GetProperty("Id");
        meetingIdProperty!.SetValue(_testMeeting, _testMeetingId);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNoteRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MeetingNoteService(
            null!,
            _meetingRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("noteRepository");
    }

    [Fact]
    public void Constructor_WithNullMeetingRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MeetingNoteService(
            _noteRepositoryMock.Object,
            null!,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("meetingRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MeetingNoteService(
            _noteRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            null!,
            _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MeetingNoteService(
            _noteRepositoryMock.Object,
            _meetingRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenNoteExists_ReturnsDto()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Test content", NoteCategory.Discussion, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByIdAsync(_testNoteId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_testNoteId);
        result.MeetingId.Should().Be(_testMeetingId);
        result.Content.Should().Be("Test content");
        result.Category.Should().Be(NoteCategory.Discussion);
        result.CategoryName.Should().Be("Discussion");
        result.DirectReportName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoteDoesNotExist_ReturnsNull()
    {
        // Arrange
        _noteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMeetingNotFound_ReturnsUnknownDirectReportName()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Test content", NoteCategory.Discussion, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var result = await _service.GetByIdAsync(_testNoteId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportName.Should().Be("Unknown");
    }

    #endregion

    #region GetByMeetingIdAsync Tests

    [Fact]
    public async Task GetByMeetingIdAsync_WithIncludePrivateTrue_ReturnsAllNotes()
    {
        // Arrange
        var notes = new List<MeetingNote>
        {
            new(_testMeetingId, "Public note", NoteCategory.Discussion, false),
            new(_testMeetingId, "Private note", NoteCategory.Feedback, true)
        };

        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(_testMeetingId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByMeetingIdAsync(_testMeetingId, includePrivate: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Content == "Public note");
        result.Should().Contain(n => n.Content == "Private note");
    }

    [Fact]
    public async Task GetByMeetingIdAsync_WithIncludePrivateFalse_CallsRepositoryCorrectly()
    {
        // Arrange
        var notes = new List<MeetingNote>
        {
            new(_testMeetingId, "Public note", NoteCategory.Discussion, false)
        };

        _noteRepositoryMock.Setup(r => r.GetByMeetingIdAsync(_testMeetingId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByMeetingIdAsync(_testMeetingId, includePrivate: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Public note");
        _noteRepositoryMock.Verify(r => r.GetByMeetingIdAsync(_testMeetingId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetActionItemsAsync Tests

    [Fact]
    public async Task GetActionItemsAsync_WithoutDirectReportId_ReturnsAllActionItems()
    {
        // Arrange
        var actionItems = new List<MeetingNote>
        {
            new(_testMeetingId, "Action 1", NoteCategory.ActionItem, false),
            new(_testMeetingId, "Action 2", NoteCategory.ActionItem, false)
        };

        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actionItems);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetActionItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Category.Should().Be(NoteCategory.ActionItem));
    }

    [Fact]
    public async Task GetActionItemsAsync_WithDirectReportId_PassesToRepository()
    {
        // Arrange
        var actionItems = new List<MeetingNote>();

        _noteRepositoryMock.Setup(r => r.GetActionItemsAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actionItems);

        // Act
        await _service.GetActionItemsAsync(_testDirectReportId);

        // Assert
        _noteRepositoryMock.Verify(r => r.GetActionItemsAsync(_testDirectReportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetOpenActionItemsAsync Tests

    [Fact]
    public async Task GetOpenActionItemsAsync_ReturnsOnlyOpenItems()
    {
        // Arrange
        var openActionItems = new List<MeetingNote>
        {
            new(_testMeetingId, "Open action", NoteCategory.ActionItem, false)
        };

        _noteRepositoryMock.Setup(r => r.GetOpenActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openActionItems);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetOpenActionItemsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Open action");
    }

    #endregion

    #region GetOverdueActionItemsAsync Tests

    [Fact]
    public async Task GetOverdueActionItemsAsync_ReturnsOverdueItems()
    {
        // Arrange
        var overdueActionItems = new List<MeetingNote>
        {
            new(_testMeetingId, "Overdue action", NoteCategory.ActionItem, false)
        };

        _noteRepositoryMock.Setup(r => r.GetOverdueActionItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overdueActionItems);
        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetOverdueActionItemsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Overdue action");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsNote()
    {
        // Arrange
        var dto = new CreateMeetingNoteDto
        {
            MeetingId = _testMeetingId,
            Content = "New note content",
            Category = NoteCategory.Discussion,
            IsPrivate = false
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);

        _noteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote n, CancellationToken _) => n);

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("New note content");
        result.Category.Should().Be(NoteCategory.Discussion);
        result.IsPrivate.Should().BeFalse();

        _noteRepositoryMock.Verify(r => r.AddAsync(It.Is<MeetingNote>(n =>
            n.Content == "New note content" &&
            n.Category == NoteCategory.Discussion &&
            n.IsPrivate == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithActionItemCategory_SetsActionDetails()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.AddDays(7);
        var dto = new CreateMeetingNoteDto
        {
            MeetingId = _testMeetingId,
            Content = "Action item",
            Category = NoteCategory.ActionItem,
            IsPrivate = false,
            ActionDueDate = dueDate,
            ActionAssignee = "John Doe"
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);

        _noteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote n, CancellationToken _) => n);

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be(NoteCategory.ActionItem);
        result.ActionDueDate.Should().Be(dueDate);
        result.ActionAssignee.Should().Be("John Doe");
    }

    [Fact]
    public async Task CreateAsync_WhenMeetingNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateMeetingNoteDto
        {
            MeetingId = Guid.NewGuid(),
            Content = "Note content",
            Category = NoteCategory.Discussion,
            IsPrivate = false
        };

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeeting?)null);

        // Act
        var act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*OneOnOneMeeting*not found*");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesNote()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Original content", NoteCategory.Discussion, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        var dto = new UpdateMeetingNoteDto
        {
            Content = "Updated content",
            Category = NoteCategory.Feedback,
            IsPrivate = true
        };

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        _noteRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.UpdateAsync(_testNoteId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Updated content");
        result.Category.Should().Be(NoteCategory.Feedback);
        result.IsPrivate.Should().BeTrue();

        _noteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new UpdateMeetingNoteDto
        {
            Content = "Updated content",
            Category = NoteCategory.Discussion,
            IsPrivate = false
        };

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*MeetingNote*not found*");
    }

    #endregion

    #region UpdateActionStatusAsync Tests

    [Fact]
    public async Task UpdateActionStatusAsync_UpdatesStatus()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Action item", NoteCategory.ActionItem, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        var dto = new UpdateActionStatusDto
        {
            Status = ActionItemStatus.InProgress
        };

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        _noteRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.UpdateActionStatusAsync(_testNoteId, dto);

        // Assert
        result.Should().NotBeNull();
        result.ActionStatus.Should().Be(ActionItemStatus.InProgress);

        _noteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CompleteActionAsync Tests

    [Fact]
    public async Task CompleteActionAsync_CompletesAction()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Action item", NoteCategory.ActionItem, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        _noteRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _meetingRepositoryMock.Setup(r => r.GetByIdAsync(_testMeetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testMeeting);

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.CompleteActionAsync(_testNoteId);

        // Assert
        result.Should().NotBeNull();
        result.ActionStatus.Should().Be(ActionItemStatus.Completed);

        _noteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<MeetingNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenNoteExists_DeletesNote()
    {
        // Arrange
        var note = new MeetingNote(_testMeetingId, "Test content", NoteCategory.Discussion, false);
        var noteIdProperty = typeof(MeetingNote).GetProperty("Id");
        noteIdProperty!.SetValue(note, _testNoteId);

        _noteRepositoryMock.Setup(r => r.GetByIdAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        _noteRepositoryMock.Setup(r => r.DeleteAsync(_testNoteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(_testNoteId);

        // Assert
        _noteRepositoryMock.Verify(r => r.DeleteAsync(_testNoteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoteDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _noteRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNote?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*MeetingNote*not found*");
    }

    #endregion
}
