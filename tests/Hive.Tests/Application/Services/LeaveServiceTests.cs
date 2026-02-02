using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for LeaveService.
/// Simplified for capacity planning - no approval workflow.
/// </summary>
public class LeaveServiceTests
{
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly Mock<ISprintCapacityService> _sprintCapacityServiceMock;
    private readonly LeaveService _service;

    private readonly Guid _testDirectReportId = Guid.NewGuid();
    private readonly DirectReport _testDirectReport;

    public LeaveServiceTests()
    {
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _sprintCapacityServiceMock = new Mock<ISprintCapacityService>();
        _service = new LeaveService(
            _leaveRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object,
            _sprintCapacityServiceMock.Object);

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
            CreateLeave(LeaveType.Sick)
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
        result[1].Type.Should().Be(LeaveType.Sick.ToString());
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
            CreateLeave(LeaveType.Sick)
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
            Notes = "Family vacation"
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
        result.Notes.Should().Be(dto.Notes);
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
    public async Task CreateAsync_WithOverlappingLeave_ThrowsInvalidOperationException()
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

        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);
        _leaveRepositoryMock.Setup(r => r.GetOverlappingLeavesAsync(_testDirectReportId, dto.StartDate, dto.EndDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { existingLeave });

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This leave overlaps with an existing leave record.");
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
            Type = "Sick",
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 5),
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
        result.Type.Should().Be("Sick");
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
            .WithMessage("This leave overlaps with an existing leave record.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesLeave()
    {
        // Arrange
        var leave = new Leave(_testDirectReportId, LeaveType.Vacation, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5));
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        // Act
        await _service.DeleteAsync(leave.Id);

        // Assert
        _leaveRepositoryMock.Verify(r => r.DeleteAsync(leave.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsKeyNotFoundException()
    {
        // Arrange
        var leaveId = Guid.NewGuid();
        _leaveRepositoryMock.Setup(r => r.GetByIdAsync(leaveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

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
        var leave2 = CreateLeave(LeaveType.Sick, today.AddDays(5), today.AddDays(7));
        var leave3 = CreateLeave(LeaveType.Vacation, today.AddDays(10), today.AddDays(15));

        var allLeaves = new List<Leave> { leave1, leave2, leave3 };

        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allLeaves);
        _leaveRepositoryMock.Setup(r => r.GetUpcomingAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave> { leave3 });
        _leaveRepositoryMock.Setup(r => r.GetByMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport> { _testDirectReport });
        _directReportRepositoryMock.Setup(r => r.GetByIdAsync(_testDirectReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDirectReport);

        // Act
        var result = await _service.GetTeamOverviewAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalLeaveRecords.Should().Be(3);
        result.TeamMembersOnLeaveToday.Should().Be(1);
    }

    #endregion

    #region GetMonthlyTrendAsync Tests

    [Fact]
    public async Task GetMonthlyTrendAsync_ReturnsCorrectTrend()
    {
        // Arrange
        var leave1 = CreateLeave(LeaveType.Vacation);
        var leave2 = CreateLeave(LeaveType.Sick);

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
        var vacationLeave = CreateLeave(LeaveType.Vacation, new DateTime(2024, 1, 15), new DateTime(2024, 1, 19));
        var sickLeave = CreateLeave(LeaveType.Sick, new DateTime(2024, 2, 1), new DateTime(2024, 2, 3));
        var otherLeave = CreateLeave(LeaveType.Other, new DateTime(2024, 3, 1), new DateTime(2024, 3, 2));

        var allLeaves = new List<Leave> { vacationLeave, sickLeave, otherLeave };

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
        balance.VacationUsed.Should().Be(vacationLeave.BusinessDaysCount);
        balance.SickLeaveUsed.Should().Be(sickLeave.BusinessDaysCount);
        balance.OtherUsed.Should().Be(otherLeave.BusinessDaysCount);
        balance.TotalUsed.Should().Be(vacationLeave.BusinessDaysCount + sickLeave.BusinessDaysCount + otherLeave.BusinessDaysCount);
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
            "Test notes");
    }

    #endregion
}
