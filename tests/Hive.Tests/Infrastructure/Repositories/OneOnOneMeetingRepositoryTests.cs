using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class OneOnOneMeetingRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly OneOnOneMeetingRepository _repository;
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    public OneOnOneMeetingRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new OneOnOneMeetingRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new OneOnOneMeetingRepository(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_StoresTheMeeting()
    {
        // Arrange
        var meeting = Add(_alice, daysAgo: 1);

        // Act
        var result = await _repository.GetByIdAsync(meeting.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(meeting.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        // Arrange
        Add(_alice, daysAgo: 10, content: "Older");
        Add(_alice, daysAgo: 1, content: "Newer");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Select(m => m.Title).Should().ContainInOrder("Newer", "Older");
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsOnlyThatPersonsMeetings()
    {
        // Arrange
        Add(_alice, daysAgo: 1, content: "With Alice");
        Add(_bob, daysAgo: 2, content: "With Bob");

        // Act
        var result = await _repository.GetByDirectReportIdAsync(_alice);

        // Assert
        result.Should().ContainSingle().Which.Title.Should().Be("With Alice");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithoutFilters_ReturnsEverything()
    {
        // Arrange
        Add(_alice, daysAgo: 1);
        Add(_bob, daysAgo: 2);
        Add(null, daysAgo: 3);

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, null, false, null);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithADirectReport_ReturnsOnlyTheirMeetings()
    {
        // Arrange
        Add(_alice, daysAgo: 1, content: "With Alice");
        Add(_bob, daysAgo: 2, content: "With Bob");

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, _alice, false, null);

        // Assert
        totalCount.Should().Be(1);
        items[0].Title.Should().Be("With Alice");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_UnlinkedOnly_ReturnsMeetingsWithNobody()
    {
        // Arrange
        Add(_alice, daysAgo: 1, content: "With Alice");
        Add(null, daysAgo: 2, content: "With nobody");

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(0, 20, null, true, null);

        // Assert
        totalCount.Should().Be(1);
        items[0].Title.Should().Be("With nobody");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithASearchTerm_MatchesTitleContentAndTags()
    {
        // Arrange
        Add(_alice, daysAgo: 1, content: "Career chat\nWants to move towards staff", tags: "#alice");
        Add(_bob, daysAgo: 2, content: "Weekly sync", tags: "#bob");

        // Act
        var (byContent, _) = await _repository.GetFilteredPagedAsync(0, 20, null, false, "staff");
        var (byTag, _) = await _repository.GetFilteredPagedAsync(0, 20, null, false, "#bob");

        // Assert
        byContent.Should().ContainSingle().Which.Title.Should().Be("Career chat");
        byTag.Should().ContainSingle().Which.Title.Should().Be("Weekly sync");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_PagesTheResults()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            Add(_alice, daysAgo: i, content: $"Meeting {i}");
        }

        // Act
        var (items, totalCount) = await _repository.GetFilteredPagedAsync(2, 2, null, false, null);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
        items[0].Title.Should().Be("Meeting 3");
    }

    [Fact]
    public async Task GetCountsByDirectReportAsync_CountsPerPersonAndTheUnlinkedOnes()
    {
        // Arrange
        Add(_alice, daysAgo: 1);
        Add(_alice, daysAgo: 2);
        Add(null, daysAgo: 3);

        // Act
        var result = await _repository.GetCountsByDirectReportAsync();

        // Assert
        result.Single(c => c.DirectReportId == _alice).Count.Should().Be(2);
        result.Single(c => c.DirectReportId == null).Count.Should().Be(1);
    }

    [Fact]
    public async Task CountAsync_CountsEvery1on1()
    {
        // Arrange
        Add(_alice, daysAgo: 1);
        Add(_bob, daysAgo: 2);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTheStoredMeeting()
    {
        // Arrange
        var meeting = Add(_alice, daysAgo: 1, content: "Before");
        meeting.UpdateContent("After");

        // Act
        await _repository.UpdateAsync(meeting);

        // Assert
        var stored = await _repository.GetByIdAsync(meeting.Id);
        stored!.Title.Should().Be("After");
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheMeeting()
    {
        // Arrange
        var meeting = Add(_alice, daysAgo: 1);

        // Act
        await _repository.DeleteAsync(meeting.Id);

        // Assert
        (await _repository.ExistsAsync(meeting.Id)).Should().BeFalse();
    }

    private OneOnOneMeeting Add(Guid? directReportId, int daysAgo, string content = "Check-in", string? tags = null)
    {
        var meeting = new OneOnOneMeeting(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)),
            content,
            tags,
            directReportId);
        _context.OneOnOneMeetings.TryAdd(meeting.Id, meeting);
        return meeting;
    }
}
