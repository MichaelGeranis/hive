using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class NoteFolderServiceTests
{
    private readonly Mock<INoteFolderRepository> _folderRepositoryMock;
    private readonly Mock<IManagerNoteRepository> _noteRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly NoteFolderService _service;

    public NoteFolderServiceTests()
    {
        _folderRepositoryMock = new Mock<INoteFolderRepository>();
        _noteRepositoryMock = new Mock<IManagerNoteRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new NoteFolderService(
            _folderRepositoryMock.Object,
            _noteRepositoryMock.Object,
            _activityServiceMock.Object);

        _folderRepositoryMock.Setup(r => r.AddAsync(It.IsAny<NoteFolder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolder folder, CancellationToken _) => folder);
        _folderRepositoryMock.Setup(r => r.GetChildrenAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NoteFolder>());
        _folderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NoteFolder>());
        _noteRepositoryMock.Setup(r => r.GetByFolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ManagerNote>());
        _noteRepositoryMock.Setup(r => r.GetCountsByFolderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(Guid?, int)>());
    }

    [Fact]
    public void Constructor_WithNullFolderRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new NoteFolderService(null!, _noteRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFoldersWithNoteCounts()
    {
        // Arrange
        var folder = new NoteFolder("Team");
        _folderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { folder });
        _noteRepositoryMock.Setup(r => r.GetCountsByFolderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ((Guid?)folder.Id, 3), ((Guid?)null, 7) });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Team");
        result[0].NoteCount.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolder?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesFolderAndLogsActivity()
    {
        // Act
        var result = await _service.CreateAsync(new CreateNoteFolderDto { Name = "Team" });

        // Assert
        result.Name.Should().Be("Team");
        _folderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<NoteFolder>(), It.IsAny<CancellationToken>()), Times.Once);
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created,
            EntityType.NoteFolder,
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PlacesFolderAfterItsSiblings()
    {
        // Arrange
        var parent = new NoteFolder("Parent");
        _folderRepositoryMock.Setup(r => r.ExistsAsync(parent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _folderRepositoryMock.Setup(r => r.GetChildrenAsync(parent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new NoteFolder("First", parent.Id), new NoteFolder("Second", parent.Id) });

        // Act
        var result = await _service.CreateAsync(new CreateNoteFolderDto { Name = "Third", ParentFolderId = parent.Id });

        // Assert
        result.SortOrder.Should().Be(2);
        result.ParentFolderId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateAsync_WithMissingParent_ThrowsNotFoundException()
    {
        // Arrange
        _folderRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.CreateAsync(new CreateNoteFolderDto
        {
            Name = "Orphan",
            ParentFolderId = Guid.NewGuid()
        });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.CreateAsync(new CreateNoteFolderDto { Name = "  " });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_RenamesFolder()
    {
        // Arrange
        var folder = new NoteFolder("Team");
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);

        // Act
        var result = await _service.UpdateAsync(folder.Id, new UpdateNoteFolderDto { Name = "Squad" });

        // Assert
        result.Name.Should().Be("Squad");
        _folderRepositoryMock.Verify(r => r.UpdateAsync(folder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenFolderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolder?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), new UpdateNoteFolderDto { Name = "Squad" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_MovingFolderIntoItself_ThrowsDomainException()
    {
        // Arrange
        var folder = new NoteFolder("Team");
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _folderRepositoryMock.Setup(r => r.ExistsAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.UpdateAsync(folder.Id, new UpdateNoteFolderDto
        {
            Name = "Team",
            ParentFolderId = folder.Id
        });

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task UpdateAsync_MovingFolderIntoItsOwnDescendant_ThrowsDomainException()
    {
        // Arrange
        var parent = new NoteFolder("Parent");
        var child = new NoteFolder("Child", parent.Id);
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(parent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        _folderRepositoryMock.Setup(r => r.ExistsAsync(child.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _folderRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parent, child });

        // Act
        var act = async () => await _service.UpdateAsync(parent.Id, new UpdateNoteFolderDto
        {
            Name = "Parent",
            ParentFolderId = child.Id
        });

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task DeleteAsync_MovesNotesAndSubFoldersUpInsteadOfDeletingThem()
    {
        // Arrange
        var grandParentId = Guid.NewGuid();
        var folder = new NoteFolder("Doomed", grandParentId);
        var child = new NoteFolder("Child", folder.Id);
        var note = new ManagerNote("Kept note", folderId: folder.Id);

        _folderRepositoryMock.Setup(r => r.GetByIdAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _folderRepositoryMock.Setup(r => r.GetChildrenAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { child });
        _noteRepositoryMock.Setup(r => r.GetByFolderAsync(folder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { note });

        // Act
        await _service.DeleteAsync(folder.Id);

        // Assert
        child.ParentFolderId.Should().Be(grandParentId);
        note.FolderId.Should().Be(grandParentId);
        _folderRepositoryMock.Verify(r => r.DeleteAsync(folder.Id, It.IsAny<CancellationToken>()), Times.Once);
        _noteRepositoryMock.Verify(r => r.UpdateAsync(note, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFolderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _folderRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteFolder?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
