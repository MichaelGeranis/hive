using Hive.Core.Entities;
using FluentAssertions;

namespace Hive.Tests.Core.Entities;

/// <summary>
/// Tests for Parent entity.
/// </summary>
public class ParentTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidName_CreatesParent()
    {
        // Act
        var parent = new Parent("Epic 1");

        // Assert
        parent.Should().NotBeNull();
        parent.Name.Should().Be("Epic 1");
        parent.Labels.Should().BeEmpty();
        parent.Id.Should().NotBe(Guid.Empty);
        parent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        parent.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNameAndLabels_CreatesParentWithLabels()
    {
        // Act
        var parent = new Parent("Epic 1", "label1,label2");

        // Assert
        parent.Name.Should().Be("Epic 1");
        parent.Labels.Should().Be("label1,label2");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var parent = new Parent("  Epic 1  ", "  label1  ");

        // Assert
        parent.Name.Should().Be("Epic 1");
        parent.Labels.Should().Be("label1");
    }

    [Fact]
    public void Constructor_WithNullLabels_SetsLabelsToEmpty()
    {
        // Act
        var parent = new Parent("Epic 1", null);

        // Assert
        parent.Labels.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Parent(name!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Parent name cannot be empty.*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 501);

        // Act
        var act = () => new Parent(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Parent name cannot exceed 500 characters.*")
            .And.ParamName.Should().Be("name");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_UpdatesNameAndLabels()
    {
        // Arrange
        var parent = new Parent("Epic 1", "label1");
        var originalCreatedAt = parent.CreatedAt;

        // Act
        parent.Update("Updated Epic", "label2,label3");

        // Assert
        parent.Name.Should().Be("Updated Epic");
        parent.Labels.Should().Be("label2,label3");
        parent.UpdatedAt.Should().NotBeNull();
        parent.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        parent.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Update_TrimsWhitespace()
    {
        // Arrange
        var parent = new Parent("Epic 1");

        // Act
        parent.Update("  Updated  ", "  labels  ");

        // Assert
        parent.Name.Should().Be("Updated");
        parent.Labels.Should().Be("labels");
    }

    [Fact]
    public void Update_WithNullLabels_SetsLabelsToEmpty()
    {
        // Arrange
        var parent = new Parent("Epic 1", "label1");

        // Act
        parent.Update("Updated", null);

        // Assert
        parent.Labels.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var parent = new Parent("Epic 1");

        // Act
        var act = () => parent.Update(name!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Parent name cannot be empty.*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Update_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var parent = new Parent("Epic 1");
        var longName = new string('a', 501);

        // Act
        var act = () => parent.Update(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Parent name cannot exceed 500 characters.*")
            .And.ParamName.Should().Be("name");
    }

    #endregion
}
