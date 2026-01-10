using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SkillAssessmentRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SkillAssessmentRepository _repository;
    private readonly Guid _directReportId;
    private readonly Guid _skillId;

    public SkillAssessmentRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SkillAssessmentRepository(_context);
        _directReportId = Guid.NewGuid();
        _skillId = Guid.NewGuid();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillAssessmentRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();

        // Act
        var result = await _repository.GetByIdAsync(assessment.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assessment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDirectReportAndSkillAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        CreateAndAddAssessment();

        // Act
        var result = await _repository.GetByDirectReportAndSkillAsync(_directReportId, _skillId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(_directReportId);
        result.SkillId.Should().Be(_skillId);
    }

    [Fact]
    public async Task GetByDirectReportAndSkillAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByDirectReportAndSkillAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAssessments()
    {
        // Arrange
        CreateAndAddAssessment();
        CreateAndAddAssessment(skillId: Guid.NewGuid());

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDirectReportIdThenSkillId()
    {
        // Arrange
        var report1 = new Guid("11111111-1111-1111-1111-111111111111");
        var report2 = new Guid("22222222-2222-2222-2222-222222222222");
        var skill1 = new Guid("11111111-1111-1111-1111-111111111111");
        var skill2 = new Guid("22222222-2222-2222-2222-222222222222");

        CreateAndAddAssessment(directReportId: report2, skillId: skill2);
        CreateAndAddAssessment(directReportId: report1, skillId: skill2);
        CreateAndAddAssessment(directReportId: report1, skillId: skill1);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].DirectReportId.Should().Be(report1);
        result[0].SkillId.Should().Be(skill1);
        result[1].DirectReportId.Should().Be(report1);
        result[1].SkillId.Should().Be(skill2);
        result[2].DirectReportId.Should().Be(report2);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingAssessments()
    {
        // Arrange
        var otherReportId = Guid.NewGuid();
        CreateAndAddAssessment();
        CreateAndAddAssessment(skillId: Guid.NewGuid());
        CreateAndAddAssessment(directReportId: otherReportId);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(_directReportId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.DirectReportId.Should().Be(_directReportId));
    }

    [Fact]
    public async Task GetBySkillIdAsync_ReturnsMatchingAssessments()
    {
        // Arrange
        var otherSkillId = Guid.NewGuid();
        CreateAndAddAssessment();
        CreateAndAddAssessment(directReportId: Guid.NewGuid());
        CreateAndAddAssessment(skillId: otherSkillId);

        // Act
        var result = await _repository.GetBySkillIdAsync(_skillId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.SkillId.Should().Be(_skillId));
    }

    [Fact]
    public async Task AddAsync_AddsAssessmentToContext()
    {
        // Arrange
        var assessment = new SkillAssessment(_directReportId, _skillId, ProficiencyLevel.Intermediate);

        // Act
        var result = await _repository.AddAsync(assessment);

        // Assert
        result.Should().Be(assessment);
        _context.SkillAssessments.Should().ContainKey(assessment.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();

        // Act
        var act = () => _repository.AddAsync(assessment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAssessmentInContext()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();
        assessment.UpdateAssessment(ProficiencyLevel.Advanced, ProficiencyLevel.Expert, "Updated notes");

        // Act
        await _repository.UpdateAsync(assessment);

        // Assert
        var stored = _context.SkillAssessments[assessment.Id];
        stored.Level.Should().Be(ProficiencyLevel.Advanced);
        stored.TargetLevel.Should().Be(ProficiencyLevel.Expert);
        stored.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentAssessment_ThrowsInvalidOperationException()
    {
        // Arrange
        var assessment = new SkillAssessment(_directReportId, _skillId, ProficiencyLevel.Intermediate);

        // Act
        var act = () => _repository.UpdateAsync(assessment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAssessmentFromContext()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();

        // Act
        await _repository.DeleteAsync(assessment.Id);

        // Assert
        _context.SkillAssessments.Should().NotContainKey(assessment.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();

        // Act
        var result = await _repository.ExistsAsync(assessment.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssessmentExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        CreateAndAddAssessment();

        // Act
        var result = await _repository.AssessmentExistsAsync(_directReportId, _skillId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AssessmentExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.AssessmentExistsAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssessmentExistsAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var assessment = CreateAndAddAssessment();

        // Act
        var result = await _repository.AssessmentExistsAsync(_directReportId, _skillId, excludeId: assessment.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssessmentExistsAsync_FindsDuplicateWhenExcludingDifferentId()
    {
        // Arrange
        CreateAndAddAssessment();
        var otherId = Guid.NewGuid();

        // Act
        var result = await _repository.AssessmentExistsAsync(_directReportId, _skillId, excludeId: otherId);

        // Assert
        result.Should().BeTrue();
    }

    private SkillAssessment CreateAndAddAssessment(
        Guid? directReportId = null,
        Guid? skillId = null,
        ProficiencyLevel level = ProficiencyLevel.Intermediate)
    {
        var assessment = new SkillAssessment(
            directReportId ?? _directReportId,
            skillId ?? _skillId,
            level,
            null,
            "Test assessment");
        _context.SkillAssessments.TryAdd(assessment.Id, assessment);
        return assessment;
    }
}
