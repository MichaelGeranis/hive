using FluentAssertions;
using Hive.Core.Entities;

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

    #region Jira Tests

    [Fact]
    public void FromJira_SetsKeyAndName()
    {
        var parent = Parent.FromJira(" mar-12 ", "Epic", "Payments revamp");

        parent.JiraKey.Should().Be("MAR-12");
        parent.Name.Should().Be("Payments revamp");
        parent.Labels.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("MAR")]
    [InlineData("MAR-0")]
    [InlineData("12-MAR")]
    [InlineData("MAR 12")]
    public void FromJira_WithInvalidKey_Throws(string key)
    {
        var act = () => Parent.FromJira(key, "Epic", "Payments");

        act.Should().Throw<ArgumentException>().WithMessage("*Jira issue key*");
    }

    [Fact]
    public void SyncFromJira_UpdatesNameButKeepsLabels()
    {
        var parent = new Parent("Payments", "payments,billing");

        parent.SyncFromJira("MAR-12", "Epic", "Payments revamp");

        parent.JiraKey.Should().Be("MAR-12");
        parent.Name.Should().Be("Payments revamp");
        parent.Labels.Should().Be("payments,billing");
        parent.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void FromJira_KeepsTheIssueType()
    {
        Parent.FromJira("MAR-111", " Story ", "Backfill created_at").JiraIssueType.Should().Be("Story");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromJira_WithoutIssueType_Throws(string issueType)
    {
        var act = () => Parent.FromJira("MAR-111", issueType, "Backfill created_at");

        act.Should().Throw<ArgumentException>().WithMessage("*issue type*");
    }

    [Fact]
    public void SyncFromJira_UpdatesTheIssueType()
    {
        var parent = Parent.FromJira("MAR-202", "Story", "Fargate Spot");

        parent.SyncFromJira("MAR-202", "Task", "Fargate Spot");

        parent.JiraIssueType.Should().Be("Task");
    }

    [Fact]
    public void SyncFromJira_WithADifferentEpic_Throws()
    {
        var parent = Parent.FromJira("MAR-12", "Epic", "Payments");

        var act = () => parent.SyncFromJira("MAR-13", "Epic", "Payments");

        act.Should().Throw<InvalidOperationException>().WithMessage("*MAR-12*MAR-13*");
    }

    [Fact]
    public void MatchesJira_ComparesKeyTypeAndName()
    {
        var parent = Parent.FromJira("MAR-12", "Epic", "Payments");

        parent.MatchesJira("mar-12", "Epic", "Payments ").Should().BeTrue();
        parent.MatchesJira("MAR-12", "Epic", "Payments revamp").Should().BeFalse();
        parent.MatchesJira("MAR-12", "Story", "Payments").Should().BeFalse("the issue type changed");
        new Parent("Payments").MatchesJira("MAR-12", "Epic", "Payments").Should().BeFalse("it is not linked yet");
    }

    [Fact]
    public void FromJira_KeepsComponentsTrimmedAndDistinct()
    {
        var parent = Parent.FromJira("MAR-12", "Epic", "Payments", new[] { " Matching ", "IPI", "matching", "" });

        parent.Components.Should().Be("Matching,IPI");
    }

    [Fact]
    public void Constructor_HasNoComponents()
    {
        new Parent("Payments").Components.Should().BeEmpty("only the Jira sync sets components");
    }

    [Fact]
    public void SyncFromJira_ReplacesComponents()
    {
        var parent = Parent.FromJira("MAR-12", "Epic", "Payments", new[] { "Matching" });

        parent.SyncFromJira("MAR-12", "Epic", "Payments", new[] { "IPI" });

        parent.Components.Should().Be("IPI");
    }

    [Fact]
    public void MatchesJira_ComparesComponents()
    {
        var parent = Parent.FromJira("MAR-12", "Epic", "Payments", new[] { "Matching" });

        parent.MatchesJira("MAR-12", "Epic", "Payments", new[] { "Matching" }).Should().BeTrue();
        parent.MatchesJira("MAR-12", "Epic", "Payments", new[] { "Matching", "IPI" }).Should().BeFalse();
        parent.MatchesJira("MAR-12", "Epic", "Payments").Should().BeFalse("the components were removed in Jira");
    }

    [Fact]
    public void FromJira_WithComponentsOver1000Characters_Throws()
    {
        var components = Enumerable.Range(0, 100).Select(i => $"Component number {i}");

        var act = () => Parent.FromJira("MAR-12", "Epic", "Payments", components);

        act.Should().Throw<ArgumentException>().WithParameterName("components");
    }

    #endregion
}
