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
/// Tests for ManagerNotesController.
/// </summary>
public class ManagerNotesControllerTests
{
    private readonly Mock<IManagerNoteService> _serviceMock;
    private readonly Mock<ILogger<ManagerNotesController>> _loggerMock;
    private readonly ManagerNotesController _controller;

    public ManagerNotesControllerTests()
    {
        _serviceMock = new Mock<IManagerNoteService>();
        _loggerMock = new Mock<ILogger<ManagerNotesController>>();
        _controller = new ManagerNotesController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto>
        {
            CreateDto("Note 1"),
            CreateDto("Note 2")
        };
        var pagedResult = new PagedResult<ManagerNoteDto>
        {
            Items = notes,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20
        };

        _serviceMock.Setup(s => s.GetFilteredPagedAsync(It.IsAny<NotePaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(1, 20, null, null, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<PagedResult<ManagerNoteDto>>();
        var resultPaged = okResult.Value as PagedResult<ManagerNoteDto>;
        resultPaged!.Items.Should().HaveCount(2);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithQueryAndTag_ReturnsOkWithMatchingNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto> { CreateDto("Note 1") };

        _serviceMock.Setup(s => s.SearchAsync("search term", "tag1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.Search("search term", "tag1", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<ManagerNoteDto>>();
    }

    [Fact]
    public async Task Search_WithNullParameters_ReturnsAllNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto> { CreateDto("Note 1") };

        _serviceMock.Setup(s => s.SearchAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.Search(null, null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetTags Tests

    [Fact]
    public async Task GetTags_ReturnsOkWithAllTags()
    {
        // Arrange
        var tags = new List<string> { "tag1", "tag2", "tag3" };

        _serviceMock.Setup(s => s.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        // Act
        var result = await _controller.GetTags(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<string>>();
        var resultTags = okResult.Value as IEnumerable<string>;
        resultTags.Should().HaveCount(3);
    }

    #endregion

    #region GetByTag Tests

    [Fact]
    public async Task GetByTag_ReturnsOkWithMatchingNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto> { CreateDto("Note 1") };

        _serviceMock.Setup(s => s.GetByTagAsync("important", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetByTag("important", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<ManagerNoteDto>>();
    }

    #endregion

    #region GetPending Tests

    [Fact]
    public async Task GetPending_ReturnsOkWithPendingNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto> { CreateDto("Note 1", isCompleted: false) };

        _serviceMock.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetPending(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetCompleted Tests

    [Fact]
    public async Task GetCompleted_ReturnsOkWithCompletedNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto> { CreateDto("Note 1", isCompleted: true) };

        _serviceMock.Setup(s => s.GetCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetCompleted(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetOverdue Tests

    [Fact]
    public async Task GetOverdue_ReturnsOkWithOverdueNotes()
    {
        // Arrange
        var notes = new List<ManagerNoteDto>
        {
            CreateDto("Overdue Note", isCompleted: false, dueDate: DateTime.UtcNow.AddDays(-1))
        };

        _serviceMock.Setup(s => s.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetOverdue(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithNote()
    {
        // Arrange
        var dto = CreateDto("Note 1");

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ManagerNoteDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNoteDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateManagerNoteDto
        {
            Title = "New Note",
            Content = "Content",
            Tags = "tag1"
        };
        var resultDto = CreateDto("New Note");

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
        var createDto = new CreateManagerNoteDto { Title = "" };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Title cannot be empty"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedNote()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateManagerNoteDto { Title = "Updated" };
        var resultDto = CreateDto("Updated");

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
        var updateDto = new UpdateManagerNoteDto { Title = "Updated" };

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ManagerNote", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateManagerNoteDto { Title = "" };

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Title cannot be empty"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ToggleComplete Tests

    [Fact]
    public async Task ToggleComplete_WhenExists_ReturnsOkWithToggledNote()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateDto("Note", isCompleted: true);

        _serviceMock.Setup(s => s.ToggleCompleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.ToggleComplete(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var note = okResult.Value as ManagerNoteDto;
        note!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleComplete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.ToggleCompleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ManagerNote", id));

        // Act
        var result = await _controller.ToggleComplete(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
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
            .ThrowsAsync(new NotFoundException("ManagerNote", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static ManagerNoteDto CreateDto(string title, bool isCompleted = false, DateTime? dueDate = null)
    {
        return new ManagerNoteDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = "Content",
            Tags = "tag1",
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? DateTime.UtcNow : null,
            DueDate = dueDate,
            Priority = NotePriority.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
