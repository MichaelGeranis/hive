using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for SentimentAnalysisController.
/// </summary>
public class SentimentAnalysisControllerTests
{
    private readonly Mock<ISentimentAnalysisService> _serviceMock = new();
    private readonly SentimentAnalysisController _controller;

    public SentimentAnalysisControllerTests()
    {
        _controller = new SentimentAnalysisController(_serviceMock.Object, Mock.Of<ILogger<SentimentAnalysisController>>());
    }

    [Fact]
    public async Task GetForDirectReport_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var analysis = new SentimentAnalysisDto { DirectReportId = id };
        _serviceMock.Setup(s => s.GetForDirectReportAsync(id, false, It.IsAny<CancellationToken>())).ReturnsAsync(analysis);

        var result = await _controller.GetForDirectReport(id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(analysis);
    }

    [Fact]
    public async Task GetForDirectReport_WhenNotConfigured_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetForDirectReportAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SentimentAnalysisDto?)null);

        var result = await _controller.GetForDirectReport(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetForDirectReport_WhenClaudeFails_ReturnsBadGatewayWithMessage()
    {
        _serviceMock.Setup(s => s.GetForDirectReportAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClaudeApiException("Claude rejected the API key. Check it in Settings."));

        var result = await _controller.GetForDirectReport(Guid.NewGuid(), CancellationToken.None);

        var error = result.Result.Should().BeOfType<ObjectResult>().Subject;
        error.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        error.Value.Should().BeEquivalentTo(new { message = "Claude rejected the API key. Check it in Settings." });
    }

    [Fact]
    public async Task RefreshForDirectReport_WhenClaudeFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.GetForDirectReportAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClaudeApiException("Claude declined to analyse these notes."));

        var result = await _controller.RefreshForDirectReport(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task GetForDirectReport_WhenOllamaFails_ReturnsBadGatewayWithMessage()
    {
        _serviceMock.Setup(s => s.GetForDirectReportAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaApiException("Could not reach Ollama at http://localhost:11434. Check that Ollama is running."));

        var result = await _controller.GetForDirectReport(Guid.NewGuid(), CancellationToken.None);

        var error = result.Result.Should().BeOfType<ObjectResult>().Subject;
        error.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        error.Value.Should().BeEquivalentTo(new { message = "Could not reach Ollama at http://localhost:11434. Check that Ollama is running." });
    }

    [Fact]
    public async Task TestOllamaConnection_ReturnsTheResult()
    {
        var dto = new TestOllamaConnectionDto { BaseUrl = "http://localhost:11434", Model = "llama3.1:8b" };
        var check = new ApiKeyValidationResultDto { Valid = false, Error = "not installed" };
        _serviceMock.Setup(s => s.TestOllamaConnectionAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(check);

        var result = await _controller.TestOllamaConnection(dto, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(check);
    }
}
