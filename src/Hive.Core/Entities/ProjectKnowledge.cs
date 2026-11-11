namespace Hive.Core.Entities;

/// <summary>
/// Represents a team member's knowledge level for a specific project.
/// Used to track knowledge gaps across the team.
/// </summary>
public class ProjectKnowledge
{
    /// <summary>
    /// Unique identifier for the knowledge assessment.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The team member being assessed.
    /// </summary>
    public Guid DirectReportId { get; private set; }

    /// <summary>
    /// The project being assessed.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Knowledge level from 1-5:
    /// 1 - No clue
    /// 2 - Limited Knowledge
    /// 3 - Moderate Knowledge
    /// 4 - Good Knowledge
    /// 5 - Confident
    /// </summary>
    public int KnowledgeLevel { get; private set; }

    /// <summary>
    /// When the assessment was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private ProjectKnowledge() { }

    /// <summary>
    /// Creates a new project knowledge assessment.
    /// </summary>
    public ProjectKnowledge(Guid directReportId, Guid projectId, int knowledgeLevel)
    {
        ValidateDirectReportId(directReportId);
        ValidateProjectId(projectId);
        ValidateKnowledgeLevel(knowledgeLevel);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        ProjectId = projectId;
        KnowledgeLevel = knowledgeLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the knowledge assessment.
    /// </summary>
    public void Update(int knowledgeLevel)
    {
        ValidateKnowledgeLevel(knowledgeLevel);

        KnowledgeLevel = knowledgeLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the label for the current knowledge level.
    /// </summary>
    public string GetKnowledgeLevelLabel()
    {
        return KnowledgeLevel switch
        {
            1 => "No clue",
            2 => "Limited Knowledge",
            3 => "Moderate Knowledge",
            4 => "Good Knowledge",
            5 => "Confident",
            _ => "Unknown"
        };
    }

    private static void ValidateDirectReportId(Guid directReportId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }
    }

    private static void ValidateProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
        }
    }

    private static void ValidateKnowledgeLevel(int knowledgeLevel)
    {
        if (knowledgeLevel < 1 || knowledgeLevel > 5)
        {
            throw new ArgumentException("Knowledge level must be between 1 and 5.", nameof(knowledgeLevel));
        }
    }
}
