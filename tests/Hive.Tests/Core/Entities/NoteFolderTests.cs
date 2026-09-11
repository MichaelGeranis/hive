using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class NoteFolderTests
{
    [Fact]
    public void Constructor_WithName_CreatesRootFolder()
    {
        // Act
        var folder = new NoteFolder("Team");

        // Assert
        folder.Id.Should().NotBe(Guid.Empty);
        folder.Name.Should().Be("Team");
        folder.ParentFolderId.Should().BeNull();
        folder.SortOrder.Should().Be(0);
        folder.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        folder.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithParent_NestsFolder()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var folder = new NoteFolder("Sub", parentId, 3);

        // Assert
        folder.ParentFolderId.Should().Be(parentId);
        folder.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        // Act
        var folder = new NoteFolder("  Team  ");

        // Assert
        folder.Name.Should().Be("Team");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        // Act
        var act = () => new NoteFolder(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithTooLongName_Throws()
    {
        // Act
        var act = () => new NoteFolder(new string('a', 101));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*100 characters*");
    }

    [Fact]
    public void Constructor_WithNegativeSortOrder_Throws()
    {
        // Act
        var act = () => new NoteFolder("Team", null, -1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_ChangesNameAndStampsUpdatedAt()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        folder.Rename("  Squad  ");

        // Assert
        folder.Name.Should().Be("Squad");
        folder.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Rename_WithEmptyName_Throws()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        var act = () => folder.Rename(" ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MoveTo_WithParentId_ReparentsFolder()
    {
        // Arrange
        var folder = new NoteFolder("Team");
        var parentId = Guid.NewGuid();

        // Act
        folder.MoveTo(parentId);

        // Assert
        folder.ParentFolderId.Should().Be(parentId);
        folder.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveTo_WithNull_MovesFolderToRoot()
    {
        // Arrange
        var folder = new NoteFolder("Team", Guid.NewGuid());

        // Act
        folder.MoveTo(null);

        // Assert
        folder.ParentFolderId.Should().BeNull();
    }

    [Fact]
    public void MoveTo_OwnId_Throws()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        var act = () => folder.MoveTo(folder.Id);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*its own parent*");
    }

    [Fact]
    public void Reorder_SetsSortOrder()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        folder.Reorder(4);

        // Assert
        folder.SortOrder.Should().Be(4);
        folder.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reorder_WithNegativeValue_Throws()
    {
        // Arrange
        var folder = new NoteFolder("Team");

        // Act
        var act = () => folder.Reorder(-2);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
