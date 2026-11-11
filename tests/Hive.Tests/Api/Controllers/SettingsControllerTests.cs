using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            TshirtSizeMappings = new List<TshirtSizeMapping>
            {
                new() { Size = "S", Sprints = 0.5m, Label = "S" },
                new() { Size = "M", Sprints = 1, Label = "M" }
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
            JiraBaseUrl = "not a url"
        };

        _serviceMock.Setup(s => s.UpdateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Jira base URL must be a valid URL"));

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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    #endregion
}
