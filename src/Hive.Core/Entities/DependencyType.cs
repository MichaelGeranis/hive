namespace Hive.Core.Entities;

/// <summary>
/// Types of dependencies between initiatives.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// The dependent initiative cannot start until the dependency finishes.
    /// </summary>
    FinishToStart = 0,

    /// <summary>
    /// The dependent initiative cannot start until the dependency starts.
    /// </summary>
    StartToStart = 1,

    /// <summary>
    /// The dependent initiative cannot finish until the dependency finishes.
    /// </summary>
    FinishToFinish = 2
}
