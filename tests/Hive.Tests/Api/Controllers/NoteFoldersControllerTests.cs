using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for NoteFoldersController.
/// </summary>
public class NoteFoldersControllerTests
{
    private readonly Mock<INoteFolderService> _serviceMock;
    private readonly Mock<ILogger<NoteFoldersController>> _loggerMock;
    private readonly NoteFoldersController _controller;

    public NoteFoldersControllerTests()
    {
        _serviceMock = new Mock<INoteFolderService>();
        _loggerMock = new Mock<ILogger<NoteFoldersController>>();
        _controller = new NoteFoldersController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithFolders()
    {
        // Arrange
        var folders = new List<NoteFolderDto> { CreateDto("Work"), CreateDto("Personal") };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        (okResult.Value as IEnumerable<NoteFolderDto>).Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var folder = CreateDto("Work");
        _serviceMock.Setup(s => s.GetByIdAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var result = await _controller.GetById(folder.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(folder);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolderDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedFolder()
    {
        // Arrange
        var folder = CreateDto("Work");
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var result = await _controller.Create(new CreateNoteFolderDto { Name = "Work" }, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(folder);
    }

    [Fact]
    public async Task Create_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Folder name cannot be empty."));

        // Act
        var result = await _controller.Create(new CreateNoteFolderDto { Name = "" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithMissingParent_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("NoteFolder", Guid.NewGuid()));

        // Act
        var result = await _controller.Create(new CreateNoteFolderDto { Name = "Sub", ParentFolderId = Guid.NewGuid() }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedFolder()
    {
        // Arrange
        var folder = CreateDto("Squad");
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var result = await _controller.Update(folder.Id, new UpdateNoteFolderDto { Name = "Squad" }, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(folder);
    }

    [Fact]
    public async Task Update_WhenFolderNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("NoteFolder", Guid.NewGuid()));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateNoteFolderDto { Name = "Squad" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenMoveWouldCreateACycle_ReturnsBadRequest()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateNoteFolderDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("A folder cannot be moved inside itself."));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), new UpdateNoteFolderDto { Name = "Squad" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenFolderNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("NoteFolder", Guid.NewGuid()));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static NoteFolderDto CreateDto(string name)
    {
        return new NoteFolderDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            SortOrder = 0,
            NoteCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
}
