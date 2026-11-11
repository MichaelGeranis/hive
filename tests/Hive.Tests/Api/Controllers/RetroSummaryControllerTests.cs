using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for RetroSummaryController.
/// </summary>
public class RetroSummaryControllerTests
{
    private readonly Mock<IRetroSummaryService> _serviceMock = new();
    private readonly RetroSummaryController _controller;

    public RetroSummaryControllerTests()
    {
        _controller = new RetroSummaryController(_serviceMock.Object, Mock.Of<ILogger<RetroSummaryController>>());
    }

    [Fact]
    public async Task Get_ReturnsTheSummaryWithoutForcingARefresh()
    {
        var summary = new RetroSummaryDto { IsEnabled = true, Summary = "The team shipped." };
        _serviceMock.Setup(s => s.GetAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _controller.Get(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task Refresh_ForcesARefresh()
    {
        var summary = new RetroSummaryDto { IsEnabled = true, Summary = "New reading." };
        _serviceMock.Setup(s => s.GetAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _controller.Refresh(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task Get_WhenTheModelFailed_StillReturnsOkWithTheError()
    {
        var summary = new RetroSummaryDto { IsEnabled = true, Summary = "Old.", Error = "Could not reach Ollama." };
        _serviceMock.Setup(s => s.GetAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _controller.Get(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<RetroSummaryDto>().Which.Error.Should().Be("Could not reach Ollama.");
    }
}
