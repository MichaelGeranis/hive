using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SprintRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SprintRepository _repository;

    public SprintRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SprintRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SprintRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var sprint = CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.GetByIdAsync(sprint.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sprint.Id);
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
    public async Task GetByNameAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.GetByNameAsync("LP_1Q24_S1");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("LP_1Q24_S1");
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.GetByNameAsync("lp_1q24_s1");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSprints()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");
        CreateAndAddSprint("LP_2Q24_S1");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrderDescending()
    {
        // Arrange
        var sprint1 = CreateAndAddSprint("LP_1Q24_S1"); // 2024101
        var sprint2 = CreateAndAddSprint("LP_2Q24_S3"); // 2024203
        var sprint3 = CreateAndAddSprint("LP_1Q25_S2"); // 2025102

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result[0].Id.Should().Be(sprint3.Id); // 2025102 (most recent)
        result[1].Id.Should().Be(sprint2.Id); // 2024203
        result[2].Id.Should().Be(sprint1.Id); // 2024101
    }

    [Fact]
    public async Task GetByTeamAsync_ReturnsMatchingSprints()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");
        CreateAndAddSprint("LP_2Q24_S1");
        CreateAndAddSprint("OPS_1Q24_S1");

        // Act
        var result = await _repository.GetByTeamAsync("LP");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.TeamName.Should().Be("LP"));
    }

    [Fact]
    public async Task GetByTeamAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.GetByTeamAsync("lp");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByYearQuarterAsync_ReturnsMatchingSprints()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");
        CreateAndAddSprint("LP_1Q24_S2");
        CreateAndAddSprint("LP_2Q24_S1");

        // Act
        var result = await _repository.GetByYearQuarterAsync(2024, 1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s =>
        {
            s.Year.Should().Be(2024);
            s.Quarter.Should().Be(1);
        });
    }

    [Fact]
    public async Task GetByYearQuarterAsync_OrdersBySprintNumber()
    {
        // Arrange
        var sprint1 = CreateAndAddSprint("LP_1Q24_S3");
        var sprint2 = CreateAndAddSprint("LP_1Q24_S1");
        var sprint3 = CreateAndAddSprint("LP_1Q24_S2");

        // Act
        var result = await _repository.GetByYearQuarterAsync(2024, 1);

        // Assert
        result[0].Id.Should().Be(sprint2.Id); // S1
        result[1].Id.Should().Be(sprint3.Id); // S2
        result[2].Id.Should().Be(sprint1.Id); // S3
    }

    [Fact]
    public async Task GetByYearAsync_ReturnsMatchingSprints()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");
        CreateAndAddSprint("LP_2Q24_S1");
        CreateAndAddSprint("LP_1Q25_S1");

        // Act
        var result = await _repository.GetByYearAsync(2024);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Year.Should().Be(2024));
    }

    [Fact]
    public async Task GetByYearAsync_OrdersByQuarterThenSprintNumber()
    {
        // Arrange
        var sprint1 = CreateAndAddSprint("LP_2Q24_S2");
        var sprint2 = CreateAndAddSprint("LP_1Q24_S1");
        var sprint3 = CreateAndAddSprint("LP_1Q24_S2");

        // Act
        var result = await _repository.GetByYearAsync(2024);

        // Assert
        result[0].Id.Should().Be(sprint2.Id); // Q1 S1
        result[1].Id.Should().Be(sprint3.Id); // Q1 S2
        result[2].Id.Should().Be(sprint1.Id); // Q2 S2
    }

    [Fact]
    public async Task AddAsync_AddsSprintToContext()
    {
        // Arrange
        var sprint = new Sprint("LP_1Q24_S1");

        // Act
        var result = await _repository.AddAsync(sprint);

        // Assert
        result.Should().Be(sprint);
        _context.Sprints.Should().ContainKey(sprint.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var sprint = CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var act = () => _repository.AddAsync(sprint);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSprintInContext()
    {
        // Arrange
        var sprint = CreateAndAddSprint("LP_1Q24_S1");
        sprint.Update("LP_2Q24_S1");

        // Act
        await _repository.UpdateAsync(sprint);

        // Assert
        var stored = _context.Sprints[sprint.Id];
        stored.Name.Should().Be("LP_2Q24_S1");
        stored.Quarter.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentSprint_ThrowsInvalidOperationException()
    {
        // Arrange
        var sprint = new Sprint("LP_1Q24_S1");

        // Act
        var act = () => _repository.UpdateAsync(sprint);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesSprintFromContext()
    {
        // Arrange
        var sprint = CreateAndAddSprint("LP_1Q24_S1");

        // Act
        await _repository.DeleteAsync(sprint.Id);

        // Assert
        _context.Sprints.Should().NotContainKey(sprint.Id);
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
        CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.ExistsAsync("LP_1Q24_S1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_IsCaseInsensitive()
    {
        // Arrange
        CreateAndAddSprint("LP_1Q24_S1");

        // Act
        var result = await _repository.ExistsAsync("lp_1q24_s1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    private Sprint CreateAndAddSprint(string name)
    {
        var sprint = new Sprint(name);
        _context.Sprints.TryAdd(sprint.Id, sprint);
        return sprint;
    }
}
