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
    Completed = 5
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
    Leave = 2
}
