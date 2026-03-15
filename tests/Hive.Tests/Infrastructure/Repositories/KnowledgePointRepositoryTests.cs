using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class KnowledgePointRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly KnowledgePointRepository _repository;

    public KnowledgePointRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new KnowledgePointRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new KnowledgePointRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var kp = CreateAndAddKnowledgePoint();

        // Act
        var result = await _repository.GetByIdAsync(kp.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(kp.Id);
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
    public async Task GetAllAsync_ReturnsAllKnowledgePoints()
    {
        // Arrange
        CreateAndAddKnowledgePoint();
        CreateAndAddKnowledgePoint();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_ReturnsMatchingKnowledgePoints()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        CreateAndAddKnowledgePoint(directReportId: directReportId);
        CreateAndAddKnowledgePoint(directReportId: directReportId);
        CreateAndAddKnowledgePoint(); // different direct report

        // Act
        var result = await _repository.GetByDirectReportIdAsync(directReportId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(kp => kp.DirectReportId.Should().Be(directReportId));
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddKnowledgePoint();

        // Act
        var result = await _repository.GetByDirectReportIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsMatchingKnowledgePoints()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        CreateAndAddKnowledgePoint(projectId: projectId);
        CreateAndAddKnowledgePoint(projectId: projectId);
        CreateAndAddKnowledgePoint(); // different project

        // Act
        var result = await _repository.GetByProjectIdAsync(projectId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(kp => kp.ProjectId.Should().Be(projectId));
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        CreateAndAddKnowledgePoint();

        // Act
        var result = await _repository.GetByProjectIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDirectReportAndProjectAsync_WhenExists_ReturnsEntity()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var kp = CreateAndAddKnowledgePoint(directReportId: directReportId, projectId: projectId);

        // Act
        var result = await _repository.GetByDirectReportAndProjectAsync(directReportId, projectId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(kp.Id);
    }

    [Fact]
    public async Task GetByDirectReportAndProjectAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByDirectReportAndProjectAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AddsKnowledgePointToContext()
    {
        // Arrange
        var kp = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10, "Great work");

        // Act
        var result = await _repository.AddAsync(kp);

        // Assert
        result.Should().Be(kp);
        _context.KnowledgePoints.Should().ContainKey(kp.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var kp = CreateAndAddKnowledgePoint();

        // Act
        var act = () => _repository.AddAsync(kp);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesKnowledgePointInContext()
    {
        // Arrange
        var kp = CreateAndAddKnowledgePoint();
        kp.UpdateManualPoints(50, "Updated notes");

        // Act
        await _repository.UpdateAsync(kp);

        // Assert
        var stored = _context.KnowledgePoints[kp.Id];
        stored.ManualPoints.Should().Be(50);
        stored.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var kp = new KnowledgePoint(Guid.NewGuid(), Guid.NewGuid(), 10, "Notes");

        // Act
        var act = () => _repository.UpdateAsync(kp);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_RemovesKnowledgePointFromContext()
    {
        // Arrange
        var kp = CreateAndAddKnowledgePoint();

        // Act
        await _repository.DeleteAsync(kp.Id);

        // Assert
        _context.KnowledgePoints.Should().NotContainKey(kp.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    private KnowledgePoint CreateAndAddKnowledgePoint(
        Guid? directReportId = null,
        Guid? projectId = null,
        int manualPoints = 10,
        string? notes = null)
    {
        var kp = new KnowledgePoint(
            directReportId ?? Guid.NewGuid(),
            projectId ?? Guid.NewGuid(),
            manualPoints,
            notes);
        _context.KnowledgePoints.TryAdd(kp.Id, kp);
        return kp;
    }
}
