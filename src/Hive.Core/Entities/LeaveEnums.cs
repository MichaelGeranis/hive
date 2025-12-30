namespace Hive.Core.Entities;

/// <summary>
/// Types of leave that can be requested
/// </summary>
public enum LeaveType
{
    PTO = 0,           // Paid Time Off
    Vacation = 1,
    SickLeave = 2,
    PersonalLeave = 3,
    FamilyLeave = 4,
    BereavementLeave = 5,
    JuryDuty = 6,
    PublicHoliday = 7,
    Unpaid = 8,
    Other = 9
}

/// <summary>
/// Status of a leave request
/// </summary>
public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}
