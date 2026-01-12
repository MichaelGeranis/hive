using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new DocumentService(_repositoryMock.Object, _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DocumentService(null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DocumentService(_repositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new Document("Test Document", "Test content", "https://example.com", "tag1,tag2");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Title.Should().Be("Test Document");
        result.Content.Should().Be("Test content");
        result.Url.Should().Be("https://example.com");
        result.Tags.Should().Be("tag1, tag2");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var entities = new List<Document>
        {
            new Document("Document 1", "Content 1"),
            new Document("Document 2", "Content 2")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Document 1");
        result[0].Content.Should().Be("Content 1");
        result[1].Title.Should().Be("Document 2");
        result[1].Content.Should().Be("Content 2");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTagsAsync_WithValidTag_ReturnsMatchingDocuments()
    {
        // Arrange
        var entities = new List<Document>
        {
            new Document("Document 1", tags: "urgent,work"),
            new Document("Document 2", tags: "urgent,personal")
        };
        _repositoryMock.Setup(r => r.GetByTagsAsync("urgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByTagsAsync("urgent");

        // Assert
        result.Should().HaveCount(2);
        result.All(d => d.Tags.Contains("urgent")).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTagsAsync_WhenNoMatchingDocuments_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByTagsAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _service.GetByTagsAsync("nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesDocument()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "New Document",
            Content = "New content",
            Url = "https://example.com",
            Tags = "tag1,tag2"
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document doc, CancellationToken _) => doc);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Document");
        result.Content.Should().Be("New content");
        result.Url.Should().Be("https://example.com");
        result.Tags.Should().Be("tag1, tag2");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalDto_CreatesDocument()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Minimal Document"
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document doc, CancellationToken _) => doc);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Minimal Document");
        result.Content.Should().BeEmpty();
        result.Url.Should().BeNull();
        result.Tags.Should().BeEmpty();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullTags_CreatesDocumentWithEmptyTags()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Document with null tags",
            Tags = null
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document doc, CancellationToken _) => doc);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentExists_UpdatesDocument()
    {
        // Arrange
        var entity = new Document("Old Title", "Old content");
        var dto = new UpdateDocumentDto
        {
            Title = "Updated Title",
            Content = "Updated content",
            Url = "https://updated.com",
            Tags = "updated,tags"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(entity.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Content.Should().Be("Updated content");
        result.Url.Should().Be("https://updated.com");
        result.Tags.Should().Be("updated, tags");
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new UpdateDocumentDto { Title = "Updated Title" };
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Document*");
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentExists_DeletesDocument()
    {
        // Arrange
        var entity = new Document("Document to delete");
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Document*");
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryWithCorrectEntity()
    {
        // Arrange
        var dto = new CreateDocumentDto
        {
            Title = "Test",
            Content = "Content",
            Url = "https://test.com",
            Tags = "tag1"
        };
        Document? capturedDocument = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync((Document doc, CancellationToken _) => doc);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Title.Should().Be("Test");
        capturedDocument.Content.Should().Be("Content");
        capturedDocument.Url.Should().Be("https://test.com");
        capturedDocument.Tags.Should().Be("tag1");
    }

    [Fact]
    public async Task UpdateAsync_CallsRepositoryWithUpdatedEntity()
    {
        // Arrange
        var entity = new Document("Original");
        var dto = new UpdateDocumentDto
        {
            Title = "Updated",
            Content = "New content"
        };
        Document? capturedDocument = null;
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Callback<Document, CancellationToken>((doc, _) => capturedDocument = doc)
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateAsync(entity.Id, dto);

        // Assert
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Title.Should().Be("Updated");
        capturedDocument.Content.Should().Be("New content");
    }
}
