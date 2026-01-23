using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class DocumentTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesDocument()
    {
        // Arrange
        var title = "API Documentation";
        var content = "This is the content";
        var url = "https://example.com/docs";
        var tags = "api, docs, reference";

        // Act
        var document = new Document(title, content, url, tags);

        // Assert
        document.Id.Should().NotBeEmpty();
        document.Title.Should().Be(title);
        document.Content.Should().Be(content);
        document.Url.Should().Be(url);
        document.Tags.Should().Be("api, docs, reference");
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        document.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMinimalData_CreatesDocument()
    {
        // Arrange & Act
        var document = new Document("Simple Title");

        // Assert
        document.Title.Should().Be("Simple Title");
        document.Content.Should().BeEmpty();
        document.Url.Should().BeNull();
        document.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var document = new Document(
            "  Document Title  ",
            "  Content here  ",
            "  https://example.com  ",
            "  tag1 , tag2  ");

        // Assert
        document.Title.Should().Be("Document Title");
        document.Content.Should().Be("Content here");
        document.Url.Should().Be("https://example.com");
        document.Tags.Should().Be("tag1, tag2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        // Act
        var act = () => new Document(title!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Constructor_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 501);

        // Act
        var act = () => new Document(longTitle);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Constructor_NormalizesTags_RemovesDuplicates()
    {
        // Arrange & Act
        var document = new Document("Title", "", null, "api, API, docs, api");

        // Assert
        document.Tags.Should().Be("api, docs");
    }

    [Fact]
    public void Constructor_NormalizesTags_RemovesEmptyEntries()
    {
        // Arrange & Act
        var document = new Document("Title", "", null, "tag1,  , tag2, , tag3");

        // Assert
        document.Tags.Should().Be("tag1, tag2, tag3");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var document = new Document("Original Title", "Original content");
        var newTitle = "Updated Title";
        var newContent = "Updated content";
        var newUrl = "https://new-url.com";
        var newTags = "new, tags";

        // Act
        document.Update(newTitle, newContent, newUrl, newTags);

        // Assert
        document.Title.Should().Be(newTitle);
        document.Content.Should().Be(newContent);
        document.Url.Should().Be(newUrl);
        document.Tags.Should().Be("new, tags");
        document.UpdatedAt.Should().NotBeNull();
        document.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var document = new Document("Original Title");

        // Act
        var act = () => document.Update("", "content", null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Update_WithNullContent_DefaultsToEmpty()
    {
        // Arrange
        var document = new Document("Title", "Original content");

        // Act
        document.Update("Title", null!, null, null);

        // Assert
        document.Content.Should().BeEmpty();
    }
}
