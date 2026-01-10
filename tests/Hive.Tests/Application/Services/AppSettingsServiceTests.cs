using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for AppSettingsService.
/// </summary>
public class AppSettingsServiceTests
{
    private readonly Mock<IAppSettingsRepository> _repositoryMock;
    private readonly AppSettingsService _service;

    public AppSettingsServiceTests()
    {
        _repositoryMock = new Mock<IAppSettingsRepository>();
        _service = new AppSettingsService(_repositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AppSettingsService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenSettingsExist_ReturnsDto()
    {
        // Arrange
        var mappings = new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 2, Label = "1 SP = 2 hours" },
            new() { Points = 3, Hours = 8, Label = "3 SP = 8 hours" }
        };
        var mappingsJson = JsonSerializer.Serialize(mappings);
        var entity = new AppSettings(mappingsJson);

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(2);
        result.StoryPointMappings[0].Points.Should().Be(1);
        result.StoryPointMappings[0].Hours.Should().Be(2);
        result.StoryPointMappings[1].Points.Should().Be(3);
        result.StoryPointMappings[1].Hours.Should().Be(8);
    }

    [Fact]
    public async Task GetAsync_WhenSettingsDoNotExist_CreatesDefaultSettings()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings s, CancellationToken _) => s);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(7); // Default has 7 mappings
        result.StoryPointMappings[0].Points.Should().Be(1);
        result.StoryPointMappings[0].Hours.Should().Be(2);
        result.StoryPointMappings[^1].Points.Should().Be(21);
        result.StoryPointMappings[^1].Hours.Should().Be(240);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenSettingsHaveInvalidJson_ReturnsDefaultMappings()
    {
        // Arrange
        var entity = new AppSettings("invalid json");

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(7); // Falls back to defaults
    }

    [Fact]
    public async Task GetAsync_WhenSettingsHaveNullJson_ReturnsDefaultMappings()
    {
        // Arrange
        var entity = new AppSettings("null");

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(7); // Falls back to defaults
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenSettingsExist_UpdatesAndReturnsDto()
    {
        // Arrange
        var existingMappings = new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 2, Label = "Old" }
        };
        var existingEntity = new AppSettings(JsonSerializer.Serialize(existingMappings));

        var newMappings = new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 4, Label = "New" },
            new() { Points = 2, Hours = 8, Label = "New 2" }
        };

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateAppSettingsDto
        {
            StoryPointMappings = newMappings
        };

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(2);
        result.StoryPointMappings[0].Hours.Should().Be(4);
        result.StoryPointMappings[0].Label.Should().Be("New");

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSettingsDoNotExist_CreatesNewSettings()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings s, CancellationToken _) => s);

        var newMappings = new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 4, Label = "New" }
        };

        var dto = new UpdateAppSettingsDto
        {
            StoryPointMappings = newMappings
        };

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().HaveCount(1);
        result.StoryPointMappings[0].Points.Should().Be(1);
        result.StoryPointMappings[0].Hours.Should().Be(4);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyMappings_UpdatesSuccessfully()
    {
        // Arrange
        var existingEntity = new AppSettings("[]");

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>()
        };

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.StoryPointMappings.Should().BeEmpty();

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var existingEntity = new AppSettings("[]");

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>()
        };

        var cts = new CancellationTokenSource();

        // Act
        await _service.UpdateAsync(dto, cts.Token);

        // Assert
        _repositoryMock.Verify(r => r.GetAsync(cts.Token), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), cts.Token), Times.Once);
    }

    #endregion

    #region Default Mappings Tests

    [Fact]
    public async Task GetAsync_DefaultMappings_HaveCorrectValues()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings s, CancellationToken _) => s);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.StoryPointMappings.Should().HaveCount(7);

        // Verify specific mappings
        var mapping1 = result.StoryPointMappings.First(m => m.Points == 1);
        mapping1.Hours.Should().Be(2);
        mapping1.Label.Should().Be("1 SP = 2 hours");

        var mapping3 = result.StoryPointMappings.First(m => m.Points == 3);
        mapping3.Hours.Should().Be(8);
        mapping3.Label.Should().Be("3 SP = 8 hours (1 day)");

        var mapping8 = result.StoryPointMappings.First(m => m.Points == 8);
        mapping8.Hours.Should().Be(72);
        mapping8.Label.Should().Be("8 SP = 72 hours (1 sprint)");

        var mapping21 = result.StoryPointMappings.First(m => m.Points == 21);
        mapping21.Hours.Should().Be(240);
        mapping21.Label.Should().Be("21 SP = 240 hours (1 month)");
    }

    #endregion
}
