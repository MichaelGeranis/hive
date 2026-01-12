using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class ManagerNoteServiceTests
{
    private readonly Mock<IManagerNoteRepository> _repositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly ManagerNoteService _service;

    public ManagerNoteServiceTests()
    {
        _repositoryMock = new Mock<IManagerNoteRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new ManagerNoteService(_repositoryMock.Object, _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ManagerNoteService(null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ManagerNoteService(_repositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new ManagerNote("Test Note", "Content", NotePriority.High);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Title.Should().Be("Test Note");
        result.Content.Should().Be("Content");
        result.Priority.Should().Be(NotePriority.High);
        result.PriorityName.Should().Be("High");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Note 1"),
            new ManagerNote("Note 2")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Note 1");
        result[1].Title.Should().Be("Note 2");
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsPendingNotes()
    {
        // Arrange
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Pending Note 1"),
            new ManagerNote("Pending Note 2")
        };
        _repositoryMock.Setup(r => r.GetPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetPendingAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(n => !n.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task GetCompletedAsync_ReturnsCompletedNotes()
    {
        // Arrange
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Completed Note 1"),
            new ManagerNote("Completed Note 2")
        };
        entities.ForEach(e => e.MarkComplete());
        _repositoryMock.Setup(r => r.GetCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetCompletedAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(n => n.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task GetOverdueAsync_ReturnsOverdueNotes()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Overdue Note", dueDate: pastDate)
        };
        _repositoryMock.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetOverdueAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTagAsync_ReturnsNotesWithTag()
    {
        // Arrange
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Note 1", tags: "urgent,work")
        };
        _repositoryMock.Setup(r => r.GetByTagAsync("urgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByTagAsync("urgent");

        // Assert
        result.Should().HaveCount(1);
        result[0].TagsList.Should().Contain("urgent");
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingNotes()
    {
        // Arrange
        var entities = new List<ManagerNote>
        {
            new ManagerNote("Test Note", "Test content")
        };
        _repositoryMock.Setup(r => r.SearchAsync("test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.SearchAsync("test", null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Test Note");
    }

    [Fact]
    public async Task GetAllTagsAsync_ReturnsAllUniqueTags()
    {
        // Arrange
        var tags = new List<string> { "urgent", "work", "personal" };
        _repositoryMock.Setup(r => r.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        // Act
        var result = await _service.GetAllTagsAsync();

        // Assert
        result.Should().BeEquivalentTo(tags);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesNote()
    {
        // Arrange
        var dto = new CreateManagerNoteDto
        {
            Title = "New Note",
            Content = "Content",
            Priority = NotePriority.Normal,
            Tags = "tag1,tag2"
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote note, CancellationToken _) => note);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Note");
        result.Content.Should().Be("Content");
        result.Priority.Should().Be(NotePriority.Normal);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteExists_UpdatesNote()
    {
        // Arrange
        var entity = new ManagerNote("Old Title");
        var dto = new UpdateManagerNoteDto
        {
            Title = "Updated Title",
            Content = "Updated content",
            Priority = NotePriority.Urgent
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(entity.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Content.Should().Be("Updated content");
        result.Priority.Should().Be(NotePriority.Urgent);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new UpdateManagerNoteDto { Title = "Updated Title" };
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ToggleCompleteAsync_WhenNoteExists_TogglesCompletion()
    {
        // Arrange
        var entity = new ManagerNote("Note");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.ToggleCompleteAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsCompleted.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ManagerNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleCompleteAsync_WhenNoteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);

        // Act
        var act = async () => await _service.ToggleCompleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenNoteExists_DeletesNote()
    {
        // Arrange
        var entity = new ManagerNote("Test Note");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ManagerNote?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
