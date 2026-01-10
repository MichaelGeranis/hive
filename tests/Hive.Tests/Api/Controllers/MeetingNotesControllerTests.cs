using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for MeetingNotesController.
/// </summary>
public class MeetingNotesControllerTests
{
    private readonly Mock<IMeetingNoteService> _serviceMock;
    private readonly Mock<ILogger<MeetingNotesController>> _loggerMock;
    private readonly MeetingNotesController _controller;

    public MeetingNotesControllerTests()
    {
        _serviceMock = new Mock<IMeetingNoteService>();
        _loggerMock = new Mock<ILogger<MeetingNotesController>>();
        _controller = new MeetingNotesController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithNote()
    {
        // Arrange
        var dto = CreateDto();

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<MeetingNoteDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeetingNoteDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByMeeting Tests

    [Fact]
    public async Task GetByMeeting_ReturnsOkWithNotes()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var notes = new List<MeetingNoteDto> { CreateDto(), CreateDto() };

        _serviceMock.Setup(s => s.GetByMeetingIdAsync(meetingId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetByMeeting(meetingId, true, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<MeetingNoteDto>>();
        var resultNotes = okResult.Value as IEnumerable<MeetingNoteDto>;
        resultNotes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMeeting_WithIncludePrivateFalse_ExcludesPrivateNotes()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var notes = new List<MeetingNoteDto> { CreateDto() };

        _serviceMock.Setup(s => s.GetByMeetingIdAsync(meetingId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetByMeeting(meetingId, false, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetActionItems Tests

    [Fact]
    public async Task GetActionItems_ReturnsOkWithActionItems()
    {
        // Arrange
        var notes = new List<MeetingNoteDto>
        {
            CreateDto(NoteCategory.ActionItem)
        };

        _serviceMock.Setup(s => s.GetActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetActionItems(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<MeetingNoteDto>>();
    }

    [Fact]
    public async Task GetActionItems_WithDirectReportId_FiltersActionItems()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var notes = new List<MeetingNoteDto> { CreateDto(NoteCategory.ActionItem) };

        _serviceMock.Setup(s => s.GetActionItemsAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetActionItems(directReportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetOpenActionItems Tests

    [Fact]
    public async Task GetOpenActionItems_ReturnsOkWithOpenActionItems()
    {
        // Arrange
        var notes = new List<MeetingNoteDto>
        {
            CreateDto(NoteCategory.ActionItem, ActionItemStatus.Open)
        };

        _serviceMock.Setup(s => s.GetOpenActionItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetOpenActionItems(null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetOverdueActionItems Tests

    [Fact]
    public async Task GetOverdueActionItems_ReturnsOkWithOverdueItems()
    {
        // Arrange
        var notes = new List<MeetingNoteDto>
        {
            CreateDto(NoteCategory.ActionItem, ActionItemStatus.Open, DateTime.UtcNow.AddDays(-1))
        };

        _serviceMock.Setup(s => s.GetOverdueActionItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetOverdueActionItems(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateMeetingNoteDto
        {
            MeetingId = Guid.NewGuid(),
            Content = "Note content",
            Category = NoteCategory.Discussion
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateMeetingNoteDto { Content = "" };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Content cannot be empty"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenMeetingNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateMeetingNoteDto
        {
            MeetingId = Guid.NewGuid(),
            Content = "Content"
        };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Meeting", createDto.MeetingId));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedNote()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMeetingNoteDto { Content = "Updated content" };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMeetingNoteDto { Content = "Updated" };

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("MeetingNote", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateMeetingNoteDto { Content = "Updated" };

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot update completed action"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateActionStatus Tests

    [Fact]
    public async Task UpdateActionStatus_WhenExists_ReturnsOkWithUpdatedNote()
    {
        // Arrange
        var id = Guid.NewGuid();
        var statusDto = new UpdateActionStatusDto { Status = ActionItemStatus.InProgress };
        var resultDto = CreateDto(NoteCategory.ActionItem, ActionItemStatus.InProgress);

        _serviceMock.Setup(s => s.UpdateActionStatusAsync(id, statusDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.UpdateActionStatus(id, statusDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var note = okResult.Value as MeetingNoteDto;
        note!.ActionStatus.Should().Be(ActionItemStatus.InProgress);
    }

    [Fact]
    public async Task UpdateActionStatus_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var statusDto = new UpdateActionStatusDto { Status = ActionItemStatus.InProgress };

        _serviceMock.Setup(s => s.UpdateActionStatusAsync(id, statusDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("MeetingNote", id));

        // Act
        var result = await _controller.UpdateActionStatus(id, statusDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateActionStatus_WhenNotActionItem_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var statusDto = new UpdateActionStatusDto { Status = ActionItemStatus.InProgress };

        _serviceMock.Setup(s => s.UpdateActionStatusAsync(id, statusDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Note is not an action item"));

        // Act
        var result = await _controller.UpdateActionStatus(id, statusDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CompleteAction Tests

    [Fact]
    public async Task CompleteAction_WhenExists_ReturnsOkWithCompletedNote()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto(NoteCategory.ActionItem, ActionItemStatus.Completed);

        _serviceMock.Setup(s => s.CompleteActionAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.CompleteAction(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var note = okResult.Value as MeetingNoteDto;
        note!.ActionStatus.Should().Be(ActionItemStatus.Completed);
    }

    [Fact]
    public async Task CompleteAction_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.CompleteActionAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("MeetingNote", id));

        // Act
        var result = await _controller.CompleteAction(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CompleteAction_WhenNotActionItem_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.CompleteActionAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Note is not an action item"));

        // Act
        var result = await _controller.CompleteAction(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("MeetingNote", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static MeetingNoteDto CreateDto(
        NoteCategory category = NoteCategory.Discussion,
        ActionItemStatus? actionStatus = null,
        DateTime? actionDueDate = null)
    {
        return new MeetingNoteDto
        {
            Id = Guid.NewGuid(),
            MeetingId = Guid.NewGuid(),
            Content = "Note content",
            Category = category,
            IsPrivate = false,
            ActionStatus = actionStatus,
            ActionDueDate = actionDueDate,
            ActionAssignee = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
