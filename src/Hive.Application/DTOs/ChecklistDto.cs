using Hive.Core.Entities;

namespace Hive.Application.DTOs;

// ============== Template DTOs ==============

public record ChecklistTemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ChecklistType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record ChecklistTemplateWithItemsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ChecklistType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<ChecklistTemplateItemDto> Items { get; init; } = [];
}

public record CreateChecklistTemplateDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ChecklistType Type { get; init; }
}

public record UpdateChecklistTemplateDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

// ============== Template Item DTOs ==============

public record ChecklistTemplateItemDto
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; init; }
    public int SortOrder { get; init; }
    public string Content { get; init; } = string.Empty;
    public ChecklistItemType ItemType { get; init; }
    public string ItemTypeName { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? HelpText { get; init; }
    public int? EstimatedMinutes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateChecklistTemplateItemDto
{
    public string Content { get; init; } = string.Empty;
    public ChecklistItemType ItemType { get; init; }
    public bool IsRequired { get; init; } = true;
    public string? HelpText { get; init; }
    public int? EstimatedMinutes { get; init; }
}

public record UpdateChecklistTemplateItemDto
{
    public int SortOrder { get; init; }
    public string Content { get; init; } = string.Empty;
    public ChecklistItemType ItemType { get; init; }
    public bool IsRequired { get; init; }
    public string? HelpText { get; init; }
    public int? EstimatedMinutes { get; init; }
}

// ============== Instance DTOs ==============

public record ChecklistInstanceDto
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public ChecklistType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public ChecklistInstanceStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    // Interview fields
    public string? CandidateName { get; init; }
    public string? Position { get; init; }
    public DateTime? InterviewDate { get; init; }

    // Onboarding fields
    public string? NewHireName { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? TargetCompletionDate { get; init; }

    public string Notes { get; init; } = string.Empty;
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record ChecklistInstanceWithItemsDto
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public ChecklistType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public ChecklistInstanceStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    // Interview fields
    public string? CandidateName { get; init; }
    public string? Position { get; init; }
    public DateTime? InterviewDate { get; init; }

    // Onboarding fields
    public string? NewHireName { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? TargetCompletionDate { get; init; }

    public string Notes { get; init; } = string.Empty;
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<ChecklistInstanceItemDto> Items { get; init; } = [];
}

public record CreateInterviewInstanceDto
{
    public Guid TemplateId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CandidateName { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public DateTime InterviewDate { get; init; }
}

public record CreateOnboardingInstanceDto
{
    public Guid TemplateId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string NewHireName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? TargetCompletionDate { get; init; }
}

// ============== Instance Item DTOs ==============

public record ChecklistInstanceItemDto
{
    public Guid Id { get; init; }
    public Guid InstanceId { get; init; }
    public Guid TemplateItemId { get; init; }
    public int SortOrder { get; init; }
    public string Content { get; init; } = string.Empty;
    public ChecklistItemType ItemType { get; init; }
    public string ItemTypeName { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public ChecklistItemStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public int? Score { get; init; }
    public string? Assignee { get; init; }
    public DateTime? DueDate { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CompleteChecklistItemDto
{
    public string? Notes { get; init; }
    public int? Score { get; init; }
}

public record SkipChecklistItemDto
{
    public string? Notes { get; init; }
}

public record UpdateChecklistItemDto
{
    public string? Notes { get; init; }
    public int? Score { get; init; }
    public string? Assignee { get; init; }
    public DateTime? DueDate { get; init; }
}

public record ReorderItemsDto
{
    public IReadOnlyList<Guid> ItemIds { get; init; } = [];
}
