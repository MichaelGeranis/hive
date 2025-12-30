using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Application.Services;

public class SkillAssessmentServiceTests
{
    private readonly Mock<ISkillAssessmentRepository> _assessmentRepositoryMock;
    private readonly Mock<ISkillRepository> _skillRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly SkillAssessmentService _service;

    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly Guid _testSkillId = Guid.NewGuid();
    private readonly DirectReport _testDirectReport;
    private readonly Skill _testSkill;

    public SkillAssessmentServiceTests()
    {
        _assessmentRepositoryMock = new Mock<ISkillAssessmentRepository>();
        _skillRepositoryMock = new Mock<ISkillRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _service = new SkillAssessmentService(
            _assessmentRepositoryMock.Object,
            _skillRepositoryMock.Object,
            _directReportRepositoryMock.Object);

        _testDirectReport = CreateDirectReport();
        _testSkill = CreateSkill();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAssessmentRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillAssessmentService(
            null!,
            _skillRepositoryMock.Object,
            _directReportRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("assessmentRepository");
    }

    [Fact]
    public void Constructor_WithNullSkillRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillAssessmentService(
            _assessmentRepositoryMock.Object,
            null!,
            _directReportRepositoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("skillRepository");
    }

    [Fact]
    public void Constructor_WithNullDirectReportRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillAssessmentService(
            _assessmentRepositoryMock.Object,
            _skillRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("directReportRepository");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var assessment = CreateAssessment();
        _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(assessment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);

        // Act
        var result = await _service.GetByIdAsync(assessment.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assessment.Id);
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.SkillId.Should().Be(_testSkillId);
        result.Level.Should().Be(ProficiencyLevel.Intermediate);
        result.LevelName.Should().Be("Intermediate");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessment?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var assessments = new List<SkillAssessment>
        {
            CreateAssessment(),
            CreateAssessment()
        };
        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByDirectReportIdAsync Tests

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var assessments = new List<SkillAssessment>
        {
            CreateAssessment(),
            CreateAssessment()
        };
        _assessmentRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.GetByDirectReportIdAsync(_testDirectReportId);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.DirectReportId == _testDirectReportId).Should().BeTrue();
    }

    #endregion

    #region GetBySkillIdAsync Tests

