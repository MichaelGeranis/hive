namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for KnowledgePoint.
/// </summary>
public record KnowledgePointDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int ManualPoints { get; init; }
    public int AutomaticPoints { get; init; }
    public int TotalPoints { get; init; }
    public int? CurrentKnowledgeLevel { get; init; }
    public bool SuggestLevelIncrease { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating or updating KnowledgePoint records.
/// </summary>
public record CreateOrUpdateKnowledgePointDto
{
    public Guid DirectReportId { get; init; }
    public Guid ProjectId { get; init; }
    public int ManualPoints { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for incrementally adding points to a KnowledgePoint record.
/// </summary>
public record AddKnowledgePointsDto
{
    public Guid DirectReportId { get; init; }
    public Guid ProjectId { get; init; }
    public int PointsToAdd { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for suggesting knowledge level increase based on accumulated points.
/// </summary>
public record KnowledgeLevelSuggestionDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int TotalPoints { get; init; }
    public int? CurrentLevel { get; init; }
    public int SuggestedLevel { get; init; }
}

/// <summary>
/// Extended matrix DTO that includes knowledge points data.
/// </summary>
public record ProjectKnowledgeMatrixWithPointsDto
{
    public IList<KnowledgeMatrixProjectDto> Projects { get; init; } = new List<KnowledgeMatrixProjectDto>();
    public IList<KnowledgeMatrixMemberDto> DirectReports { get; init; } = new List<KnowledgeMatrixMemberDto>();
    public IList<ProjectKnowledgeDto> Scores { get; init; } = new List<ProjectKnowledgeDto>();
    public IList<KnowledgePointDto> Points { get; init; } = new List<KnowledgePointDto>();
    public IList<KnowledgeLevelSuggestionDto> Suggestions { get; init; } = new List<KnowledgeLevelSuggestionDto>();
}
