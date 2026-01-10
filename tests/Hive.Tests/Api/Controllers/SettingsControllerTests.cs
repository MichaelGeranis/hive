using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for SettingsController.
/// </summary>
public class SettingsControllerTests
{
    private readonly Mock<IAppSettingsService> _serviceMock;
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _serviceMock = new Mock<IAppSettingsService>();
        _loggerMock = new Mock<ILogger<SettingsController>>();
        _controller = new SettingsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Get Tests

    [Fact]
    public async Task Get_ReturnsOkWithSettings()
    {
        // Arrange
        var settings = CreateDto();

        _serviceMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _controller.Get(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<AppSettingsDto>();
        var settingsResult = okResult.Value as AppSettingsDto;
        settingsResult.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkWithUpdatedSettings()
    {
        // Arrange
        var updateDto = new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>
            {
                new() { Points = 1, Hours = 2, Label = "XS" },
                new() { Points = 2, Hours = 4, Label = "S" },
                new() { Points = 3, Hours = 8, Label = "M" }
            }
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.UpdateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<AppSettingsDto>();
    }

    [Fact]
    public async Task Update_WithInvalidDto_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>()
        };

        _serviceMock.Setup(s => s.UpdateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Story point mappings cannot be empty"));

        // Act
        var result = await _controller.Update(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WithNegativeStoryPoints_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateAppSettingsDto
        {
            StoryPointMappings = new List<StoryPointMapping>
            {
                new() { Points = -1, Hours = 2, Label = "Invalid" }
            }
        };

        _serviceMock.Setup(s => s.UpdateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Story point keys must be non-negative"));

        // Act
        var result = await _controller.Update(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static AppSettingsDto CreateDto()
    {
        return new AppSettingsDto
        {
            Id = Guid.NewGuid(),
            StoryPointMappings = new List<StoryPointMapping>
            {
                new() { Points = 1, Hours = 2, Label = "XS" },
                new() { Points = 2, Hours = 4, Label = "S" },
                new() { Points = 3, Hours = 8, Label = "M" },
                new() { Points = 5, Hours = 16, Label = "L" },
                new() { Points = 8, Hours = 24, Label = "XL" }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
