using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for managing activity logs.
/// </summary>
public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;

    public ActivityService(IActivityRepository activityRepository)
    {
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
    }

    public async Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var activity = await _activityRepository.GetByIdAsync(id, cancellationToken);
        return activity == null ? null : MapToDto(activity);
    }

    public async Task<IReadOnlyList<ActivityDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var activities = await _activityRepository.GetAllAsync(cancellationToken);
        return activities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ActivityDto>> GetRecentAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            throw new ArgumentException("Days must be greater than zero.", nameof(days));
        }

        var activities = await _activityRepository.GetRecentAsync(days, cancellationToken);
        return activities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ActivityDto>> GetByEntityTypeAsync(
        EntityType entityType,
        CancellationToken cancellationToken = default)
    {
        var activities = await _activityRepository.GetByEntityTypeAsync(entityType, cancellationToken);
        return activities.Select(MapToDto).ToList();
    }

    public async Task<ActivityDto> LogActivityAsync(
        ActivityType activityType,
        EntityType entityType,
        Guid entityId,
        string entityName,
        string description,
        CancellationToken cancellationToken = default)
    {
        var activity = new Activity(
            activityType,
            entityType,
            entityId,
            entityName,
            description);

        var created = await _activityRepository.AddAsync(activity, cancellationToken);
        return MapToDto(created);
    }

    private static ActivityDto MapToDto(Activity activity)
    {
        return new ActivityDto
        {
            Id = activity.Id,
            ActivityType = activity.ActivityType.ToString(),
            ActivityTypeName = GetActivityTypeName(activity.ActivityType),
            EntityType = activity.EntityType.ToString(),
            EntityTypeName = GetEntityTypeName(activity.EntityType),
            EntityId = activity.EntityId,
            EntityName = activity.EntityName,
            Description = activity.Description,
            Timestamp = activity.Timestamp,
            CreatedAt = activity.CreatedAt
        };
    }

    private static string GetActivityTypeName(ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.Created => "Created",
            ActivityType.Updated => "Updated",
            ActivityType.StatusChanged => "Status Changed",
            ActivityType.Approved => "Approved",
            ActivityType.Rejected => "Rejected",
            ActivityType.Completed => "Completed",
            ActivityType.Deleted => "Deleted",
            ActivityType.Cancelled => "Cancelled",
            _ => activityType.ToString()
        };
    }

    private static string GetEntityTypeName(EntityType entityType)
    {
        return entityType switch
        {
            EntityType.Review => "Performance Review",
            EntityType.Task => "Task",
            EntityType.Leave => "Leave",
            EntityType.DirectReport => "Team Member",
            EntityType.Meeting => "1:1 Meeting",
            EntityType.MeetingNote => "Meeting Note",
            EntityType.ManagerNote => "Note/TODO",
            EntityType.Project => "Project",
            EntityType.Sprint => "Sprint",
            EntityType.SprintCapacity => "Sprint Capacity",
            EntityType.Document => "Document",
            EntityType.Skill => "Skill",
            EntityType.SkillAssessment => "Skill Assessment",
            EntityType.ChecklistTemplate => "Checklist Template",
            EntityType.ChecklistInstance => "Checklist",
            EntityType.Parent => "Reporting Structure",
            EntityType.ProjectKnowledge => "Knowledge Assessment",
            _ => entityType.ToString()
        };
    }
}
