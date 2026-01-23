using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ChecklistTemplateItemTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var sortOrder = 1;
        var content = "Interview question about experience";
        var itemType = ChecklistItemType.Question;
        var isRequired = true;
        var helpText = "Ask about past projects";
        var estimatedMinutes = 10;

        // Act
        var item = new ChecklistTemplateItem(
            templateId, sortOrder, content, itemType, isRequired, helpText, estimatedMinutes);

        // Assert
        item.Id.Should().NotBeEmpty();
        item.TemplateId.Should().Be(templateId);
        item.SortOrder.Should().Be(sortOrder);
        item.Content.Should().Be(content);
        item.ItemType.Should().Be(itemType);
        item.IsRequired.Should().Be(isRequired);
        item.HelpText.Should().Be(helpText);
        item.EstimatedMinutes.Should().Be(estimatedMinutes);
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        item.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMinimalData_CreatesItem()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        // Act
        var item = new ChecklistTemplateItem(templateId, 0, "Content", ChecklistItemType.Task);

        // Assert
        item.IsRequired.Should().BeTrue();
        item.HelpText.Should().BeNull();
        item.EstimatedMinutes.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(),
            1,
            "  Content here  ",
            ChecklistItemType.Task,
            true,
            "  Help text  ");

        // Assert
        item.Content.Should().Be("Content here");
        item.HelpText.Should().Be("Help text");
    }

    [Fact]
    public void Constructor_WithEmptyTemplateId_ThrowsArgumentException()
    {
        // Act
        var act = () => new ChecklistTemplateItem(
            Guid.Empty, 0, "Content", ChecklistItemType.Task);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("templateId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyContent_ThrowsArgumentException(string? content)
    {
        // Act
        var act = () => new ChecklistTemplateItem(
            Guid.NewGuid(), 0, content!, ChecklistItemType.Task);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void Constructor_WithContentTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longContent = new string('a', 2001);

        // Act
        var act = () => new ChecklistTemplateItem(
            Guid.NewGuid(), 0, longContent, ChecklistItemType.Task);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void Constructor_WithNegativeSortOrder_ThrowsArgumentException()
    {
        // Act
        var act = () => new ChecklistTemplateItem(
            Guid.NewGuid(), -1, "Content", ChecklistItemType.Task);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sortOrder");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(), 0, "Original", ChecklistItemType.Task);

        // Act
        item.Update(5, "Updated content", ChecklistItemType.Question, false, "New help", 30);

        // Assert
        item.SortOrder.Should().Be(5);
        item.Content.Should().Be("Updated content");
        item.ItemType.Should().Be(ChecklistItemType.Question);
        item.IsRequired.Should().BeFalse();
        item.HelpText.Should().Be("New help");
        item.EstimatedMinutes.Should().Be(30);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(), 0, "Original", ChecklistItemType.Task);

        // Act
        var act = () => item.Update(0, "", ChecklistItemType.Task, true, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("content");
    }

    [Fact]
    public void UpdateSortOrder_WithValidValue_UpdatesSortOrder()
    {
        // Arrange
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(), 0, "Content", ChecklistItemType.Task);

        // Act
        item.UpdateSortOrder(10);

        // Assert
        item.SortOrder.Should().Be(10);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSortOrder_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(), 0, "Content", ChecklistItemType.Task);

        // Act
        var act = () => item.UpdateSortOrder(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sortOrder");
    }

    [Theory]
    [InlineData(ChecklistItemType.Question)]
    [InlineData(ChecklistItemType.Topic)]
    [InlineData(ChecklistItemType.Task)]
    [InlineData(ChecklistItemType.Document)]
    [InlineData(ChecklistItemType.Training)]
    public void Constructor_WithAllItemTypes_CreatesItem(ChecklistItemType itemType)
    {
        // Act
        var item = new ChecklistTemplateItem(
            Guid.NewGuid(), 0, "Content", itemType);

        // Assert
        item.ItemType.Should().Be(itemType);
    }
}
