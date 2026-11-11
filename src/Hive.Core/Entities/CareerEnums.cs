namespace Hive.Core.Entities;

/// <summary>
/// Whether a skill is a hard (technical) skill or a soft (people and ways of working) skill.
/// </summary>
public enum SkillCategory
{
    Hard = 0,
    Soft = 1
}

/// <summary>
/// Where a direct report stands on one skill of their career plan.
/// </summary>
public enum CareerSkillStatus
{
    /// <summary>
    /// Below Practising, and not being worked on now.
    /// </summary>
    NotYet = 0,

    /// <summary>
    /// Being worked on now, whatever the level.
    /// </summary>
    WorkingOn = 1,

    /// <summary>
    /// Practising or above.
    /// </summary>
    Has = 2
}

/// <summary>
/// How a direct report's skills read against the stage of their current role. An indication for
/// the manager, never a goal.
/// </summary>
public enum LadderReading
{
    /// <summary>
    /// No skill has been rated yet.
    /// </summary>
    NotAssessed = 0,

    /// <summary>
    /// Their skills do not yet meet what their stage expects.
    /// </summary>
    BelowRole = 1,

    /// <summary>
    /// Their skills meet their stage, and not yet the next one.
    /// </summary>
    AtRole = 2,

    /// <summary>
    /// Their skills already meet a later stage.
    /// </summary>
    AboveRole = 3
}
