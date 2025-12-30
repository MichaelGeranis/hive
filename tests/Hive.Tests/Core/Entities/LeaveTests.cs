using Hive.Core.Entities;
using FluentAssertions;

namespace Hive.Tests.Core.Entities;

public class LeaveTests
{
    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly DateTime _testStartDate = new DateTime(2024, 1, 15);
    private readonly DateTime _testEndDate = new DateTime(2024, 1, 19);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidData_CreatesLeave()
    {
        // Arrange & Act
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            _testStartDate,
            _testEndDate,
            "Family vacation",
            "Will be in Europe");

        // Assert
        leave.Id.Should().NotBeEmpty();
        leave.DirectReportId.Should().Be(_testDirectReportId);
        leave.Type.Should().Be(LeaveType.Vacation);
        leave.Status.Should().Be(LeaveStatus.Pending);
        leave.StartDate.Should().Be(_testStartDate.Date);
        leave.EndDate.Should().Be(_testEndDate.Date);
        leave.Reason.Should().Be("Family vacation");
        leave.Notes.Should().Be("Will be in Europe");
        leave.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        leave.UpdatedAt.Should().BeNull();
        leave.ApprovedAt.Should().BeNull();
        leave.ApprovedBy.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithOptionalParametersNull_CreatesLeave()
    {
        // Arrange & Act
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.SickLeave,
            _testStartDate,
            _testEndDate);

        // Assert
        leave.Reason.Should().BeNull();
        leave.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromReasonAndNotes()
    {
        // Arrange & Act
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            _testStartDate,
            _testEndDate,
            "  Vacation  ",
            "  Some notes  ");

