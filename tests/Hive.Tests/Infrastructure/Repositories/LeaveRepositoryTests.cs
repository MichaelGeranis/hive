using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class LeaveRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly LeaveRepository _repository;
    private readonly Guid _directReportId;

    public LeaveRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new LeaveRepository(_context);
        _directReportId = Guid.NewGuid();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LeaveRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var leave = CreateAndAddLeave();

        // Act
        var result = await _repository.GetByIdAsync(leave.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(leave.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllLeaves()
    {
        // Arrange
        CreateAndAddLeave(startDate: DateTime.UtcNow.Date.AddDays(10));
        CreateAndAddLeave(startDate: DateTime.UtcNow.Date.AddDays(20));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByStartDateDescending()
    {
        // Arrange
        var leave1 = CreateAndAddLeave(startDate: DateTime.UtcNow.Date.AddDays(10));
        var leave2 = CreateAndAddLeave(startDate: DateTime.UtcNow.Date.AddDays(20));
        var leave3 = CreateAndAddLeave(startDate: DateTime.UtcNow.Date.AddDays(5));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(leave2.Id); // Most recent first
        result[1].Id.Should().Be(leave1.Id);
        result[2].Id.Should().Be(leave3.Id);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingLeaves()
    {
        // Arrange
        var reportId1 = Guid.NewGuid();
        var reportId2 = Guid.NewGuid();
        CreateAndAddLeave(directReportId: reportId1);
        CreateAndAddLeave(directReportId: reportId1);
        CreateAndAddLeave(directReportId: reportId2);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(reportId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(l => l.DirectReportId.Should().Be(reportId1));
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsOverlappingLeaves()
    {
        // Arrange
        var searchStart = new DateTime(2024, 6, 15);
        var searchEnd = new DateTime(2024, 6, 25);

        // Leave fully within range
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 18), endDate: new DateTime(2024, 6, 22));

        // Leave overlapping start
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 10), endDate: new DateTime(2024, 6, 20));

        // Leave overlapping end
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 20), endDate: new DateTime(2024, 6, 30));

        // Leave fully containing range
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 1), endDate: new DateTime(2024, 6, 30));

        // Leave completely outside range
        CreateAndAddLeave(startDate: new DateTime(2024, 7, 1), endDate: new DateTime(2024, 7, 10));

        // Act
        var result = await _repository.GetByDateRangeAsync(searchStart, searchEnd);

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetByMonthAsync_ReturnsLeavesInMonth()
    {
        // Arrange
        // Leave fully in June
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 10), endDate: new DateTime(2024, 6, 15));

        // Leave starting in June, ending in July
        CreateAndAddLeave(startDate: new DateTime(2024, 6, 25), endDate: new DateTime(2024, 7, 5));

        // Leave starting in May, ending in June
        CreateAndAddLeave(startDate: new DateTime(2024, 5, 25), endDate: new DateTime(2024, 6, 5));

        // Leave in different month
        CreateAndAddLeave(startDate: new DateTime(2024, 8, 1), endDate: new DateTime(2024, 8, 10));

        // Act
        var result = await _repository.GetByMonthAsync(2024, 6);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsLeavesWithinDays()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // Leave starting tomorrow
        CreateAndAddLeave(startDate: today.AddDays(1), endDate: today.AddDays(3));

        // Leave starting in 15 days
        CreateAndAddLeave(startDate: today.AddDays(15), endDate: today.AddDays(17));

        // Leave starting in 29 days (within 30 day window)
        CreateAndAddLeave(startDate: today.AddDays(29), endDate: today.AddDays(31));

        // Leave starting in 31 days (outside 30 day window)
        CreateAndAddLeave(startDate: today.AddDays(31), endDate: today.AddDays(33));

        // Leave in the past
        CreateAndAddLeave(startDate: today.AddDays(-10), endDate: today.AddDays(-8));

        // Act
        var result = await _repository.GetUpcomingAsync(30);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(l => l.StartDate);
    }

    [Fact]
    public async Task GetUpcomingAsync_WithCustomDays_ReturnsCorrectLeaves()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        CreateAndAddLeave(startDate: today.AddDays(1), endDate: today.AddDays(3));
        CreateAndAddLeave(startDate: today.AddDays(5), endDate: today.AddDays(7));
        CreateAndAddLeave(startDate: today.AddDays(8), endDate: today.AddDays(10));

        // Act
        var result = await _repository.GetUpcomingAsync(7);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverlappingLeavesAsync_ReturnsOverlappingLeavesForDirectReport()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var otherReportId = Guid.NewGuid();

        // Overlapping leave for same direct report
        CreateAndAddLeave(
            directReportId: reportId,
            startDate: new DateTime(2024, 6, 10),
            endDate: new DateTime(2024, 6, 20));

        // Non-overlapping leave for same direct report
        CreateAndAddLeave(
            directReportId: reportId,
            startDate: new DateTime(2024, 7, 10),
            endDate: new DateTime(2024, 7, 20));

        // Overlapping leave for different direct report (should not be included)
        CreateAndAddLeave(
            directReportId: otherReportId,
            startDate: new DateTime(2024, 6, 15),
            endDate: new DateTime(2024, 6, 25));

        // Act
        var result = await _repository.GetOverlappingLeavesAsync(
            reportId,
            new DateTime(2024, 6, 15),
            new DateTime(2024, 6, 25));

        // Assert
        result.Should().HaveCount(1);
        result[0].DirectReportId.Should().Be(reportId);
    }

    [Fact]
    public async Task GetOverlappingLeavesAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var leave1 = CreateAndAddLeave(
            directReportId: reportId,
            startDate: new DateTime(2024, 6, 10),
            endDate: new DateTime(2024, 6, 20));

        var leave2 = CreateAndAddLeave(
            directReportId: reportId,
            startDate: new DateTime(2024, 6, 15),
            endDate: new DateTime(2024, 6, 25));

        // Act
        var result = await _repository.GetOverlappingLeavesAsync(
            reportId,
            new DateTime(2024, 6, 10),
            new DateTime(2024, 6, 20),
            excludeId: leave1.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(leave2.Id);
    }

    [Fact]
    public async Task AddAsync_AddsLeaveToContext()
    {
        // Arrange
        var leave = new Leave(_directReportId, LeaveType.Vacation, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(5));

        // Act
        var result = await _repository.AddAsync(leave);

        // Assert
        result.Should().Be(leave);
        _context.Leaves.Should().ContainKey(leave.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLeaveInContext()
    {
        // Arrange
        var leave = CreateAndAddLeave();
        leave.Update(LeaveType.Sick, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(2), "Updated notes");

        // Act
        await _repository.UpdateAsync(leave);

        // Assert
        var stored = _context.Leaves[leave.Id];
        stored.Type.Should().Be(LeaveType.Sick);
        stored.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task DeleteAsync_RemovesLeaveFromContext()
    {
        // Arrange
        var leave = CreateAndAddLeave();

        // Act
        await _repository.DeleteAsync(leave.Id);

        // Assert
        _context.Leaves.Should().NotContainKey(leave.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var leave = CreateAndAddLeave();

        // Act
        var result = await _repository.ExistsAsync(leave.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    private Leave CreateAndAddLeave(
        Guid? directReportId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        LeaveType type = LeaveType.Vacation)
    {
        var leave = new Leave(
            directReportId ?? _directReportId,
            type,
            startDate ?? DateTime.UtcNow.Date,
            endDate ?? DateTime.UtcNow.Date.AddDays(5),
            "Test leave");
        _context.Leaves.TryAdd(leave.Id, leave);
        return leave;
    }
}
