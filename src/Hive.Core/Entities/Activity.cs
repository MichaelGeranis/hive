namespace Hive.Core.Entities;

/// <summary>
/// Represents an immutable activity log entry tracking changes across the system.
/// Activities are append-only and never modified after creation.
/// </summary>
public class Activity
{
    /// <summary>
    /// Gets the unique identifier for the activity.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type of activity that occurred.
    /// </summary>
    public ActivityType ActivityType { get; private set; }

    /// <summary>
    /// Gets the type of entity that the activity is related to.
    /// </summary>
    public EntityType EntityType { get; private set; }

    /// <summary>
    /// Gets the ID of the entity that was affected.
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// Gets the name or description of the entity for display purposes.
    /// </summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the human-readable description of the activity.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the activity occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Gets the timestamp when the activity was logged.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for Entity Framework.
    /// </summary>
    private Activity() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Activity"/> class.
    /// </summary>
    /// <param name="activityType">The type of activity.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The ID of the affected entity.</param>
    /// <param name="entityName">The name of the entity for display.</param>
    /// <param name="description">The human-readable description of the activity.</param>
    /// <param name="timestamp">Optional timestamp when the activity occurred. Defaults to current UTC time.</param>
    /// <exception cref="ArgumentException">Thrown when entityId is empty, or when entityName or description are null or whitespace.</exception>
    public Activity(
        ActivityType activityType,
        EntityType entityType,
        Guid entityId,
        string entityName,
        string description,
        DateTime? timestamp = null)
    {
        ValidateEntityId(entityId);
        ValidateEntityName(entityName);
        ValidateDescription(description);

        Id = Guid.NewGuid();
        ActivityType = activityType;
        EntityType = entityType;
        EntityId = entityId;
        EntityName = entityName.Trim();
        Description = description.Trim();
        Timestamp = timestamp ?? DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    private static void ValidateEntityId(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
        }
    }

    private static void ValidateEntityName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
        }

        if (entityName.Trim().Length > 500)
        {
            throw new ArgumentException("Entity name cannot exceed 500 characters.", nameof(entityName));
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be null or whitespace.", nameof(description));
        }

        if (description.Trim().Length > 1000)
        {
            throw new ArgumentException("Description cannot exceed 1000 characters.", nameof(description));
        }
    }
}
