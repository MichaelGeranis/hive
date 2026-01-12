using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class SkillAssessmentTests
{
    private readonly Guid _validDirectReportId = Guid.NewGuid();
    private readonly Guid _validSkillId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesAssessment()
    {
        // Act
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Intermediate,
            ProficiencyLevel.Advanced,
            "Working on improving");

        // Assert
        assessment.Id.Should().NotBeEmpty();
        assessment.DirectReportId.Should().Be(_validDirectReportId);
        assessment.SkillId.Should().Be(_validSkillId);
        assessment.Level.Should().Be(ProficiencyLevel.Intermediate);
        assessment.TargetLevel.Should().Be(ProficiencyLevel.Advanced);
        assessment.Notes.Should().Be("Working on improving");
        assessment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithoutOptionalParams_UsesDefaults()
    {
        // Act
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.Beginner);

        // Assert
        assessment.TargetLevel.Should().BeNull();
        assessment.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new SkillAssessment(Guid.Empty, _validSkillId, ProficiencyLevel.Beginner);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId");
    }

    [Fact]
    public void Constructor_WithEmptySkillId_ThrowsArgumentException()
    {
        // Act
        var act = () => new SkillAssessment(_validDirectReportId, Guid.Empty, ProficiencyLevel.Beginner);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("skillId");
    }

    [Fact]
    public void Constructor_WithNoneLevel_ThrowsArgumentException()
    {
        // Act
        var act = () => new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.None);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("level");
    }

    [Fact]
    public void UpdateAssessment_WithValidData_UpdatesProperties()
    {
        // Arrange
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.Beginner);

        // Act
        assessment.UpdateAssessment(ProficiencyLevel.Advanced, ProficiencyLevel.Expert, "Great progress");

        // Assert
        assessment.Level.Should().Be(ProficiencyLevel.Advanced);
        assessment.TargetLevel.Should().Be(ProficiencyLevel.Expert);
        assessment.Notes.Should().Be("Great progress");
        assessment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAssessment_WithNoneLevel_ThrowsArgumentException()
    {
        // Arrange
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.Beginner);

        // Act
        var act = () => assessment.UpdateAssessment(ProficiencyLevel.None);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("level");
    }

    [Fact]
    public void GetSkillGap_WithNoTarget_ReturnsNull()
    {
        // Arrange
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.Intermediate);

        // Act
        var gap = assessment.GetSkillGap();

        // Assert
        gap.Should().BeNull();
    }

    [Fact]
    public void GetSkillGap_WithTargetNone_ReturnsNull()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Intermediate,
            ProficiencyLevel.None);

        // Act
        var gap = assessment.GetSkillGap();

        // Assert
        gap.Should().BeNull();
    }

    [Fact]
    public void GetSkillGap_WhenBelowTarget_ReturnsPositiveGap()
    {
        // Arrange - Intermediate (3) with target Expert (5)
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Intermediate,
            ProficiencyLevel.Expert);

        // Act
        var gap = assessment.GetSkillGap();

        // Assert
        gap.Should().Be(2); // Expert(5) - Intermediate(3) = 2
    }

    [Fact]
    public void GetSkillGap_WhenAtTarget_ReturnsZero()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Advanced,
            ProficiencyLevel.Advanced);

        // Act
        var gap = assessment.GetSkillGap();

        // Assert
        gap.Should().Be(0);
    }

    [Fact]
    public void GetSkillGap_WhenAboveTarget_ReturnsNegativeGap()
    {
        // Arrange - Expert (5) with target Intermediate (3)
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Expert,
            ProficiencyLevel.Intermediate);

        // Act
        var gap = assessment.GetSkillGap();

        // Assert
        gap.Should().Be(-2); // Intermediate(3) - Expert(5) = -2
    }

    [Fact]
    public void MeetsTarget_WithNoTarget_ReturnsTrue()
    {
        // Arrange
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, ProficiencyLevel.Beginner);

        // Act
        var meetsTarget = assessment.MeetsTarget();

        // Assert
        meetsTarget.Should().BeTrue();
    }

    [Fact]
    public void MeetsTarget_WithTargetNone_ReturnsTrue()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Beginner,
            ProficiencyLevel.None);

        // Act
        var meetsTarget = assessment.MeetsTarget();

        // Assert
        meetsTarget.Should().BeTrue();
    }

    [Fact]
    public void MeetsTarget_WhenBelowTarget_ReturnsFalse()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Beginner,
            ProficiencyLevel.Expert);

        // Act
        var meetsTarget = assessment.MeetsTarget();

        // Assert
        meetsTarget.Should().BeFalse();
    }

    [Fact]
    public void MeetsTarget_WhenAtTarget_ReturnsTrue()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Advanced,
            ProficiencyLevel.Advanced);

        // Act
        var meetsTarget = assessment.MeetsTarget();

        // Assert
        meetsTarget.Should().BeTrue();
    }

    [Fact]
    public void MeetsTarget_WhenAboveTarget_ReturnsTrue()
    {
        // Arrange
        var assessment = new SkillAssessment(
            _validDirectReportId,
            _validSkillId,
            ProficiencyLevel.Expert,
            ProficiencyLevel.Intermediate);

        // Act
        var meetsTarget = assessment.MeetsTarget();

        // Assert
        meetsTarget.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProficiencyLevel.Novice)]
    [InlineData(ProficiencyLevel.Beginner)]
    [InlineData(ProficiencyLevel.Intermediate)]
    [InlineData(ProficiencyLevel.Advanced)]
    [InlineData(ProficiencyLevel.Expert)]
    public void Constructor_AcceptsAllValidLevels(ProficiencyLevel level)
    {
        // Act
        var assessment = new SkillAssessment(_validDirectReportId, _validSkillId, level);

        // Assert
        assessment.Level.Should().Be(level);
    }
}
