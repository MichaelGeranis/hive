using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Application.Services;

public class LeaveServiceTests
{
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly LeaveService _service;

    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly DirectReport _testDirectReport;

    public LeaveServiceTests()
    {
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _service = new LeaveService(
            _leaveRepositoryMock.Object,
            _directReportRepositoryMock.Object);

        _testDirectReport = new DirectReport(
            "John",
            "Doe",
            "john.doe@test.com",
            "Software Engineer",
            "Engineering",
            new DateTime(2020, 1, 1));

        // Set up the test direct report ID using reflection
        var idProperty = typeof(DirectReport).GetProperty("Id");
        idProperty!.SetValue(_testDirectReport, _testDirectReportId);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var leave = CreateLeave();
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByIdAsync(leave.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(leave.Id);
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.DirectReportName.Should().Be(_testDirectReport.FullName);
        result.Type.Should().Be(LeaveType.Vacation.ToString());
        result.Status.Should().Be(LeaveStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenDirectReportNotFound_ReturnsUnknownName()
    {
        // Arrange
        var leave = CreateLeave();
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var result = await _service.GetByIdAsync(leave.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportName.Should().Be("Unknown");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var leaves = new List<Leave>
        {
            CreateLeave(),
            CreateLeave(LeaveType.SickLeave)
        };
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Type.Should().Be(LeaveType.Vacation.ToString());
        result[1].Type.Should().Be(LeaveType.SickLeave.ToString());
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByDirectReportIdAsync Tests

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var leaves = new List<Leave>
        {
            CreateLeave(),
            CreateLeave(LeaveType.SickLeave)
        };
        _leaveRepositoryMock.Setup(r => r.GetByDirectReportIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByDirectReportIdAsync(_testDirectReportId);

        // Assert
        result.Should().HaveCount(2);
        result.All(l => l.DirectReportId == _testDirectReportId).Should().BeTrue();
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ReturnsFilteredDtos()
    {
        // Arrange
        var leaves = new List<Leave> { CreateLeave() };
        _leaveRepositoryMock.Setup(r => r.GetByStatusAsync(LeaveStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByStatusAsync("Pending");

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(LeaveStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetByStatusAsync_WithValidStatusCaseInsensitive_ReturnsFilteredDtos()
    {
        // Arrange
        var leaves = new List<Leave> { CreateLeave() };
        _leaveRepositoryMock.Setup(r => r.GetByStatusAsync(LeaveStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByStatusAsync("pending");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.GetByStatusAsync("InvalidStatus");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid leave status: InvalidStatus");
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsFilteredDtos()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var leaves = new List<Leave> { CreateLeave() };
        _leaveRepositoryMock.Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetUpcomingAsync Tests

    [Fact]
    public async Task GetUpcomingAsync_ReturnsUpcomingLeaves()
    {
        // Arrange
        var leaves = new List<Leave> { CreateLeave() };
        _leaveRepositoryMock.Setup(r => r.GetUpcomingAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetUpcomingAsync(30);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUpcomingAsync_WithCustomDays_UsesProvidedValue()
    {
        // Arrange
        var leaves = new List<Leave> { CreateLeave() };
        _leaveRepositoryMock.Setup(r => r.GetUpcomingAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetUpcomingAsync(60);

        // Assert
        _leaveRepositoryMock.Verify(r => r.GetUpcomingAsync(60, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesLeave()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19),
            Reason = "Family vacation",
            Notes = "Going to Europe"
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _leaveRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Leave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave l, CancellationToken ct) => l);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.DirectReportId.Should().Be(_testDirectReportId);
        result.Type.Should().Be("Vacation");
        result.StartDate.Should().Be(dto.StartDate.Date);
        result.EndDate.Should().Be(dto.EndDate.Date);
        result.Reason.Should().Be(dto.Reason);
        result.Notes.Should().Be(dto.Notes);
        result.Status.Should().Be(LeaveStatus.Pending.ToString());
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDirectReport_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = Guid.NewGuid(),
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(dto.DirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Direct report with ID {dto.DirectReportId} not found.");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidLeaveType_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "InvalidType",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid leave type: InvalidType");
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        var existingLeave = CreateLeave();
        existingLeave.Approve("Manager");

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingLeave });

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This leave request overlaps with an existing leave.");
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingPendingLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        var existingLeave = CreateLeave(); // Status is Pending

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingLeave });

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This leave request overlaps with an existing leave.");
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingCancelledLeave_Succeeds()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        var existingLeave = CreateLeave();
        existingLeave.Cancel();

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingLeave });
        _leaveRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Leave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave l, CancellationToken ct) => l);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingRejectedLeave_Succeeds()
    {
        // Arrange
        var dto = new CreateLeaveDto
        {
            DirectReportId = _testDirectReportId,
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        var existingLeave = CreateLeave();
        existingLeave.Reject();

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingLeave });
        _leaveRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Leave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave l, CancellationToken ct) => l);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesLeave()
    {
        // Arrange
        var leave = CreateLeave();
        var dto = new UpdateLeaveDto
        {
            Type = "SickLeave",
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 5),
            Reason = "Updated reason",
            Notes = "Updated notes"
        };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.UpdateAsync(leave.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("SickLeave");
        result.StartDate.Should().Be(dto.StartDate.Date);
        result.EndDate.Should().Be(dto.EndDate.Date);
        _leaveRepositoryMock.Verify(r => r.UpdateAsync(leave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentLeave_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        var dto = new UpdateLeaveDto
        {
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        // Act
        var act = () => _service.UpdateAsync(leaveId, dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Leave with ID {leaveId} not found.");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidLeaveType_ThrowsArgumentException()
    {
        // Arrange
        var leave = CreateLeave();
        var dto = new UpdateLeaveDto
        {
            Type = "InvalidType",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        // Act
        var act = () => _service.UpdateAsync(leave.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid leave type: InvalidType");
    }

    [Fact]
    public async Task UpdateAsync_WithOverlappingLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = CreateLeave();
        var overlappingLeave = CreateLeave();
        overlappingLeave.Approve("Manager");

        var dto = new UpdateLeaveDto
        {
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { overlappingLeave });

        // Act
        var act = () => _service.UpdateAsync(leave.Id, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This leave request overlaps with an existing leave.");
    }

    #endregion

    #region ApproveAsync Tests

    [Fact]
    public async Task ApproveAsync_WithValidData_ApprovesLeave()
    {
        // Arrange
        var leave = CreateLeave();
        var dto = new ApproveLeaveDto { ApprovedBy = "Jane Manager" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.ApproveAsync(leave.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaveStatus.Approved.ToString());
        result.ApprovedBy.Should().Be("Jane Manager");
        result.ApprovedAt.Should().NotBeNull();
        _leaveRepositoryMock.Verify(r => r.UpdateAsync(leave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentLeave_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        var dto = new ApproveLeaveDto { ApprovedBy = "Manager" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        // Act
        var act = () => _service.ApproveAsync(leaveId, dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Leave with ID {leaveId} not found.");
    }

    [Fact]
    public async Task ApproveAsync_OnAlreadyApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = CreateLeave();
        leave.Approve("First Manager");
        var dto = new ApproveLeaveDto { ApprovedBy = "Second Manager" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        // Act
        var act = () => _service.ApproveAsync(leave.Id, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot approve a leave request with status: Approved");
    }

    #endregion

    #region RejectAsync Tests

    [Fact]
    public async Task RejectAsync_WithValidData_RejectsLeave()
    {
        // Arrange
        var leave = CreateLeave();
        var dto = new RejectLeaveDto { Notes = "Insufficient leave balance" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.RejectAsync(leave.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaveStatus.Rejected.ToString());
        result.Notes.Should().Be("Insufficient leave balance");
        _leaveRepositoryMock.Verify(r => r.UpdateAsync(leave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_WithNonExistentLeave_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        var dto = new RejectLeaveDto { Notes = "Rejection reason" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        // Act
        var act = () => _service.RejectAsync(leaveId, dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Leave with ID {leaveId} not found.");
    }

    [Fact]
    public async Task RejectAsync_OnApprovedLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = CreateLeave();
        leave.Approve("Manager");
        var dto = new RejectLeaveDto { Notes = "Changed mind" };

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        // Act
        var act = () => _service.RejectAsync(leave.Id, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot reject a leave request with status: Approved");
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_WithValidLeave_CancelsLeave()
    {
        // Arrange
        var leave = CreateLeave();

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.CancelAsync(leave.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(LeaveStatus.Cancelled.ToString());
        _leaveRepositoryMock.Verify(r => r.UpdateAsync(leave, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WithNonExistentLeave_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        // Act
        var act = () => _service.CancelAsync(leaveId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Leave with ID {leaveId} not found.");
    }

    [Fact]
    public async Task CancelAsync_OnAlreadyCancelledLeave_ThrowsInvalidOperationException()
    {
        // Arrange
        var leave = CreateLeave();
        leave.Cancel();

        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        // Act
        var act = () => _service.CancelAsync(leave.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Leave request is already cancelled.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesLeave()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        _leaveRepositoryMock.Setup(r => r.ExistsAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(leaveId);

        // Assert
        _leaveRepositoryMock.Verify(r => r.DeleteAsync(leaveId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        _leaveRepositoryMock.Setup(r => r.ExistsAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(leaveId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Leave with ID {leaveId} not found.");
    }

    #endregion

    #region GetTeamOverviewAsync Tests

    [Fact]
    public async Task GetTeamOverviewAsync_ReturnsCorrectOverview()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var leave1 = CreateLeave(LeaveType.Vacation, today, today.AddDays(2));
        leave1.Approve("Manager");

        var leave2 = CreateLeave(LeaveType.SickLeave, today.AddDays(5), today.AddDays(7));

        var leave3 = CreateLeave(LeaveType.PTO, today.AddDays(10), today.AddDays(15));
        leave3.Approve("Manager");

        var allLeaves = new List<Leave> { leave1, leave2, leave3 };

        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allLeaves);
        _leaveRepositoryMock.Setup(r => r.GetUpcomingAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { leave3 });
        _leaveRepositoryMock.Setup(r => r.GetByMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetTeamOverviewAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalLeaveRequests.Should().Be(3);
        result.PendingRequests.Should().Be(1);
        result.ApprovedRequests.Should().Be(2);
        result.TeamMembersOnLeaveToday.Should().Be(1);
    }

    #endregion

    #region GetMonthlyTrendAsync Tests

    [Fact]
    public async Task GetMonthlyTrendAsync_ReturnsCorrectTrend()
    {
        // Arrange
        var leave1 = CreateLeave(LeaveType.Vacation);
        leave1.Approve("Manager");
        var leave2 = CreateLeave(LeaveType.SickLeave);
        leave2.Approve("Manager");

        _leaveRepositoryMock.Setup(r => r.GetByMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { leave1, leave2 });

        // Act
        var result = await _service.GetMonthlyTrendAsync(6);

        // Assert
        result.Should().HaveCount(6);
        result.Should().AllSatisfy(month =>
        {
            month.Year.Should().BeGreaterThan(0);
            month.Month.Should().BeInRange(1, 12);
            month.MonthName.Should().NotBeNullOrEmpty();
        });
    }

    #endregion

    #region GetTeamBalancesAsync Tests

    [Fact]
    public async Task GetTeamBalancesAsync_ReturnsCorrectBalances()
    {
        // Arrange
        var year = 2024;
        var ptoLeave = CreateLeave(LeaveType.PTO, new DateTime(2024, 1, 15), new DateTime(2024, 1, 19));
        ptoLeave.Approve("Manager");

        var vacationLeave = CreateLeave(LeaveType.Vacation, new DateTime(2024, 2, 1), new DateTime(2024, 2, 5));
        vacationLeave.Approve("Manager");

        var pendingLeave = CreateLeave(LeaveType.SickLeave, new DateTime(2024, 3, 1), new DateTime(2024, 3, 3));

        var allLeaves = new List<Leave> { ptoLeave, vacationLeave, pendingLeave };

        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _leaveRepositoryMock.Setup(r => r.GetByDateRangeAsync(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(allLeaves);

        // Act
        var result = await _service.GetTeamBalancesAsync(year);

        // Assert
        result.Should().HaveCount(1);
        var balance = result[0];
        balance.DirectReportId.Should().Be(_testDirectReportId);
        balance.DirectReportName.Should().Be(_testDirectReport.FullName);
        balance.Year.Should().Be(year);
        balance.PtoUsed.Should().Be(ptoLeave.BusinessDaysCount);
        balance.VacationUsed.Should().Be(vacationLeave.BusinessDaysCount);
        balance.SickLeaveUsed.Should().Be(0); // Pending, not approved
        balance.TotalUsed.Should().Be(ptoLeave.BusinessDaysCount + vacationLeave.BusinessDaysCount);
        balance.PendingDays.Should().Be(pendingLeave.BusinessDaysCount);
    }

    #endregion

    #region Helper Methods

    private Leave CreateLeave(LeaveType type = LeaveType.Vacation, DateTime? start = null, DateTime? end = null)
    {
        var startDate = start ?? new DateTime(2024, 1, 15);
        var endDate = end ?? new DateTime(2024, 1, 19);

        return new Leave(
            _testDirectReportId,
            type,
            startDate,
            endDate,
            "Test reason",
            "Test notes");
    }

    #endregion
}
