using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class ChecklistInstanceTests
{
    [Fact]
    public void CreateInterview_WithValidData_CreatesInterviewChecklist()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var title = "Software Engineer Interview";
        var candidateName = "John Doe";
        var position = "Senior Developer";
        var interviewDate = DateTime.UtcNow.AddDays(7);

        // Act
        var instance = ChecklistInstance.CreateInterview(
            templateId, title, candidateName, position, interviewDate);

        // Assert
        instance.Id.Should().NotBeEmpty();
        instance.TemplateId.Should().Be(templateId);
        instance.Type.Should().Be(ChecklistType.Interview);
        instance.Title.Should().Be(title);
        instance.Status.Should().Be(ChecklistInstanceStatus.NotStarted);
        instance.CandidateName.Should().Be(candidateName);
        instance.Position.Should().Be(position);
        instance.InterviewDate.Should().Be(interviewDate);
        instance.NewHireName.Should().BeNull();
        instance.StartDate.Should().BeNull();
        instance.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateOnboarding_WithValidData_CreatesOnboardingChecklist()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var title = "New Employee Onboarding";
        var newHireName = "Jane Smith";
        var startDate = DateTime.UtcNow.AddDays(14);
        var targetCompletion = DateTime.UtcNow.AddDays(44);

        // Act
        var instance = ChecklistInstance.CreateOnboarding(
            templateId, title, newHireName, startDate, targetCompletion);

        // Assert
        instance.Id.Should().NotBeEmpty();
        instance.TemplateId.Should().Be(templateId);
        instance.Type.Should().Be(ChecklistType.Onboarding);
        instance.Title.Should().Be(title);
        instance.Status.Should().Be(ChecklistInstanceStatus.NotStarted);
        instance.NewHireName.Should().Be(newHireName);
        instance.StartDate.Should().Be(startDate);
        instance.TargetCompletionDate.Should().Be(targetCompletion);
        instance.CandidateName.Should().BeNull();
        instance.InterviewDate.Should().BeNull();
    }

    [Fact]
    public void CreateInterview_TrimsWhitespace()
    {
        // Act
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(),
            "  Title  ",
            "  Candidate  ",
            "  Position  ",
            DateTime.UtcNow);

        // Assert
        instance.Title.Should().Be("Title");
        instance.CandidateName.Should().Be("Candidate");
        instance.Position.Should().Be("Position");
    }

    [Fact]
    public void CreateInterview_WithEmptyTemplateId_ThrowsArgumentException()
    {
        // Act
        var act = () => ChecklistInstance.CreateInterview(
            Guid.Empty, "Title", "Candidate", "Position", DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("templateId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateInterview_WithEmptyTitle_ThrowsArgumentException(string? title)
    {
        // Act
        var act = () => ChecklistInstance.CreateInterview(
            Guid.NewGuid(), title!, "Candidate", "Position", DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void CreateInterview_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 301);

        // Act
        var act = () => ChecklistInstance.CreateInterview(
            Guid.NewGuid(), longTitle, "Candidate", "Position", DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Start_FromNotStarted_ChangesToInProgress()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);

        // Act
        instance.Start();

        // Assert
        instance.Status.Should().Be(ChecklistInstanceStatus.InProgress);
        instance.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Start_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.Start();
        instance.Complete();

        // Act
        var act = () => instance.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Start_FromCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.Cancel();

        // Act
        var act = () => instance.Start();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromInProgress_ChangesToCompleted()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.Start();

        // Act
        instance.Complete();

        // Assert
        instance.Status.Should().Be(ChecklistInstanceStatus.Completed);
        instance.CompletedAt.Should().NotBeNull();
        instance.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_FromCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.Cancel();

        // Act
        var act = () => instance.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromNotStarted_ChangesToCancelled()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);

        // Act
        instance.Cancel();

        // Assert
        instance.Status.Should().Be(ChecklistInstanceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.Start();
        instance.Complete();

        // Act
        var act = () => instance.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateNotes_UpdatesNotesProperty()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        var notes = "Some interview notes";

        // Act
        instance.UpdateNotes(notes);

        // Assert
        instance.Notes.Should().Be(notes);
        instance.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateNotes_WithNull_DefaultsToEmpty()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);
        instance.UpdateNotes("Some notes");

        // Act
        instance.UpdateNotes(null!);

        // Assert
        instance.Notes.Should().BeEmpty();
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesTitle()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Original Title", "Candidate", "Position", DateTime.UtcNow);

        // Act
        instance.UpdateTitle("New Title");

        // Assert
        instance.Title.Should().Be("New Title");
        instance.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateTitle_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var instance = ChecklistInstance.CreateInterview(
            Guid.NewGuid(), "Title", "Candidate", "Position", DateTime.UtcNow);

        // Act
        var act = () => instance.UpdateTitle("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }
}
