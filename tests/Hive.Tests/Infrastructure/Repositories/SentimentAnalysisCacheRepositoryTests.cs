using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SentimentAnalysisCacheRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly SentimentAnalysisCacheRepository _repository;

    public SentimentAnalysisCacheRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new SentimentAnalysisCacheRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SentimentAnalysisCacheRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenExists_ReturnsCache()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var cache = CreateAndAddCache(directReportId);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(directReportId);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(directReportId);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByDirectReportIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenMultipleCaches_ReturnsCorrectOne()
    {
        // Arrange
        var directReportId1 = Guid.NewGuid();
        var directReportId2 = Guid.NewGuid();
        CreateAndAddCache(directReportId1);
        CreateAndAddCache(directReportId2);

        // Act
        var result = await _repository.GetByDirectReportIdAsync(directReportId1);

        // Assert
        result.Should().NotBeNull();
        result!.DirectReportId.Should().Be(directReportId1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCaches()
    {
        // Arrange
        CreateAndAddCache(Guid.NewGuid());
        CreateAndAddCache(Guid.NewGuid());

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
    public async Task AddAsync_AddsCacheToContext()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var cache = new SentimentAnalysisCache(directReportId, 70.0, 20.0, 10.0, "Positive", "[\"theme\"]", "[]", 5, 90, DateTime.UtcNow.AddDays(-1));

        // Act
        var result = await _repository.AddAsync(cache);

        // Assert
        result.Should().Be(cache);
        _context.SentimentAnalysisCache.Should().ContainKey(cache.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var cache = CreateAndAddCache(Guid.NewGuid());

        // Act
        var act = () => _repository.AddAsync(cache);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCacheInContext()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var cache = CreateAndAddCache(directReportId);
        cache.Update(90.0, 5.0, 5.0, "VeryPositive", "[\"growth\"]", "[]", 10, 180, DateTime.UtcNow);

        // Act
        await _repository.UpdateAsync(cache);

        // Assert
        var stored = _context.SentimentAnalysisCache[cache.Id];
        stored.PositiveScore.Should().Be(90.0);
        stored.OverallSentiment.Should().Be("VeryPositive");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingCache()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var cache = CreateAndAddCache(directReportId);
        cache.Update(50.0, 30.0, 20.0, "Neutral", "[]", "[]", 3, 60, DateTime.UtcNow);

        // Act
        await _repository.UpdateAsync(cache);

        // Assert
        _context.SentimentAnalysisCache.Should().ContainKey(cache.Id);
        _context.SentimentAnalysisCache[cache.Id].PositiveScore.Should().Be(50.0);
    }

    [Fact]
    public async Task DeleteByDirectReportIdAsync_WhenExists_RemovesCache()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var cache = CreateAndAddCache(directReportId);

        // Act
        await _repository.DeleteByDirectReportIdAsync(directReportId);

        // Assert
        _context.SentimentAnalysisCache.Should().NotContainKey(cache.Id);
    }

    [Fact]
    public async Task DeleteByDirectReportIdAsync_WhenNotExists_DoesNotThrow()
    {
        // Act
        var act = () => _repository.DeleteByDirectReportIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteByDirectReportIdAsync_OnlyRemovesMatchingCache()
    {
        // Arrange
        var directReportId1 = Guid.NewGuid();
        var directReportId2 = Guid.NewGuid();
        var cache1 = CreateAndAddCache(directReportId1);
        var cache2 = CreateAndAddCache(directReportId2);

        // Act
        await _repository.DeleteByDirectReportIdAsync(directReportId1);

        // Assert
        _context.SentimentAnalysisCache.Should().NotContainKey(cache1.Id);
        _context.SentimentAnalysisCache.Should().ContainKey(cache2.Id);
    }

    private SentimentAnalysisCache CreateAndAddCache(Guid directReportId)
    {
        var cache = new SentimentAnalysisCache(directReportId, 70.0, 20.0, 10.0, "Positive", "[\"theme\"]", "[]", 5, 90, DateTime.UtcNow.AddDays(-1));
        _context.SentimentAnalysisCache.TryAdd(cache.Id, cache);
        return cache;
    }
}
