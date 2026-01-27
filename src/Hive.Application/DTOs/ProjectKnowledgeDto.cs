namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for ProjectKnowledge.
/// </summary>
public record ProjectKnowledgeDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int KnowledgeLevel { get; init; }
    public string KnowledgeLevelLabel { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating or updating a ProjectKnowledge assessment.
/// </summary>
public record CreateOrUpdateProjectKnowledgeDto
{
    public Guid DirectReportId { get; init; }
    public Guid ProjectId { get; init; }
    public int KnowledgeLevel { get; init; }
}

/// <summary>
/// DTO for the project knowledge matrix view - shows all projects and team members' knowledge levels.
/// </summary>
public record ProjectKnowledgeMatrixDto
{
    public IList<KnowledgeMatrixProjectDto> Projects { get; init; } = new List<KnowledgeMatrixProjectDto>();
    public IList<KnowledgeMatrixMemberDto> DirectReports { get; init; } = new List<KnowledgeMatrixMemberDto>();
    public IList<ProjectKnowledgeDto> Scores { get; init; } = new List<ProjectKnowledgeDto>();
}

/// <summary>
/// Simplified project summary for knowledge matrix display.
/// </summary>
public record KnowledgeMatrixProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Simplified direct report summary for knowledge matrix display.
/// </summary>
public record KnowledgeMatrixMemberDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Represents a single knowledge progression entry derived from activity feed.
/// </summary>
public record KnowledgeProgressionEntryDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int OldLevel { get; init; }
    public int NewLevel { get; init; }
    public int Change { get; init; }
    public DateTime Timestamp { get; init; }
}
