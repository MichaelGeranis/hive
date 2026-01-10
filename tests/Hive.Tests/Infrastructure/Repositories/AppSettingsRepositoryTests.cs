using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class AppSettingsRepositoryTests
{
    private readonly InMemoryDbContext _context;
    private readonly AppSettingsRepository _repository;

    public AppSettingsRepositoryTests()
    {
        _context = new InMemoryDbContext();
        _repository = new AppSettingsRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AppSettingsRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetAsync_WhenExists_ReturnsSettings()
    {
        // Arrange
        var settings = new AppSettings("{\"mapping\": \"test\"}");
        _context.AppSettings.Add(settings);

        // Act
        var result = await _repository.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(settings.Id);
    }

    [Fact]
    public async Task GetAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _repository.GetAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_AddsSettingsToContext()
    {
        // Arrange
        var settings = new AppSettings("{\"mapping\": \"test\"}");

        // Act
        var result = await _repository.AddAsync(settings);

        // Assert
        result.Should().Be(settings);
        _context.AppSettings.Should().Contain(settings);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSettings()
    {
        // Arrange
        var settings = new AppSettings("{\"mapping\": \"test\"}");
        _context.AppSettings.Add(settings);
        settings.UpdateStoryPointMappings("{\"mapping\": \"updated\"}");

        // Act
        await _repository.UpdateAsync(settings);

        // Assert
        var stored = _context.AppSettings.First();
        stored.StoryPointMappings.Should().Be("{\"mapping\": \"updated\"}");
    }
}
