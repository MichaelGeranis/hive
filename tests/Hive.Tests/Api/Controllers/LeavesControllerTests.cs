using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for LeavesController.
/// Simplified for capacity planning - no approval workflow.
/// </summary>
public class LeavesControllerTests
{
    private readonly Mock<ILeaveService> _serviceMock;
    private readonly Mock<ILogger<LeavesController>> _loggerMock;
    private readonly LeavesController _controller;

    public LeavesControllerTests()
    {
        _serviceMock = new Mock<ILeaveService>();
        _loggerMock = new Mock<ILogger<LeavesController>>();
        _controller = new LeavesController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllLeaves()
    {
        // Arrange
        var leaves = new List<LeaveDto>
        {
            CreateDto(),
            CreateDto()
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLeaves = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveDto>>().Subject;
        returnedLeaves.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaveDto>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLeaves = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveDto>>().Subject;
        returnedLeaves.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithLeave()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<LeaveDto>().Subject;
        returnedDto.Id.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaveDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByDirectReport Tests

    [Fact]
    public async Task GetByDirectReport_ReturnsOkWithFilteredLeaves()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var leaves = new List<LeaveDto>
        {
            CreateDto(directReportId: directReportId),
            CreateDto(directReportId: directReportId)
        };
        _serviceMock.Setup(s => s.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        // Act
        var result = await _controller.GetByDirectReport(directReportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLeaves = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveDto>>().Subject;
        returnedLeaves.Should().HaveCount(2);
        returnedLeaves.Should().AllSatisfy(l => l.DirectReportId.Should().Be(directReportId));
    }

    #endregion

    #region GetByDateRange Tests

    [Fact]
    public async Task GetByDateRange_ReturnsOkWithFilteredLeaves()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var leaves = new List<LeaveDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        // Act
        var result = await _controller.GetByDateRange(startDate, endDate, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLeaves = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveDto>>().Subject;
        returnedLeaves.Should().HaveCount(1);
    }

    #endregion

    #region GetUpcoming Tests

    [Fact]
    public async Task GetUpcoming_WithDefaultDays_ReturnsOkWithUpcomingLeaves()
    {
        // Arrange
        var leaves = new List<LeaveDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetUpcomingAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        // Act
        var result = await _controller.GetUpcoming(30, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLeaves = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveDto>>().Subject;
        returnedLeaves.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUpcoming_WithCustomDays_ReturnsOkWithUpcomingLeaves()
    {
        // Arrange
        var days = 60;
        var leaves = new List<LeaveDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetUpcomingAsync(days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        // Act
        var result = await _controller.GetUpcoming(days, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _serviceMock.Verify(s => s.GetUpcomingAsync(days, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetOverview Tests

    [Fact]
    public async Task GetOverview_ReturnsOkWithOverview()
    {
        // Arrange
        var overview = new TeamLeaveOverviewDto
        {
            TotalLeaveRecords = 10,
            TeamMembersOnLeaveToday = 2,
            TeamMembersOnLeaveThisWeek = 4,
            UpcomingLeaves = new List<LeaveDto>(),
            CurrentLeaves = new List<LeaveDto>(),
            MonthlyTrend = new List<MonthlyLeaveSummaryDto>()
        };
        _serviceMock.Setup(s => s.GetTeamOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        // Act
        var result = await _controller.GetOverview(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOverview = okResult.Value.Should().BeOfType<TeamLeaveOverviewDto>().Subject;
        returnedOverview.TotalLeaveRecords.Should().Be(10);
        returnedOverview.TeamMembersOnLeaveToday.Should().Be(2);
        returnedOverview.TeamMembersOnLeaveThisWeek.Should().Be(4);
    }

    #endregion

    #region GetMonthlyTrend Tests

    [Fact]
    public async Task GetMonthlyTrend_WithDefaultMonths_ReturnsOkWithTrend()
    {
        // Arrange
        var trend = new List<MonthlyLeaveSummaryDto>
        {
            new MonthlyLeaveSummaryDto { Year = 2024, Month = 1, MonthName = "Jan" }
        };
        _serviceMock.Setup(s => s.GetMonthlyTrendAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trend);

        // Act
        var result = await _controller.GetMonthlyTrend(12, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTrend = okResult.Value.Should().BeAssignableTo<IEnumerable<MonthlyLeaveSummaryDto>>().Subject;
        returnedTrend.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMonthlyTrend_WithCustomMonths_ReturnsOkWithTrend()
    {
        // Arrange
        var months = 6;
        var trend = new List<MonthlyLeaveSummaryDto>();
        _serviceMock.Setup(s => s.GetMonthlyTrendAsync(months, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trend);

        // Act
        var result = await _controller.GetMonthlyTrend(months, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.GetMonthlyTrendAsync(months, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetBalances Tests

    [Fact]
    public async Task GetBalances_ReturnsOkWithBalances()
    {
        // Arrange
        var year = 2024;
        var balances = new List<LeaveBalanceDto>
        {
            new LeaveBalanceDto
            {
                DirectReportId = Guid.NewGuid(),
                DirectReportName = "John Doe",
                Year = year,
                VacationUsed = 5,
                SickLeaveUsed = 2,
                OtherUsed = 1,
                TotalUsed = 8
            }
        };
        _serviceMock.Setup(s => s.GetTeamBalancesAsync(year, It.IsAny<CancellationToken>()))
            .ReturnsAsync(balances);

        // Act
        var result = await _controller.GetBalances(year, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBalances = okResult.Value.Should().BeAssignableTo<IEnumerable<LeaveBalanceDto>>().Subject;
        returnedBalances.Should().HaveCount(1);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateLeaveDto
        {
            DirectReportId = Guid.NewGuid(),
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19),
            Notes = "Family vacation"
        };
        var resultDto = CreateDto();
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<LeaveDto>().Subject;
        returnedDto.Id.Should().Be(resultDto.Id);
    }

    [Fact]
    public async Task Create_WithNonExistentDirectReport_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateLeaveDto
        {
            DirectReportId = Guid.NewGuid(),
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Direct report with ID {createDto.DirectReportId} not found."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithInvalidLeaveType_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateLeaveDto
        {
            DirectReportId = Guid.NewGuid(),
            Type = "InvalidType",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid leave type: InvalidType"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithOverlappingLeave_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateLeaveDto
        {
            DirectReportId = Guid.NewGuid(),
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("This leave overlaps with an existing leave record."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkWithUpdatedLeave()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateLeaveDto
        {
            Type = "Sick",
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 5),
            Notes = "Updated notes"
        };
        var resultDto = CreateDto(type: "Sick");
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<LeaveDto>().Subject;
        returnedDto.Type.Should().Be("Sick");
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateLeaveDto
        {
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Leave with ID {id} not found."));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithInvalidLeaveType_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateLeaveDto
        {
            Type = "InvalidType",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid leave type: InvalidType"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithOverlappingLeave_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateLeaveDto
        {
            Type = "Vacation",
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19)
        };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("This leave overlaps with an existing leave record."));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Leave with ID {id} not found."));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static LeaveDto CreateDto(
        Guid? id = null,
        Guid? directReportId = null,
        string type = "Vacation")
    {
        return new LeaveDto
        {
            Id = id ?? Guid.NewGuid(),
            DirectReportId = directReportId ?? Guid.NewGuid(),
            DirectReportName = "John Doe",
            Type = type,
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 1, 19),
            DaysCount = 5,
            BusinessDaysCount = 5,
            Notes = "Test notes",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