    [Fact]
    public async Task GetBySkillIdAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var assessments = new List<SkillAssessment>
        {
            CreateAssessment(),
            CreateAssessment()
        };
        _assessmentRepositoryMock.Setup(r => r.GetBySkillIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.GetBySkillIdAsync(_testSkillId);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.SkillId == _testSkillId).Should().BeTrue();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAssessment()
    {
        // Arrange
        var dto = new CreateSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            SkillId = _testSkillId,
            Level = ProficiencyLevel.Intermediate,
            TargetLevel = ProficiencyLevel.Advanced,
            Notes = "Test notes"
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);
        _assessmentRepositoryMock.Setup(r => r.AssessmentExistsAsync(_testDirectReportId, _testSkillId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assessmentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SkillAssessment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessment a, CancellationToken ct) => a);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.SkillId.Should().Be(_testSkillId);
        result.Level.Should().Be(ProficiencyLevel.Intermediate);
        result.TargetLevel.Should().Be(ProficiencyLevel.Advanced);
        result.SkillGap.Should().Be(1); // Advanced (4) - Intermediate (3) = 1
        result.MeetsTarget.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDirectReport_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = _testSkillId,
            Level = ProficiencyLevel.Intermediate
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"DirectReport with id {dto.DirectReportId} not found");
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentSkill_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(dto.SkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Skill with id {dto.SkillId} not found");
    }

    [Fact]
    public async Task CreateAsync_WithInactiveSkill_ThrowsInvalidOperationException()
    {
        // Arrange
        var inactiveSkill = CreateSkill();
        inactiveSkill.Deactivate();

        var dto = new CreateSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            SkillId = _testSkillId,
            Level = ProficiencyLevel.Intermediate
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveSkill);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot assess an inactive skill.");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAssessment_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            SkillId = _testSkillId,
            Level = ProficiencyLevel.Intermediate
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);
        _assessmentRepositoryMock.Setup(r => r.AssessmentExistsAsync(_testDirectReportId, _testSkillId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("An assessment for this skill and direct report already exists.");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAssessment()
    {
        // Arrange
        var assessment = CreateAssessment();
        var dto = new UpdateSkillAssessmentDto
        {
            Level = ProficiencyLevel.Advanced,
            TargetLevel = ProficiencyLevel.Expert,
            Notes = "Updated notes"
        };

        _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(assessment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessment);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);

        // Act
        var result = await _service.UpdateAsync(assessment.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Level.Should().Be(ProficiencyLevel.Advanced);
        result.TargetLevel.Should().Be(ProficiencyLevel.Expert);
        result.Notes.Should().Be("Updated notes");
        _assessmentRepositoryMock.Verify(r => r.UpdateAsync(assessment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        var dto = new UpdateSkillAssessmentDto
        {
            Level = ProficiencyLevel.Advanced
        };

        _assessmentRepositoryMock.Setup(r => r.GetByIdAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessment?)null);

        // Act
        var act = () => _service.UpdateAsync(assessmentId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"SkillAssessment with id {assessmentId} not found");
    }

    #endregion

    #region BulkAssessAsync Tests

    [Fact]
    public async Task BulkAssessAsync_CreatesNewAssessments()
    {
        // Arrange
        var skill2Id = Guid.NewGuid();
        var skill2 = new Skill("TypeScript", "Programming language", SkillCategory.Technical);
        SetPropertyValue(skill2, "Id", skill2Id);

        var dto = new BulkSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            Assessments = new List<SkillLevelDto>
            {
                new SkillLevelDto { SkillId = _testSkillId, Level = ProficiencyLevel.Intermediate, TargetLevel = ProficiencyLevel.Advanced },
                new SkillLevelDto { SkillId = skill2Id, Level = ProficiencyLevel.Beginner, TargetLevel = ProficiencyLevel.Intermediate }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(skill2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill2);
        _assessmentRepositoryMock.Setup(r => r.GetByDirectReportAndSkillAsync(_testDirectReportId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessment?)null);
        _assessmentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SkillAssessment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessment a, CancellationToken ct) => a);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill, skill2 });

        // Act
        var result = await _service.BulkAssessAsync(dto);

        // Assert
        result.Should().HaveCount(2);
        _assessmentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SkillAssessment>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkAssessAsync_UpdatesExistingAssessments()
    {
        // Arrange
        var existingAssessment = CreateAssessment();
        var dto = new BulkSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            Assessments = new List<SkillLevelDto>
            {
                new SkillLevelDto { SkillId = _testSkillId, Level = ProficiencyLevel.Advanced, TargetLevel = ProficiencyLevel.Expert }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(_testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testSkill);
        _assessmentRepositoryMock.Setup(r => r.GetByDirectReportAndSkillAsync(_testDirectReportId, _testSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssessment);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.BulkAssessAsync(dto);

        // Assert
        result.Should().HaveCount(1);
        _assessmentRepositoryMock.Verify(r => r.UpdateAsync(existingAssessment, It.IsAny<CancellationToken>()), Times.Once);
        _assessmentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SkillAssessment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkAssessAsync_WithNonExistentDirectReport_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new BulkSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            Assessments = new List<SkillLevelDto>()
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.BulkAssessAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task BulkAssessAsync_WithNonExistentSkill_ThrowsNotFoundException()
    {
        // Arrange
        var invalidSkillId = Guid.NewGuid();
        var dto = new BulkSkillAssessmentDto
        {
            DirectReportId = _testDirectReportId,
            Assessments = new List<SkillLevelDto>
            {
                new SkillLevelDto { SkillId = invalidSkillId, Level = ProficiencyLevel.Intermediate }
            }
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _skillRepositoryMock.Setup(r => r.GetByIdAsync(invalidSkillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = () => _service.BulkAssessAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesAssessment()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        _assessmentRepositoryMock.Setup(r => r.ExistsAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(assessmentId);

        // Assert
        _assessmentRepositoryMock.Verify(r => r.DeleteAsync(assessmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        _assessmentRepositoryMock.Setup(r => r.ExistsAsync(assessmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(assessmentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region GetSkillMatrixAsync Tests

    [Fact]
    public async Task GetSkillMatrixAsync_ReturnsMatrix()
    {
        // Arrange
        var assessment = CreateAssessment();
        _skillRepositoryMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment> { assessment });

        // Act
        var result = await _service.GetSkillMatrixAsync();

        // Assert
        result.Should().NotBeNull();
        result.Skills.Should().HaveCount(1);
        result.DirectReports.Should().HaveCount(1);
        result.DirectReports[0].Assessments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSkillMatrixAsync_WithNoData_ReturnsEmptyMatrix()
    {
        // Arrange
        _skillRepositoryMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());
        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment>());

        // Act
        var result = await _service.GetSkillMatrixAsync();

        // Assert
        result.Should().NotBeNull();
        result.Skills.Should().BeEmpty();
        result.DirectReports.Should().BeEmpty();
    }

    #endregion

    #region GetSkillGapsAsync Tests

    [Fact]
    public async Task GetSkillGapsAsync_ReturnsOnlyGaps()
    {
        // Arrange
        var gapAssessment = CreateAssessment(ProficiencyLevel.Beginner, ProficiencyLevel.Advanced);
        var meetsTargetAssessment = CreateAssessment(ProficiencyLevel.Advanced, ProficiencyLevel.Intermediate);

        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment> { gapAssessment, meetsTargetAssessment });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.GetSkillGapsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].SkillGap.Should().BeGreaterThan(0);
        result[0].MeetsTarget.Should().BeFalse();
    }

    [Fact]
    public async Task GetSkillGapsAsync_WhenAllMeetTarget_ReturnsEmpty()
    {
        // Arrange
        var meetsTargetAssessment = CreateAssessment(ProficiencyLevel.Advanced, ProficiencyLevel.Intermediate);

        _assessmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessment> { meetsTargetAssessment });
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _skillRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill> { _testSkill });

        // Act
        var result = await _service.GetSkillGapsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private DirectReport CreateDirectReport()
    {
        var dr = new DirectReport("John", "Doe", "john.doe@test.com", "Engineer", "Engineering", DateTime.UtcNow);
        SetPropertyValue(dr, "Id", _testDirectReportId);
        return dr;
    }

    private Skill CreateSkill()
    {
        var skill = new Skill("C#", "Programming language", SkillCategory.Technical);
        SetPropertyValue(skill, "Id", _testSkillId);
        return skill;
    }

    private SkillAssessment CreateAssessment(
        ProficiencyLevel level = ProficiencyLevel.Intermediate,
        ProficiencyLevel? targetLevel = ProficiencyLevel.Advanced)
    {
        return new SkillAssessment(_testDirectReportId, _testSkillId, level, targetLevel, "Test notes");
    }

    private static void SetPropertyValue(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property!.SetValue(obj, value);
    }

    #endregion
}
