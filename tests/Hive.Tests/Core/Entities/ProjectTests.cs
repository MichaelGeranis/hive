using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesProject()
    {
        // Arrange
        var name = "API Redesign";
        var description = "Redesign the REST API";
        var startDate = DateTime.UtcNow;
        var targetEndDate = DateTime.UtcNow.AddDays(90);

        // Act
        var project = new Project(name, description, startDate, targetEndDate);

        // Assert
        project.Id.Should().NotBeEmpty();
        project.Name.Should().Be(name);
        project.Description.Should().Be(description);
        project.Status.Should().Be(ProjectStatus.Planning);
        project.StartDate.Should().Be(startDate);
        project.TargetEndDate.Should().Be(targetEndDate);
        project.ActualEndDate.Should().BeNull();
        project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithOnlyName_UsesDefaults()
    {
        // Act
        var project = new Project("Simple Project");

        // Assert
        project.Description.Should().BeEmpty();
        project.StartDate.Should().BeNull();
        project.TargetEndDate.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var project = new Project("  Project Name  ", "  Description  ");

        // Assert
        project.Name.Should().Be("Project Name");
        project.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Project(name!);

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
        var act = () => new Project(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var project = new Project("Original", "Original desc");
        var newStartDate = DateTime.UtcNow.AddDays(1);
        var newTargetEndDate = DateTime.UtcNow.AddDays(100);

        // Act
        project.Update("Updated", "Updated desc", newStartDate, newTargetEndDate);

        // Assert
        project.Name.Should().Be("Updated");
        project.Description.Should().Be("Updated desc");
        project.StartDate.Should().Be(newStartDate);
        project.TargetEndDate.Should().Be(newTargetEndDate);
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var project = new Project("Original");

        // Act
        var act = () => project.Update("", null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Activate_FromPlanning_ChangesStatusToActive()
    {
        // Arrange
        var project = new Project("Project");

        // Act
        project.Activate();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
        project.StartDate.Should().NotBeNull();
        project.StartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_PreservesExistingStartDate()
    {
        // Arrange
        var originalStartDate = DateTime.UtcNow.AddDays(-10);
        var project = new Project("Project", null, originalStartDate, null);

        // Act
        project.Activate();

        // Assert
        project.StartDate.Should().Be(originalStartDate);
    }

    [Fact]
    public void Activate_FromOnHold_ChangesStatusToActive()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();
        project.PutOnHold();

        // Act
        project.Activate();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Activate_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();
        project.Complete();

        // Act
        var act = () => project.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void Activate_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Project");
        project.Cancel();

        // Act
        var act = () => project.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void PutOnHold_FromActive_ChangesStatusToOnHold()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();

        // Act
        project.PutOnHold();

        // Assert
        project.Status.Should().Be(ProjectStatus.OnHold);
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void PutOnHold_WhenNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Project");

        // Act
        var act = () => project.PutOnHold();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void Complete_FromActive_ChangesStatusToCompleted()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();

        // Act
        project.Complete();

        // Assert
        project.Status.Should().Be(ProjectStatus.Completed);
        project.ActualEndDate.Should().NotBeNull();
        project.ActualEndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromPlanning_ChangesStatusToCompleted()
    {
        // Arrange
        var project = new Project("Project");

        // Act
        project.Complete();

        // Assert
        project.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public void Complete_FromOnHold_ChangesStatusToCompleted()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();
        project.PutOnHold();

        // Act
        project.Complete();

        // Assert
        project.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Project");
        project.Cancel();

        // Act
        var act = () => project.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void Cancel_FromPlanning_ChangesStatusToCancelled()
    {
        // Arrange
        var project = new Project("Project");

        // Act
        project.Cancel();

        // Assert
        project.Status.Should().Be(ProjectStatus.Cancelled);
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromActive_ChangesStatusToCancelled()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();

        // Act
        project.Cancel();

        // Assert
        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromOnHold_ChangesStatusToCancelled()
    {
        // Arrange
        var project = new Project("Project");
        project.Activate();
        project.PutOnHold();

        // Act
        project.Cancel();

        // Assert
        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = new Project("Project");
        project.Complete();

        // Act
        var act = () => project.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }
}
