using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class InitiativeTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInitiative()
    {
        // Arrange
        var quarterId = Guid.NewGuid();
        var name = "New API Feature";
        var color = "#3B82F6";
        var description = "Implement the new API endpoints";
        var projectId = Guid.NewGuid();
        var tshirtSize = "L";

        // Act
        var initiative = new Initiative(quarterId, name, color, description, projectId, tshirtSize);

        // Assert
        initiative.Id.Should().NotBeEmpty();
        initiative.QuarterId.Should().Be(quarterId);
        initiative.Name.Should().Be(name);
        initiative.Color.Should().Be(color);
        initiative.Description.Should().Be(description);
        initiative.ProjectId.Should().Be(projectId);
        initiative.TshirtSize.Should().Be("L");
        initiative.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        initiative.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMinimalData_CreatesInitiative()
    {
        // Arrange & Act
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF");

        // Assert
        initiative.Description.Should().BeEmpty();
        initiative.ProjectId.Should().BeNull();
        initiative.TshirtSize.Should().Be("M");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var initiative = new Initiative(
            Guid.NewGuid(),
            "  Initiative Name  ",
            "#FFF",
            "  Description  ");

        // Assert
        initiative.Name.Should().Be("Initiative Name");
        initiative.Description.Should().Be("Description");
    }

    [Fact]
    public void Constructor_NormalizesToUppercaseTshirtSize()
    {
        // Act
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF", null, null, "xl");

        // Assert
        initiative.TshirtSize.Should().Be("XL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Initiative(Guid.NewGuid(), name!, "#FFF");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act
        var act = () => new Initiative(Guid.NewGuid(), longName, "#FFF");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Theory]
    [InlineData("red")]
    [InlineData("invalid")]
    [InlineData("FFF")]
    public void Constructor_WithInvalidColorFormat_ThrowsArgumentException(string color)
    {
        // Act
        var act = () => new Initiative(Guid.NewGuid(), "Name", color);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("color");
    }

    [Theory]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public void Constructor_WithInvalidColorLength_ThrowsArgumentException(string color)
    {
        // Act
        var act = () => new Initiative(Guid.NewGuid(), "Name", color);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("color");
    }

    [Fact]
    public void Constructor_WithInvalidHexCharacters_ThrowsArgumentException()
    {
        // Act
        var act = () => new Initiative(Guid.NewGuid(), "Name", "#GGGGGG");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("color");
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#fff")]
    [InlineData("#FFFFFF")]
    [InlineData("#ffffff")]
    [InlineData("#3B82F6")]
    [InlineData("#abc")]
    public void Constructor_WithValidColorFormats_CreatesInitiative(string color)
    {
        // Act
        var initiative = new Initiative(Guid.NewGuid(), "Name", color);

        // Assert
        initiative.Color.Should().Be(color);
    }

    [Theory]
    [InlineData("XXS")]
    [InlineData("XS")]
    [InlineData("XXL")]
    [InlineData("Invalid")]
    public void Constructor_WithInvalidTshirtSize_ThrowsArgumentException(string size)
    {
        // Act
        var act = () => new Initiative(Guid.NewGuid(), "Name", "#FFF", null, null, size);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tshirtSize");
    }

    [Theory]
    [InlineData("S")]
    [InlineData("M")]
    [InlineData("L")]
    [InlineData("XL")]
    [InlineData("s")]
    [InlineData("m")]
    [InlineData("l")]
    [InlineData("xl")]
    public void Constructor_WithValidTshirtSizes_CreatesInitiative(string size)
    {
        // Act
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF", null, null, size);

        // Assert
        initiative.TshirtSize.Should().Be(size.ToUpperInvariant());
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Original", "#FFF");
        var newName = "Updated Name";
        var newDescription = "Updated description";
        var newColor = "#000";
        var newProjectId = Guid.NewGuid();
        var newSize = "XL";

        // Act
        initiative.Update(newName, newDescription, newColor, newProjectId, newSize);

        // Assert
        initiative.Name.Should().Be(newName);
        initiative.Description.Should().Be(newDescription);
        initiative.Color.Should().Be(newColor);
        initiative.ProjectId.Should().Be(newProjectId);
        initiative.TshirtSize.Should().Be("XL");
        initiative.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithNullColor_KeepsExistingColor()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF");

        // Act
        initiative.Update("New Name", null, null, null);

        // Assert
        initiative.Color.Should().Be("#FFF");
    }

    [Fact]
    public void Update_WithNullTshirtSize_KeepsExistingSize()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF", null, null, "L");

        // Act
        initiative.Update("New Name", null, null, null, null);

        // Assert
        initiative.TshirtSize.Should().Be("L");
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Name", "#FFF");

        // Act
        var act = () => initiative.Update("", null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AvailableColors_ContainsExpectedColors()
    {
        // Assert
        Initiative.AvailableColors.Should().NotBeEmpty();
        Initiative.AvailableColors.Should().Contain("#3B82F6");
        Initiative.AvailableColors.Should().Contain("#10B981");
        Initiative.AvailableColors.Should().HaveCountGreaterThan(5);
    }
}
