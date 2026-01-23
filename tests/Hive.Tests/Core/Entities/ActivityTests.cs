using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ActivityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesActivity()
    {
        // Arrange
        var activityType = ActivityType.Created;
        var entityType = EntityType.DirectReport;
        var entityId = Guid.NewGuid();
        var entityName = "John Doe";
        var description = "Created new team member";

        // Act
        var activity = new Activity(activityType, entityType, entityId, entityName, description);

        // Assert
        activity.Id.Should().NotBeEmpty();
        activity.ActivityType.Should().Be(activityType);
        activity.EntityType.Should().Be(entityType);
        activity.EntityId.Should().Be(entityId);
        activity.EntityName.Should().Be(entityName);
        activity.Description.Should().Be(description);
        activity.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        activity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithCustomTimestamp_UsesProvidedTimestamp()
    {
        // Arrange
        var customTimestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var activity = new Activity(
            ActivityType.Updated,
            EntityType.Task,
            Guid.NewGuid(),
            "Task Name",
            "Updated task",
            customTimestamp);

        // Assert
        activity.Timestamp.Should().Be(customTimestamp);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var activity = new Activity(
            ActivityType.Created,
            EntityType.Project,
            Guid.NewGuid(),
            "  Project Name  ",
            "  Created project  ");

        // Assert
        activity.EntityName.Should().Be("Project Name");
        activity.Description.Should().Be("Created project");
    }

    [Fact]
    public void Constructor_WithEmptyEntityId_ThrowsArgumentException()
    {
        // Act
        var act = () => new Activity(
            ActivityType.Created,
            EntityType.DirectReport,
            Guid.Empty,
            "Name",
            "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEntityName_ThrowsArgumentException(string? entityName)
    {
        // Act
        var act = () => new Activity(
            ActivityType.Created,
            EntityType.DirectReport,
            Guid.NewGuid(),
            entityName!,
            "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityName");
    }

    [Fact]
    public void Constructor_WithEntityNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 501);

        // Act
        var act = () => new Activity(
            ActivityType.Created,
            EntityType.DirectReport,
            Guid.NewGuid(),
            longName,
            "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDescription_ThrowsArgumentException(string? description)
    {
        // Act
        var act = () => new Activity(
            ActivityType.Created,
            EntityType.DirectReport,
            Guid.NewGuid(),
            "Name",
            description!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithDescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longDescription = new string('a', 1001);

        // Act
        var act = () => new Activity(
            ActivityType.Created,
            EntityType.DirectReport,
            Guid.NewGuid(),
            "Name",
            longDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Theory]
    [InlineData(ActivityType.Created)]
    [InlineData(ActivityType.Updated)]
    [InlineData(ActivityType.StatusChanged)]
    [InlineData(ActivityType.Approved)]
    [InlineData(ActivityType.Rejected)]
    [InlineData(ActivityType.Completed)]
    [InlineData(ActivityType.Deleted)]
    [InlineData(ActivityType.Cancelled)]
    public void Constructor_WithAllActivityTypes_CreatesActivity(ActivityType activityType)
    {
        // Act
        var activity = new Activity(
            activityType,
            EntityType.Task,
            Guid.NewGuid(),
            "Task",
            "Activity occurred");

        // Assert
        activity.ActivityType.Should().Be(activityType);
    }

    [Theory]
    [InlineData(EntityType.Review)]
    [InlineData(EntityType.Task)]
    [InlineData(EntityType.Leave)]
    [InlineData(EntityType.DirectReport)]
    [InlineData(EntityType.Meeting)]
    [InlineData(EntityType.Project)]
    public void Constructor_WithVariousEntityTypes_CreatesActivity(EntityType entityType)
    {
        // Act
        var activity = new Activity(
            ActivityType.Created,
            entityType,
            Guid.NewGuid(),
            "Entity",
            "Created entity");

        // Assert
        activity.EntityType.Should().Be(entityType);
    }
}
