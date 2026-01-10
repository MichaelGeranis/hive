using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class DocumentRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly DocumentRepository _repository;

    public DocumentRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new DocumentRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DocumentRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var document = CreateAndAddDocument("Test Doc");

        // Act
        var result = await _repository.GetByIdAsync(document.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(document.Id);
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
    public async Task GetAllAsync_ReturnsAllDocuments()
    {
        // Arrange
        CreateAndAddDocument("Doc 1");
        CreateAndAddDocument("Doc 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var doc1 = CreateAndAddDocument("First");
        await Task.Delay(10);
        var doc2 = CreateAndAddDocument("Second");
        await Task.Delay(10);
        var doc3 = CreateAndAddDocument("Third");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(doc3.Id); // Most recent first
        result[1].Id.Should().Be(doc2.Id);
        result[2].Id.Should().Be(doc1.Id);
    }

    [Fact]
    public async Task GetByTagsAsync_ReturnsDocumentsWithMatchingTags()
    {
        // Arrange
        CreateAndAddDocument("Doc 1", tags: "important,urgent");
        CreateAndAddDocument("Doc 2", tags: "important");
        CreateAndAddDocument("Doc 3", tags: "review");

        // Act
        var result = await _repository.GetByTagsAsync("important");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTagsAsync_WithMultipleTags_ReturnsDocumentsMatchingAnyTag()
    {
        // Arrange
        CreateAndAddDocument("Doc 1", tags: "important");
        CreateAndAddDocument("Doc 2", tags: "urgent");
        CreateAndAddDocument("Doc 3", tags: "review");

        // Act
        var result = await _repository.GetByTagsAsync("important,urgent");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTagsAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddDocument("Doc 1", tags: "IMPORTANT");

        // Act
        var result = await _repository.GetByTagsAsync("important");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_AddsDocumentToContext()
    {
        // Arrange
        var document = new Document("Test Doc", "Content");

        // Act
        var result = await _repository.AddAsync(document);

        // Assert
        result.Should().Be(document);
        _context.Documents.Should().ContainKey(document.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDocumentInContext()
    {
        // Arrange
        var document = CreateAndAddDocument("Original");
        document.Update("Updated", "New content", "https://example.com", "new-tag");

        // Act
        await _repository.UpdateAsync(document);

        // Assert
        var stored = _context.Documents[document.Id];
        stored.Title.Should().Be("Updated");
        stored.Content.Should().Be("New content");
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentFromContext()
    {
        // Arrange
        var document = CreateAndAddDocument("Test");

        // Act
        await _repository.DeleteAsync(document.Id);

        // Assert
        _context.Documents.Should().NotContainKey(document.Id);
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
        var document = CreateAndAddDocument("Test");

        // Act
        var result = await _repository.ExistsAsync(document.Id);

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

    private Document CreateAndAddDocument(
        string title = "Test Document",
        string content = "Test content",
        string? url = null,
        string? tags = null)
    {
        var document = new Document(title, content, url, tags);
        _context.Documents.TryAdd(document.Id, document);
        return document;
    }
}
