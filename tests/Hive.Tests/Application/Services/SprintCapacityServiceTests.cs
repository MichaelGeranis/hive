using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class SprintCapacityServiceTests
{
    private readonly Mock<ISprintCapacityRepository> _capacityRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<ILeaveRepository> _leaveRepositoryMock;
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly SprintCapacityService _service;

    public SprintCapacityServiceTests()
    {
        _capacityRepositoryMock = new Mock<ISprintCapacityRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _leaveRepositoryMock = new Mock<ILeaveRepository>();
        _directReportRepositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new SprintCapacityService(
            _capacityRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _leaveRepositoryMock.Object,
            _directReportRepositoryMock.Object,
            _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullCapacityRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityService(null!, _sprintRepositoryMock.Object, _leaveRepositoryMock.Object, _directReportRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("capacityRepository");
    }

    [Fact]
    public void Constructor_WithNullSprintRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityService(_capacityRepositoryMock.Object, null!, _leaveRepositoryMock.Object, _directReportRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sprintRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintCapacityService(_capacityRepositoryMock.Object, _sprintRepositoryMock.Object, _leaveRepositoryMock.Object, _directReportRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var entity = new SprintCapacity(sprintId, 100, 5);

        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.SprintId.Should().Be(sprintId);
        result.TotalCapacityPoints.Should().Be(100);
        result.AvailableMembers.Should().Be(5);
        result.SprintName.Should().Be("LP_4Q25_S6");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        var entity = new SprintCapacity(sprintId, 100, 5);

        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);

        // Act
        var result = await _service.GetBySprintIdAsync(sprintId);

        // Assert
        result.Should().NotBeNull();
        result!.SprintId.Should().Be(sprintId);
    }

    [Fact]
    public async Task GetBySprintIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var result = await _service.GetBySprintIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var sprint1Id = Guid.NewGuid();
        var sprint2Id = Guid.NewGuid();
        var sprint1 = new Sprint("LP_4Q25_S6");
        var sprint2 = new Sprint("LP_4Q25_S7");
        var entities = new List<SprintCapacity>
        {
            new SprintCapacity(sprint1Id, 100, 5),
            new SprintCapacity(sprint2Id, 120, 6)
        };

        _capacityRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint1);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint2);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].TotalCapacityPoints.Should().Be(100);
        result[1].TotalCapacityPoints.Should().Be(120);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenSprintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateSprintCapacityDto
        {
            SprintId = Guid.NewGuid(),
            TotalCapacityPoints = 100
        };
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(dto.SprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var act = async () => await _service.CreateOrUpdateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenCapacityNotExists_CreatesNew()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        sprint.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));
        var dto = new CreateSprintCapacityDto
        {
            SprintId = sprintId,
            TotalCapacityPoints = 100
        };

        SetupSprintWithIdMock(sprint, sprintId);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);
        _capacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity capacity, CancellationToken _) => capacity);
        SetupLeaveAndDirectReportMocks(3, new List<Leave>());

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TotalCapacityPoints.Should().Be(100);
        _capacityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Once);
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WhenCapacityExists_UpdatesExisting()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint("LP_4Q25_S6");
        sprint.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));
        var existing = new SprintCapacity(sprintId, 100, 5);
        var dto = new CreateSprintCapacityDto
        {
            SprintId = sprintId,
            TotalCapacityPoints = 120
        };

        SetupSprintWithIdMock(sprint, sprintId);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        SetupLeaveAndDirectReportMocks(5, new List<Leave>());

        // Act
        var result = await _service.CreateOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TotalCapacityPoints.Should().Be(120);
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Once);
        _capacityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCapacityExists_DeletesCapacity()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var entity = new SprintCapacity(sprintId, 100, 5);
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _capacityRepositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCapacityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _capacityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #region Recalculation Tests

    [Fact]
    public async Task RecalculateAvailableMembers_NoLeaves_FullTeam()
    {
        // Arrange: 5 direct reports, no leaves → all 5 available
        var sprint = new Sprint("LP_1Q26_S1");
        sprint.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));
        var sprintId = sprint.Id;

        SetupSprintWithIdMock(sprint, sprintId);
        SetupLeaveAndDirectReportMocks(5, new List<Leave>());

        var existing = new SprintCapacity(sprintId, 50, 3);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await _service.RecalculateAvailableMembersAsync(sprintId);

        // Assert
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<SprintCapacity>(c => c.AvailableMembers == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateAvailableMembers_FullLeaveOverlap_ReducesMembers()
    {
        // Arrange: 4 direct reports, 1 on leave for entire sprint → 3 available
        var sprint = new Sprint("LP_1Q26_S1");
        sprint.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));
        var sprintId = sprint.Id;

        var directReports = CreateDirectReports(4);
        var leaves = new List<Leave>
        {
            new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                DateTime.UtcNow.AddDays(7),
                DateTime.UtcNow.AddDays(20))
        };

        SetupSprintWithIdMock(sprint, sprintId);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        var existing = new SprintCapacity(sprintId, 50, 4);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await _service.RecalculateAvailableMembersAsync(sprintId);

        // Assert
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<SprintCapacity>(c => c.AvailableMembers == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateAvailableMembers_CreatesCapacityIfNotExists()
    {
        // Arrange: No existing capacity for this sprint
        var sprint = new Sprint("LP_1Q26_S1");
        sprint.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));
        var sprintId = sprint.Id;

        SetupSprintWithIdMock(sprint, sprintId);
        SetupLeaveAndDirectReportMocks(3, new List<Leave>());

        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);
        _capacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity capacity, CancellationToken _) => capacity);

        // Act
        await _service.RecalculateAvailableMembersAsync(sprintId);

        // Assert
        _capacityRepositoryMock.Verify(r => r.AddAsync(
            It.Is<SprintCapacity>(c => c.SprintId == sprintId && c.AvailableMembers == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateForDateRange_OnlyAffectsOverlappingSprints()
    {
        // Arrange: 2 sprints, only 1 overlaps the given date range
        var sprint1 = new Sprint("LP_1Q26_S1");
        sprint1.UpdateDates(DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(20));

        var sprint2 = new Sprint("LP_2Q26_S1");
        sprint2.UpdateDates(DateTime.UtcNow.AddDays(60), DateTime.UtcNow.AddDays(73));

        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Sprint> { sprint1, sprint2 });
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint1);
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprint2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint2);

        SetupLeaveAndDirectReportMocks(4, new List<Leave>());

        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity?)null);
        _capacityRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SprintCapacity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SprintCapacity capacity, CancellationToken _) => capacity);

        // Act — date range only overlaps sprint1
        await _service.RecalculateForDateRangeAsync(
            DateTime.UtcNow.AddDays(10),
            DateTime.UtcNow.AddDays(15));

        // Assert — only sprint1 should have been recalculated
        _capacityRepositoryMock.Verify(r => r.AddAsync(
            It.Is<SprintCapacity>(c => c.SprintId == sprint1.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _capacityRepositoryMock.Verify(r => r.AddAsync(
            It.Is<SprintCapacity>(c => c.SprintId == sprint2.Id),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateAvailableMembers_PartialLeaveOverlap_ProportionalReduction()
    {
        // Arrange: 4 direct reports, 1 on leave for ~half the sprint
        // Sprint: Mon-Fri, Mon-Fri (10 working days)
        // Leave: Mon-Fri (5 working days) — first week only
        // Lost capacity = 5/10 = 0.5 → floor(max(0, 4 - 0.5)) = 3

        // Find next Monday for deterministic working-day calculations
        var now = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;

        var sprintStart = now.AddDays(daysUntilMonday);
        var sprintEnd = sprintStart.AddDays(11); // 12 calendar days = 10 working days

        var sprint = new Sprint("LP_1Q26_S1");
        sprint.UpdateDates(sprintStart, sprintEnd);
        var sprintId = sprint.Id;

        var directReports = CreateDirectReports(4);
        var leaves = new List<Leave>
        {
            new Leave(
                directReports[0].Id,
                LeaveType.Vacation,
                sprintStart,
                sprintStart.AddDays(4)) // Mon-Fri = 5 working days
        };

        SetupSprintWithIdMock(sprint, sprintId);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);

        var existing = new SprintCapacity(sprintId, 50, 4);
        _capacityRepositoryMock.Setup(r => r.GetBySprintIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await _service.RecalculateAvailableMembersAsync(sprintId);

        // Assert — 5 leave days / 10 working days = 0.5 lost → floor(4 - 0.5) = 3
        _capacityRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<SprintCapacity>(c => c.AvailableMembers == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private List<DirectReport> CreateDirectReports(int count)
    {
        var list = new List<DirectReport>();
        for (int i = 0; i < count; i++)
        {
            var dr = new DirectReport(
                $"Person{i}",
                $"Last{i}",
                $"person{i}@test.com",
                "Dev",
                "Eng",
                new DateTime(2020, 1, 1));
            list.Add(dr);
        }
        return list;
    }

    private void SetupSprintWithIdMock(Sprint sprint, Guid sprintId)
    {
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
    }

    private void SetupLeaveAndDirectReportMocks(int directReportCount, List<Leave> leaves)
    {
        var directReports = CreateDirectReports(directReportCount);
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReports);
        _leaveRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaves);
    }

    #endregion
}
