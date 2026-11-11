namespace Hive.Core.Entities;

/// <summary>
/// What an initiative is on the plan: a candidate option from the PM, work left over from the
/// previous quarter, or a standing lane (Maintenance, Tech roadmap) that exists every quarter.
/// </summary>
public enum InitiativeKind
{
    Option = 0,
    Leftover = 1,
    Bucket = 2
}

/// <summary>
/// Where an initiative is in the quarter's planning: in discovery, in solution design, ready for
/// commitment, then decided (committed, stretch, dropped), and at the end delivered.
/// </summary>
public enum InitiativeStatus
{
    InDiscovery = 0,
    InSolutionDesign = 1,
    ReadyForCommitment = 2,
    Committed = 3,
    Stretch = 4,
    Dropped = 5,
    Delivered = 6
}

/// <summary>
/// What still stands between an option and being ready to start. Several can be open at once.
/// </summary>
[Flags]
public enum ReadinessIssue
{
    None = 0,
    RequirementsPending = 1,
    EstimationPending = 2,
    ReleaseCoordination = 4,
    SolutionDesignPending = 8
}

/// <summary>
/// A person's part on an initiative: the one who leads it, or someone who contributes.
/// </summary>
public enum InitiativeRole
{
    Contributor = 0,
    Lead = 1
}

/// <summary>
/// Where a dependency on another team stands.
/// </summary>
public enum ExternalDependencyStatus
{
    NeedsCoordination = 0,
    Agreed = 1,
    AtRisk = 2,
    Done = 3
}