        // Assert
        leave.Reason.Should().Be("Vacation");
        leave.Notes.Should().Be("Some notes");
    }

    [Fact]
    public void Constructor_NormalizesDateToDateOnly()
    {
        // Arrange
        var startWithTime = new DateTime(2024, 1, 15, 14, 30, 45);
        var endWithTime = new DateTime(2024, 1, 19, 18, 45, 30);

        // Act
        var leave = new Leave(_testDirectReportId, LeaveType.PTO, startWithTime, endWithTime);

        // Assert
        leave.StartDate.Should().Be(new DateTime(2024, 1, 15));
        leave.EndDate.Should().Be(new DateTime(2024, 1, 19));
        leave.StartDate.TimeOfDay.Should().Be(TimeSpan.Zero);
        leave.EndDate.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_WithEmptyDirectReportId_ThrowsArgumentException()
    {
        // Act
        var act = () => new Leave(Guid.Empty, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("directReportId")
            .WithMessage("Direct report ID is required.*");
    }

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 19);
        var endDate = new DateTime(2024, 1, 15);

        // Act
        var act = () => new Leave(_testDirectReportId, LeaveType.Vacation, startDate, endDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("End date cannot be before start date.");
    }

    [Fact]
    public void Constructor_WithDurationExceeding365Days_ThrowsArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2025, 1, 2); // 366 days

        // Act
        var act = () => new Leave(_testDirectReportId, LeaveType.Unpaid, startDate, endDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Leave duration cannot exceed 365 days.");
    }

    [Fact]
    public void Constructor_WithExactly365Days_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31); // 365 days

        // Act
        var act = () => new Leave(_testDirectReportId, LeaveType.Unpaid, startDate, endDate);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region DaysCount Tests

    [Theory]
    [InlineData("2024-01-15", "2024-01-15", 1)]  // Single day
    [InlineData("2024-01-15", "2024-01-16", 2)]  // Two days
    [InlineData("2024-01-15", "2024-01-19", 5)]  // Five days
    [InlineData("2024-01-01", "2024-01-31", 31)] // Full month
    public void DaysCount_ReturnsCorrectInclusiveCount(string start, string end, int expectedDays)
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            DateTime.Parse(start),
            DateTime.Parse(end));

        // Act & Assert
        leave.DaysCount.Should().Be(expectedDays);
    }

    #endregion

    #region BusinessDaysCount Tests

    [Fact]
    public void BusinessDaysCount_ExcludesWeekends()
    {
        // Arrange - Monday to Friday (5 business days)
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15), // Monday
            new DateTime(2024, 1, 19)); // Friday

        // Act & Assert
        leave.BusinessDaysCount.Should().Be(5);
    }

    [Fact]
    public void BusinessDaysCount_IncludingWeekend_CountsOnlyWeekdays()
    {
        // Arrange - Monday to Sunday (5 business days, excludes Sat & Sun)
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15), // Monday
            new DateTime(2024, 1, 21)); // Sunday

        // Act & Assert
        leave.BusinessDaysCount.Should().Be(5);
    }

    [Fact]
    public void BusinessDaysCount_OnlyWeekend_ReturnsZero()
    {
        // Arrange - Saturday to Sunday
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 20), // Saturday
            new DateTime(2024, 1, 21)); // Sunday

        // Act & Assert
        leave.BusinessDaysCount.Should().Be(0);
    }

    [Fact]
    public void BusinessDaysCount_SingleWeekday_ReturnsOne()
    {
        // Arrange - Single Monday
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.SickLeave,
            new DateTime(2024, 1, 15), // Monday
            new DateTime(2024, 1, 15)); // Monday

        // Act & Assert
        leave.BusinessDaysCount.Should().Be(1);
    }

    [Fact]
    public void BusinessDaysCount_SingleWeekendDay_ReturnsZero()
    {
        // Arrange - Single Saturday
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.SickLeave,
            new DateTime(2024, 1, 20), // Saturday
            new DateTime(2024, 1, 20)); // Saturday

        // Act & Assert
        leave.BusinessDaysCount.Should().Be(0);
    }

    #endregion

    #region OverlapsWith Tests

    [Fact]
    public void OverlapsWith_CompletelyBefore_ReturnsFalse()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 1), new DateTime(2024, 1, 14))
            .Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_CompletelyAfter_ReturnsFalse()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 20), new DateTime(2024, 1, 25))
            .Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_PartialOverlapAtStart_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 10), new DateTime(2024, 1, 16))
            .Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_PartialOverlapAtEnd_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 18), new DateTime(2024, 1, 25))
            .Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_CompletelyContained_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 16), new DateTime(2024, 1, 18))
            .Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_CompletelyContains_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 10), new DateTime(2024, 1, 25))
            .Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.OverlapsWith(new DateTime(2024, 1, 15), new DateTime(2024, 1, 19))
            .Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_TouchingAtBoundary_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert - End date touches start date
        leave.OverlapsWith(new DateTime(2024, 1, 10), new DateTime(2024, 1, 15))
            .Should().BeTrue();

        // Act & Assert - Start date touches end date
        leave.OverlapsWith(new DateTime(2024, 1, 19), new DateTime(2024, 1, 25))
            .Should().BeTrue();
    }

    #endregion

    #region IncludesDate Tests

    [Fact]
    public void IncludesDate_DateWithinRange_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.IncludesDate(new DateTime(2024, 1, 17)).Should().BeTrue();
    }

    [Fact]
    public void IncludesDate_StartDate_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.IncludesDate(new DateTime(2024, 1, 15)).Should().BeTrue();
    }

    [Fact]
    public void IncludesDate_EndDate_ReturnsTrue()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.IncludesDate(new DateTime(2024, 1, 19)).Should().BeTrue();
    }

    [Fact]
    public void IncludesDate_BeforeRange_ReturnsFalse()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.IncludesDate(new DateTime(2024, 1, 14)).Should().BeFalse();
    }

    [Fact]
    public void IncludesDate_AfterRange_ReturnsFalse()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert
        leave.IncludesDate(new DateTime(2024, 1, 20)).Should().BeFalse();
    }

    [Fact]
    public void IncludesDate_NormalizesToDateOnly()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19));

        // Act & Assert - Time component should be ignored
        leave.IncludesDate(new DateTime(2024, 1, 17, 14, 30, 45)).Should().BeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var leave = new Leave(
            _testDirectReportId,
            LeaveType.Vacation,
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 19),
            "Original reason",
            "Original notes");

        // Act
        leave.Update(
            LeaveType.SickLeave,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 5),
            "Updated reason",
            "Updated notes");

        // Assert
        leave.Type.Should().Be(LeaveType.SickLeave);
        leave.StartDate.Should().Be(new DateTime(2024, 2, 1));
        leave.EndDate.Should().Be(new DateTime(2024, 2, 5));
        leave.Reason.Should().Be("Updated reason");
        leave.Notes.Should().Be("Updated notes");
        leave.UpdatedAt.Should().NotBeNull();
        leave.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_TrimsWhitespace()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        leave.Update(
            LeaveType.SickLeave,
            _testStartDate,
            _testEndDate,
            "  New reason  ",
            "  New notes  ");

        // Assert
        leave.Reason.Should().Be("New reason");
        leave.Notes.Should().Be("New notes");
    }

    [Fact]
    public void Update_OnApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Approve("Manager");

        // Act
        var act = () => leave.Update(
            LeaveType.SickLeave,
            _testStartDate,
            _testEndDate,
            "New reason",
            "New notes");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify an approved leave request.");
    }

    [Fact]
    public void Update_OnPendingLeave_Succeeds()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        var act = () => leave.Update(LeaveType.SickLeave, _testStartDate, _testEndDate, null, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Update_OnRejectedLeave_Succeeds()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Reject();

        // Act
        var act = () => leave.Update(LeaveType.SickLeave, _testStartDate, _testEndDate, null, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Update_OnCancelledLeave_Succeeds()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Cancel();

        // Act
        var act = () => leave.Update(LeaveType.SickLeave, _testStartDate, _testEndDate, null, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Update_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        var act = () => leave.Update(
            LeaveType.Vacation,
            new DateTime(2024, 1, 19),
            new DateTime(2024, 1, 15),
            null,
            null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("End date cannot be before start date.");
    }

    [Fact]
    public void Update_WithDurationExceeding365Days_ThrowsArgumentException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        var act = () => leave.Update(
            LeaveType.Unpaid,
            new DateTime(2024, 1, 1),
            new DateTime(2025, 1, 2),
            null,
            null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Leave duration cannot exceed 365 days.");
    }

    #endregion

    #region Approve Tests

    [Fact]
    public void Approve_OnPendingLeave_ApprovesSuccessfully()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        var approverName = "Jane Manager";

        // Act
        leave.Approve(approverName);

        // Assert
        leave.Status.Should().Be(LeaveStatus.Approved);
        leave.ApprovedBy.Should().Be(approverName);
        leave.ApprovedAt.Should().NotBeNull();
        leave.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        leave.UpdatedAt.Should().NotBeNull();
        leave.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Approve_TrimsApproverName()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        leave.Approve("  Jane Manager  ");

        // Assert
        leave.ApprovedBy.Should().Be("Jane Manager");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_WithEmptyApproverName_ThrowsArgumentException(string? approverName)
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        var act = () => leave.Approve(approverName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("approvedBy")
            .WithMessage("Approver name is required.*");
    }

    [Fact]
    public void Approve_OnAlreadyApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Approve("Manager");

        // Act
        var act = () => leave.Approve("Another Manager");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot approve a leave request with status: Approved");
    }

    [Fact]
    public void Approve_OnRejectedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Reject();

        // Act
        var act = () => leave.Approve("Manager");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot approve a leave request with status: Rejected");
    }

    [Fact]
    public void Approve_OnCancelledLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Cancel();

        // Act
        var act = () => leave.Approve("Manager");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot approve a leave request with status: Cancelled");
    }

    #endregion

    #region Reject Tests

    [Fact]
    public void Reject_OnPendingLeave_RejectsSuccessfully()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        leave.Reject("Insufficient leave balance");

        // Assert
        leave.Status.Should().Be(LeaveStatus.Rejected);
        leave.Notes.Should().Be("Insufficient leave balance");
        leave.UpdatedAt.Should().NotBeNull();
        leave.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Reject_WithoutNotes_Succeeds()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate, null, "Original notes");

        // Act
        leave.Reject();

        // Assert
        leave.Status.Should().Be(LeaveStatus.Rejected);
        leave.Notes.Should().Be("Original notes"); // Should not change
    }

    [Fact]
    public void Reject_TrimsNotes()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        leave.Reject("  Rejection reason  ");

        // Assert
        leave.Notes.Should().Be("Rejection reason");
    }

    [Fact]
    public void Reject_OnApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Approve("Manager");

        // Act
        var act = () => leave.Reject();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reject a leave request with status: Approved");
    }

    [Fact]
    public void Reject_OnAlreadyRejectedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Reject();

        // Act
        var act = () => leave.Reject();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reject a leave request with status: Rejected");
    }

    [Fact]
    public void Reject_OnCancelledLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Cancel();

        // Act
        var act = () => leave.Reject();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reject a leave request with status: Cancelled");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_OnPendingLeave_CancelsSuccessfully()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);

        // Act
        leave.Cancel();

        // Assert
        leave.Status.Should().Be(LeaveStatus.Cancelled);
        leave.UpdatedAt.Should().NotBeNull();
        leave.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancel_OnApprovedLeave_CancelsSuccessfully()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Approve("Manager");

        // Act
        leave.Cancel();

        // Assert
        leave.Status.Should().Be(LeaveStatus.Cancelled);
    }

    [Fact]
    public void Cancel_OnRejectedLeave_CancelsSuccessfully()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Reject();

        // Act
        leave.Cancel();

        // Assert
        leave.Status.Should().Be(LeaveStatus.Cancelled);
    }

    [Fact]
    public void Cancel_OnAlreadyCancelledLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, _testStartDate, _testEndDate);
        leave.Cancel();

        // Act
        var act = () => leave.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Leave request is already cancelled.");
    }

    #endregion
}
