namespace Hive.Core.Entities;

/// <summary>
/// Represents the type of activity that occurred in the system.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// A new entity was created.
    /// </summary>
    Created = 0,

    /// <summary>
    /// An existing entity was updated.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// An entity's status changed.
    /// </summary>
    StatusChanged = 2,

    /// <summary>
    /// An entity was approved.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// An entity was rejected.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// An entity was completed.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// An entity was deleted.
    /// </summary>
    Deleted = 6,

    /// <summary>
    /// An entity was cancelled.
    /// </summary>
    Cancelled = 7
}

/// <summary>
/// Represents the type of entity that the activity is related to.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// Performance review entity.
    /// </summary>
    Review = 0,

    /// <summary>
    /// Task entity.
    /// </summary>
    Task = 1,

    /// <summary>
    /// Leave entity.
    /// </summary>
    Leave = 2,

    /// <summary>
    /// Direct report (team member) entity.
    /// </summary>
    DirectReport = 3,

    /// <summary>
    /// One-on-one meeting entity.
    /// </summary>
    Meeting = 4,

    /// <summary>
    /// Meeting note entity.
    /// </summary>
    MeetingNote = 5,

    /// <summary>
    /// Manager note/TODO entity.
    /// </summary>
    ManagerNote = 6,

    /// <summary>
    /// Project entity.
    /// </summary>
    Project = 7,

    /// <summary>
    /// Sprint entity.
    /// </summary>
    Sprint = 8,

    /// <summary>
    /// Sprint capacity entity.
    /// </summary>
    SprintCapacity = 9,

    /// <summary>
    /// Document entity.
    /// </summary>
    Document = 10,

    /// <summary>
    /// Skill entity.
    /// </summary>
    Skill = 11,

    /// <summary>
    /// Skill assessment entity.
    /// </summary>
    SkillAssessment = 12,

    /// <summary>
    /// Checklist template entity.
    /// </summary>
    ChecklistTemplate = 13,

    /// <summary>
    /// Checklist instance entity.
    /// </summary>
    ChecklistInstance = 14,

    /// <summary>
    /// Parent (reporting hierarchy) entity.
    /// </summary>
    Parent = 15
}
