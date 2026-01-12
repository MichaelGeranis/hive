using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for SkillAssessment.
/// </summary>
public record SkillAssessmentDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public SkillCategory SkillCategory { get; init; }
    public ProficiencyLevel Level { get; init; }
    public string LevelName { get; init; } = string.Empty;
    public ProficiencyLevel? TargetLevel { get; init; }
    public string? TargetLevelName { get; init; }
    public int? SkillGap { get; init; }
    public bool MeetsTarget { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new SkillAssessment.
/// </summary>
public record CreateSkillAssessmentDto
{
    public Guid DirectReportId { get; init; }
    public Guid SkillId { get; init; }
    public ProficiencyLevel Level { get; init; }
    public ProficiencyLevel? TargetLevel { get; init; }
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating a SkillAssessment.
/// </summary>
public record UpdateSkillAssessmentDto
{
    public ProficiencyLevel Level { get; init; }
    public ProficiencyLevel? TargetLevel { get; init; }
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// DTO for bulk assessing skills for a direct report.
/// </summary>
public record BulkSkillAssessmentDto
{
    public Guid DirectReportId { get; init; }
    public IList<SkillLevelDto> Assessments { get; init; } = new List<SkillLevelDto>();
}

/// <summary>
/// Simple skill level pair for bulk operations.
/// </summary>
public record SkillLevelDto
{
    public Guid SkillId { get; init; }
    public ProficiencyLevel Level { get; init; }
    public ProficiencyLevel? TargetLevel { get; init; }
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// DTO for the skill matrix view - shows all direct reports and their skill levels.
/// </summary>
public record SkillMatrixDto
{
    public IList<SkillDto> Skills { get; init; } = new List<SkillDto>();
    public IList<DirectReportSkillsDto> DirectReports { get; init; } = new List<DirectReportSkillsDto>();
}

/// <summary>
/// A direct report with all their skill assessments.
/// </summary>
public record DirectReportSkillsDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public IList<SkillAssessmentDto> Assessments { get; init; } = new List<SkillAssessmentDto>();
}
