using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ManagerNoteTests
{
    [Fact]
    public void Constructor_WithValidTitle_CreatesManagerNote()
    {
        // Arrange & Act
        var note = new ManagerNote("Test Note");

        // Assert
        note.Id.Should().NotBeEmpty();
        note.Title.Should().Be("Test Note");
        note.Content.Should().BeEmpty();
        note.Tags.Should().BeEmpty();
        note.FolderId.Should().BeNull();
        note.IsPinned.Should().BeFalse();
        note.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        note.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesManagerNote()
    {
        // Arrange
        var folderId = Guid.NewGuid();

        // Act
        var note = new ManagerNote("Test Note", "Test content", "tag1,tag2", folderId);

        // Assert
        note.Title.Should().Be("Test Note");
        note.Content.Should().Be("Test content");
        note.Tags.Should().Be("tag1,tag2");
        note.FolderId.Should().Be(folderId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act
        var act = () => new ManagerNote(invalidTitle!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title cannot be empty.*");
    }

    [Fact]
    public void Constructor_WithTitleExceeding200Characters_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act
        var act = () => new ManagerNote(longTitle);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Title cannot exceed 200 characters.*");
    }

    [Fact]
    public void Update_WithValidData_UpdatesNote()
    {
        // Arrange
        var note = new ManagerNote("Original Title");

        // Act
        note.Update("Updated Title", "New content", "new,tags");

        // Assert
        note.Title.Should().Be("Updated Title");
        note.Content.Should().Be("New content");
        note.Tags.Should().Be("new,tags");
        note.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NormalizeTags_WithCommaSeparatedTags_NormalizesCorrectly()
    {
        // Act
        var note = new ManagerNote("Test", tags: "urgent, work, personal");

        // Assert
        note.Tags.Should().Be("personal,urgent,work");
    }

    [Fact]
    public void NormalizeTags_WithDuplicateTags_RemovesDuplicates()
    {
        // Act
        var note = new ManagerNote("Test", tags: "urgent, Urgent, URGENT");

        // Assert
        note.Tags.Should().Be("urgent");
    }

    [Fact]
    public void NormalizeTags_WithMixedSeparators_NormalizesCorrectly()
    {
        // Act
        var note = new ManagerNote("Test", tags: "tag1,tag2;tag3 tag4");

        // Assert
        note.GetTagsList().Should().BeEquivalentTo(new[] { "tag1", "tag2", "tag3", "tag4" });
    }

    [Fact]
    public void GetTagsList_WithNoTags_ReturnsEmptyArray()
    {
        // Arrange
        var note = new ManagerNote("Test");

        // Act
        var tags = note.GetTagsList();

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public void GetTagsList_WithTags_ReturnsTagsArray()
    {
        // Arrange
        var note = new ManagerNote("Test", tags: "tag1,tag2,tag3");

        // Act
        var tags = note.GetTagsList();

        // Assert
        tags.Should().BeEquivalentTo(new[] { "tag1", "tag2", "tag3" });
    }

    [Fact]
    public void HasTag_WithExistingTag_ReturnsTrue()
    {
        // Arrange
        var note = new ManagerNote("Test", tags: "urgent,work");

        // Act & Assert
        note.HasTag("urgent").Should().BeTrue();
        note.HasTag("URGENT").Should().BeTrue(); // Case insensitive
        note.HasTag("work").Should().BeTrue();
    }

    [Fact]
    public void HasTag_WithNonExistingTag_ReturnsFalse()
    {
        // Arrange
        var note = new ManagerNote("Test", tags: "urgent,work");

        // Act & Assert
        note.HasTag("personal").Should().BeFalse();
    }

    [Fact]
    public void HasTag_WithEmptyTag_ReturnsFalse()
    {
        // Arrange
        var note = new ManagerNote("Test", tags: "urgent");

        // Act & Assert
        note.HasTag("").Should().BeFalse();
        note.HasTag(null!).Should().BeFalse();
    }

    [Fact]
    public void CreateBlank_CreatesEmptyNoteWithPlaceholderTitle()
    {
        // Arrange
        var folderId = Guid.NewGuid();

        // Act
        var note = ManagerNote.CreateBlank(folderId);

        // Assert
        note.Title.Should().Be(ManagerNote.DefaultTitle);
        note.Content.Should().BeEmpty();
        note.FolderId.Should().Be(folderId);
        note.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void CreateBlank_WithoutFolder_SitsAtRoot()
    {
        // Act
        var note = ManagerNote.CreateBlank();

        // Assert
        note.FolderId.Should().BeNull();
    }

    [Fact]
    public void UpdateContent_TakesTitleFromFirstLine()
    {
        // Arrange
        var note = ManagerNote.CreateBlank();

        // Act
        note.UpdateContent("Sprint retro\nWhat went well?");

        // Assert
        note.Title.Should().Be("Sprint retro");
        note.Content.Should().Be("Sprint retro\nWhat went well?");
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateContent_PreservesWhitespaceAsTyped()
    {
        // Arrange
        var note = ManagerNote.CreateBlank();

        // Act
        note.UpdateContent("Notes\n\n  indented line\n\n");

        // Assert
        note.Content.Should().Be("Notes\n\n  indented line\n\n");
    }

    [Fact]
    public void UpdateContent_WithEmptyContent_FallsBackToDefaultTitle()
    {
        // Arrange
        var note = new ManagerNote("Something");

        // Act
        note.UpdateContent("   ");

        // Assert
        note.Title.Should().Be(ManagerNote.DefaultTitle);
    }

    [Theory]
    [InlineData("# Heading", "Heading")]
    [InlineData("### Deep heading", "Deep heading")]
    [InlineData("- bullet item", "bullet item")]
    [InlineData("1. first step", "first step")]
    [InlineData("- [ ] open task", "open task")]
    [InlineData("- [x] done task", "done task")]
    [InlineData("> quoted", "quoted")]
    [InlineData("**bold title**", "bold title")]
    [InlineData("`code title`", "code title")]
    public void DeriveTitle_StripsMarkdownDecoration(string content, string expected)
    {
        // Act
        var title = ManagerNote.DeriveTitle(content);

        // Assert
        title.Should().Be(expected);
    }

    [Fact]
    public void DeriveTitle_SkipsBlankAndRuleOnlyLines()
    {
        // Act
        var title = ManagerNote.DeriveTitle("\n---\n\nActual title\n");

        // Assert
        title.Should().Be("Actual title");
    }

    [Fact]
    public void DeriveTitle_TruncatesVeryLongFirstLine()
    {
        // Act
        var title = ManagerNote.DeriveTitle(new string('a', 250));

        // Assert
        title.Length.Should().Be(200);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeriveTitle_WithNoContent_ReturnsDefaultTitle(string? content)
    {
        // Act
        var title = ManagerNote.DeriveTitle(content);

        // Assert
        title.Should().Be(ManagerNote.DefaultTitle);
    }

    [Fact]
    public void MoveToFolder_FilesNoteInFolder()
    {
        // Arrange
        var note = new ManagerNote("Note");
        var folderId = Guid.NewGuid();

        // Act
        note.MoveToFolder(folderId);

        // Assert
        note.FolderId.Should().Be(folderId);
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveToFolder_WithNull_MovesNoteToRoot()
    {
        // Arrange
        var note = new ManagerNote("Note", folderId: Guid.NewGuid());

        // Act
        note.MoveToFolder(null);

        // Assert
        note.FolderId.Should().BeNull();
    }

    [Fact]
    public void Pin_MarksNotePinned()
    {
        // Arrange
        var note = new ManagerNote("Note");

        // Act
        note.Pin();

        // Assert
        note.IsPinned.Should().BeTrue();
        note.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Unpin_ClearsPin()
    {
        // Arrange
        var note = new ManagerNote("Note");
        note.Pin();

        // Act
        note.Unpin();

        // Assert
        note.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void TogglePin_FlipsPinnedState()
    {
        // Arrange
        var note = new ManagerNote("Note");

        // Act
        note.TogglePin();
        var afterFirstToggle = note.IsPinned;
        note.TogglePin();

        // Assert
        afterFirstToggle.Should().BeTrue();
        note.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void GetHotItems_ListsTheBulletsUnderTheHotHeading()
    {
        // Arrange
        var note = new ManagerNote("2026-09-28", "# Journal\n\n## hot 🔥\n- Write a downtime document\n- MA Pub incident\n\n## cold 🧊\n- Write a proposal");

        // Act
        var items = note.GetHotItems();

        // Assert
        items.Should().Equal("Write a downtime document", "MA Pub incident");
    }

    [Fact]
    public void GetHotItems_WithoutHotSection_ReturnsNothing()
    {
        // Arrange
        var note = new ManagerNote("2026-09-28", "# Journal\n\n## cold\n- Write a proposal");

        // Act & Assert
        note.GetHotItems().Should().BeEmpty();
    }

    [Fact]
    public void FindLatestJournalEntry_PicksTheGreatestTitle()
    {
        // Arrange
        var older = new ManagerNote("2026-09-24");
        var latest = new ManagerNote("2026-09-28");
        var earliest = new ManagerNote("2026-09-09");

        // Act
        var found = ManagerNote.FindLatestJournalEntry(new[] { older, latest, earliest });

        // Assert
        found.Should().BeSameAs(latest);
    }

    [Fact]
    public void FindLatestJournalEntry_WithNoNotes_ReturnsNull()
    {
        // Act & Assert
        ManagerNote.FindLatestJournalEntry(Array.Empty<ManagerNote>()).Should().BeNull();
    }
}
