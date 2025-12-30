using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

public class SkillAssessmentsControllerTests
{
    private readonly Mock<ISkillAssessmentService> _serviceMock;
    private readonly Mock<ILogger<SkillAssessmentsController>> _loggerMock;
    private readonly SkillAssessmentsController _controller;

    public SkillAssessmentsControllerTests()
    {
        _serviceMock = new Mock<ISkillAssessmentService>();
        _loggerMock = new Mock<ILogger<SkillAssessmentsController>>();
        _controller = new SkillAssessmentsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllAssessments()
    {
        // Arrange
        var assessments = new List<SkillAssessmentDto>
        {
            CreateDto(),
            CreateDto()
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssessments = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedAssessments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessmentDto>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssessments = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedAssessments.Should().BeEmpty();
    }

    #endregion

    #region GetMatrix Tests

    [Fact]
    public async Task GetMatrix_ReturnsOkWithMatrix()
    {
        // Arrange
        var matrix = new SkillMatrixDto
        {
            Skills = new List<SkillDto>
            {
                new SkillDto { Id = Guid.NewGuid(), Name = "C#", Category = SkillCategory.Technical }
            },
            DirectReports = new List<DirectReportSkillsDto>
            {
                new DirectReportSkillsDto
                {
                    DirectReportId = Guid.NewGuid(),
                    DirectReportName = "John Doe",
                    Assessments = new List<SkillAssessmentDto> { CreateDto() }
                }
            }
        };
        _serviceMock.Setup(s => s.GetSkillMatrixAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(matrix);

        // Act
        var result = await _controller.GetMatrix(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMatrix = okResult.Value.Should().BeOfType<SkillMatrixDto>().Subject;
        returnedMatrix.Skills.Should().HaveCount(1);
        returnedMatrix.DirectReports.Should().HaveCount(1);
    }

    #endregion

    #region GetGaps Tests

    [Fact]
    public async Task GetGaps_ReturnsOkWithGaps()
    {
        // Arrange
        var gaps = new List<SkillAssessmentDto>
        {
            CreateDto(level: ProficiencyLevel.Beginner, targetLevel: ProficiencyLevel.Advanced, skillGap: 2)
        };
        _serviceMock.Setup(s => s.GetSkillGapsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gaps);

        // Act
        var result = await _controller.GetGaps(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGaps = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedGaps.Should().HaveCount(1);
        returnedGaps.First().SkillGap.Should().Be(2);
    }

    [Fact]
    public async Task GetGaps_WhenNoGaps_ReturnsEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetSkillGapsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillAssessmentDto>());

        // Act
        var result = await _controller.GetGaps(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGaps = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedGaps.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithAssessment()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<SkillAssessmentDto>().Subject;
        returnedDto.Id.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillAssessmentDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByDirectReport Tests

    [Fact]
    public async Task GetByDirectReport_ReturnsOkWithFilteredAssessments()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var assessments = new List<SkillAssessmentDto>
        {
            CreateDto(directReportId: directReportId),
            CreateDto(directReportId: directReportId)
        };
        _serviceMock.Setup(s => s.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);

        // Act
        var result = await _controller.GetByDirectReport(directReportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssessments = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedAssessments.Should().HaveCount(2);
        returnedAssessments.Should().AllSatisfy(a => a.DirectReportId.Should().Be(directReportId));
    }

    #endregion

    #region GetBySkill Tests

    [Fact]
    public async Task GetBySkill_ReturnsOkWithFilteredAssessments()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var assessments = new List<SkillAssessmentDto>
        {
            CreateDto(skillId: skillId),
            CreateDto(skillId: skillId)
        };
        _serviceMock.Setup(s => s.GetBySkillIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);

        // Act
        var result = await _controller.GetBySkill(skillId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssessments = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedAssessments.Should().HaveCount(2);
        returnedAssessments.Should().AllSatisfy(a => a.SkillId.Should().Be(skillId));
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate,
            TargetLevel = ProficiencyLevel.Advanced,
            Notes = "Test notes"
        };
        var resultDto = CreateDto();
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<SkillAssessmentDto>().Subject;
        returnedDto.Id.Should().Be(resultDto.Id);
    }

    [Fact]
    public async Task Create_WithNonExistentDirectReport_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(DirectReport), createDto.DirectReportId));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithNonExistentSkill_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Skill), createDto.SkillId));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithDuplicateAssessment_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("An assessment for this skill and direct report already exists."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_WithInactiveSkill_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot assess an inactive skill."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSkillAssessmentDto
        {
            DirectReportId = Guid.Empty,
            SkillId = Guid.NewGuid(),
            Level = ProficiencyLevel.Intermediate
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("DirectReportId cannot be empty."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkWithUpdatedAssessment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateSkillAssessmentDto
        {
            Level = ProficiencyLevel.Advanced,
            TargetLevel = ProficiencyLevel.Expert,
            Notes = "Updated notes"
        };
        var resultDto = CreateDto(level: ProficiencyLevel.Advanced, targetLevel: ProficiencyLevel.Expert);
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<SkillAssessmentDto>().Subject;
        returnedDto.Level.Should().Be(ProficiencyLevel.Advanced);
        returnedDto.TargetLevel.Should().Be(ProficiencyLevel.Expert);
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateSkillAssessmentDto
        {
            Level = ProficiencyLevel.Advanced
        };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(SkillAssessment), id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateSkillAssessmentDto
        {
            Level = ProficiencyLevel.None
        };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Proficiency level must be set."));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region BulkAssess Tests

    [Fact]
    public async Task BulkAssess_WithValidDto_ReturnsOkWithResults()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var bulkDto = new BulkSkillAssessmentDto
        {
            DirectReportId = directReportId,
            Assessments = new List<SkillLevelDto>
            {
                new SkillLevelDto { SkillId = Guid.NewGuid(), Level = ProficiencyLevel.Intermediate, TargetLevel = ProficiencyLevel.Advanced },
                new SkillLevelDto { SkillId = Guid.NewGuid(), Level = ProficiencyLevel.Beginner, TargetLevel = ProficiencyLevel.Intermediate }
            }
        };
        var resultDtos = new List<SkillAssessmentDto>
        {
            CreateDto(directReportId: directReportId),
            CreateDto(directReportId: directReportId)
        };
        _serviceMock.Setup(s => s.BulkAssessAsync(bulkDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDtos);

        // Act
        var result = await _controller.BulkAssess(bulkDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssessments = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillAssessmentDto>>().Subject;
        returnedAssessments.Should().HaveCount(2);
    }

    [Fact]
    public async Task BulkAssess_WithNonExistentDirectReport_ReturnsNotFound()
    {
        // Arrange
        var bulkDto = new BulkSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            Assessments = new List<SkillLevelDto>()
        };
        _serviceMock.Setup(s => s.BulkAssessAsync(bulkDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(DirectReport), bulkDto.DirectReportId));

        // Act
        var result = await _controller.BulkAssess(bulkDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BulkAssess_WithNonExistentSkill_ReturnsNotFound()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var bulkDto = new BulkSkillAssessmentDto
        {
            DirectReportId = Guid.NewGuid(),
            Assessments = new List<SkillLevelDto>
            {
                new SkillLevelDto { SkillId = skillId, Level = ProficiencyLevel.Intermediate }
            }
        };
        _serviceMock.Setup(s => s.BulkAssessAsync(bulkDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Skill), skillId));

        // Act
        var result = await _controller.BulkAssess(bulkDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task BulkAssess_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var bulkDto = new BulkSkillAssessmentDto
        {
            DirectReportId = Guid.Empty,
            Assessments = new List<SkillLevelDto>()
        };
        _serviceMock.Setup(s => s.BulkAssessAsync(bulkDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("DirectReportId cannot be empty."));

        // Act
        var result = await _controller.BulkAssess(bulkDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(SkillAssessment), id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static SkillAssessmentDto CreateDto(
        Guid? id = null,
        Guid? directReportId = null,
        Guid? skillId = null,
        ProficiencyLevel level = ProficiencyLevel.Intermediate,
        ProficiencyLevel? targetLevel = ProficiencyLevel.Advanced,
        int? skillGap = null)
    {
        var gap = skillGap ?? (targetLevel.HasValue ? (int)targetLevel.Value - (int)level : (int?)null);
        return new SkillAssessmentDto
        {
            Id = id ?? Guid.NewGuid(),
            DirectReportId = directReportId ?? Guid.NewGuid(),
            DirectReportName = "John Doe",
            SkillId = skillId ?? Guid.NewGuid(),
            SkillName = "C#",
            SkillCategory = SkillCategory.Technical,
            Level = level,
            LevelName = level.ToString(),
            TargetLevel = targetLevel,
            TargetLevelName = targetLevel?.ToString(),
            SkillGap = gap,
            MeetsTarget = gap.HasValue ? gap.Value <= 0 : true,
            Notes = "Test notes",
            AssessedAt = DateTime.UtcNow
        };
    }

    #endregion
}
