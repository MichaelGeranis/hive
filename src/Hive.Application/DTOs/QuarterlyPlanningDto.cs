using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data transfer object for Quarter entity.
/// </summary>
public record QuarterDto
{
    public Guid Id { get; init; }
    public int Year { get; init; }
    public int QuarterNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public QuarterStatus Status { get; init; }
    public string OkrReference { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating a Quarter.
/// </summary>
public record CreateQuarterDto
{
    public int Year { get; init; }
    public int QuarterNumber { get; init; }
    public string? OkrReference { get; init; }
}

/// <summary>
/// Data transfer object for updating a Quarter.
/// </summary>
public record UpdateQuarterDto
{
    public string? OkrReference { get; init; }
}

/// <summary>
/// Data transfer object for Initiative entity.
/// </summary>
public record InitiativeDto
{
    public Guid Id { get; init; }
    public Guid QuarterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string TshirtSize { get; init; } = "M";
    public string Url { get; init; } = string.Empty;
    public int WorkType { get; init; } = 1;
    public Guid? StartSprintId { get; init; }
    public int SprintSpan { get; init; } = 1;
    public IReadOnlyList<InitiativeMemberDto> Members { get; init; } = new List<InitiativeMemberDto>();
    public int AllocationCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating an Initiative.
/// </summary>
public record CreateInitiativeDto
{
    public Guid QuarterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ProjectId { get; init; }
    public string? TshirtSize { get; init; }
    public string? Url { get; init; }
    public int? WorkType { get; init; }
    public Guid? StartSprintId { get; init; }
}

/// <summary>
/// Data transfer object for updating an Initiative.
/// </summary>
public record UpdateInitiativeDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Color { get; init; }
    public Guid? ProjectId { get; init; }
    public string? TshirtSize { get; init; }
    public string? Url { get; init; }
    public int? WorkType { get; init; }
    public Guid? StartSprintId { get; init; }
}

/// <summary>
/// Data transfer object for InitiativeMember entity.
/// </summary>
public record InitiativeMemberDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for adding a member to an initiative.
/// </summary>
public record CreateInitiativeMemberDto
{
    public Guid DirectReportId { get; init; }
}

/// <summary>
/// Data transfer object for assigning an initiative to a sprint.
/// </summary>
public record AssignSprintDto
{
    public Guid? StartSprintId { get; init; }
}

/// <summary>
/// Data transfer object for Allocation entity.
/// </summary>
public record AllocationDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public string InitiativeName { get; init; } = string.Empty;
    public string InitiativeColor { get; init; } = string.Empty;
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating an Allocation.
/// </summary>
public record CreateAllocationDto
{
    public Guid InitiativeId { get; init; }
    public Guid DirectReportId { get; init; }
    public Guid SprintId { get; init; }
}

/// <summary>
/// Data transfer object for SprintGoal entity.
/// </summary>
public record SprintGoalDto
{
    public Guid Id { get; init; }
    public Guid QuarterId { get; init; }
    public Guid SprintId { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating or updating a SprintGoal.
/// </summary>
public record UpsertSprintGoalDto
{
    public Guid QuarterId { get; init; }
    public Guid SprintId { get; init; }
    public string? Goal { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Data transfer object for InitiativeDependency entity.
/// </summary>
public record InitiativeDependencyDto
{
    public Guid Id { get; init; }
    public Guid DependentInitiativeId { get; init; }
    public string DependentInitiativeName { get; init; } = string.Empty;
    public Guid DependencyInitiativeId { get; init; }
    public string DependencyInitiativeName { get; init; } = string.Empty;
    public DependencyType Type { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for creating an InitiativeDependency.
/// </summary>
public record CreateInitiativeDependencyDto
{
    public Guid DependentInitiativeId { get; init; }
    public Guid DependencyInitiativeId { get; init; }
    public DependencyType Type { get; init; } = DependencyType.FinishToStart;
    public string? Notes { get; init; }
}

/// <summary>
/// Data transfer object for the full planning board data.
/// </summary>
public record PlanningBoardDto
{
    public QuarterDto Quarter { get; init; } = null!;
    public IReadOnlyList<InitiativeDto> Initiatives { get; init; } = new List<InitiativeDto>();
    public IReadOnlyList<SprintDto> Sprints { get; init; } = new List<SprintDto>();
    public IReadOnlyList<DirectReportDto> TeamMembers { get; init; } = new List<DirectReportDto>();
    public IReadOnlyList<AllocationDto> Allocations { get; init; } = new List<AllocationDto>();
    public IReadOnlyList<SprintGoalDto> SprintGoals { get; init; } = new List<SprintGoalDto>();
    public IReadOnlyList<InitiativeDependencyDto> Dependencies { get; init; } = new List<InitiativeDependencyDto>();
    public IReadOnlyList<InitiativeMemberDto> InitiativeMembers { get; init; } = new List<InitiativeMemberDto>();
    public IReadOnlyList<LeaveDto> Leaves { get; init; } = new List<LeaveDto>();
}

/// <summary>
/// Summary information for a team member in the planning board.
/// </summary>
public record TeamMemberSummaryDto
{
    public Guid DirectReportId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int InitiativeCount { get; init; }
    public IReadOnlyList<SprintAllocationSummary> SprintAllocations { get; init; } = new List<SprintAllocationSummary>();
}

/// <summary>
/// Summary of allocations for a specific sprint.
/// </summary>
public record SprintAllocationSummary
{
    public Guid SprintId { get; init; }
    public int AllocationCount { get; init; }
    public bool HasLeave { get; init; }
    public int LeaveDays { get; init; }
}
