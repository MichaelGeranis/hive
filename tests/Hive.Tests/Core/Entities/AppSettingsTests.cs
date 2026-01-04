using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_WithValidMappings_CreatesAppSettings()
    {
        // Arrange
        var mappings = "[{\"points\":1,\"hours\":2},{\"points\":2,\"hours\":4}]";

        // Act
        var settings = new AppSettings(mappings);

        // Assert
        settings.Id.Should().NotBeEmpty();
        settings.StoryPointMappings.Should().Be(mappings);
        settings.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        settings.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullMappings_UsesEmptyArrayAsDefault()
    {
        // Act
        var settings = new AppSettings(null!);

        // Assert
        settings.StoryPointMappings.Should().Be("[]");
    }

    [Fact]
    public void Constructor_WithEmptyString_StoresEmptyString()
    {
        // Act
        var settings = new AppSettings("");

        // Assert
        settings.StoryPointMappings.Should().Be("");
    }

    [Fact]
    public void UpdateStoryPointMappings_WithValidMappings_UpdatesMappings()
    {
        // Arrange
        var settings = new AppSettings("[]");
        var newMappings = "[{\"points\":3,\"hours\":6}]";

        // Act
        settings.UpdateStoryPointMappings(newMappings);

        // Assert
        settings.StoryPointMappings.Should().Be(newMappings);
        settings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateStoryPointMappings_WithNullMappings_UsesEmptyArrayAsDefault()
    {
        // Arrange
        var settings = new AppSettings("[{\"points\":1,\"hours\":2}]");

        // Act
        settings.UpdateStoryPointMappings(null!);

        // Assert
        settings.StoryPointMappings.Should().Be("[]");
    }

    [Fact]
    public void UpdateStoryPointMappings_MultipleUpdates_UpdatesTimestamp()
    {
        // Arrange
        var settings = new AppSettings("[]");
        var firstUpdate = "[{\"points\":1,\"hours\":2}]";
        var secondUpdate = "[{\"points\":2,\"hours\":4}]";

        // Act
        settings.UpdateStoryPointMappings(firstUpdate);
        var firstUpdateTime = settings.UpdatedAt;

        Thread.Sleep(10); // Small delay to ensure different timestamp

        settings.UpdateStoryPointMappings(secondUpdate);
        var secondUpdateTime = settings.UpdatedAt;

        // Assert
        settings.StoryPointMappings.Should().Be(secondUpdate);
        secondUpdateTime.Should().BeAfter(firstUpdateTime!.Value);
    }
}
