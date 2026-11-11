using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for GoogleCalendarController.
/// </summary>
public class GoogleCalendarControllerTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 30);

    private readonly Mock<IGoogleCalendarService> _serviceMock = new();
    private readonly GoogleCalendarController _controller;

    public GoogleCalendarControllerTests()
    {
        _controller = new GoogleCalendarController(_serviceMock.Object, Mock.Of<ILogger<GoogleCalendarController>>());
    }

    [Fact]
    public async Task GetEvents_OnSuccess_ReturnsOkWithEvents()
    {
        var events = new GoogleCalendarEventsDto { IsConfigured = true };
        _serviceMock.Setup(s => s.GetEventsAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(events);

        var result = await _controller.GetEvents(From, To, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(events);
    }

    [Fact]
    public async Task GetEvents_WithInvalidRange_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetEventsAsync(From, To, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("A calendar range cannot exceed 100 days."));

        var result = await _controller.GetEvents(From, To, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Sync_OnSuccess_ReturnsOkWithEvents()
    {
        var events = new GoogleCalendarEventsDto { IsConfigured = true, IsLoaded = true };
        _serviceMock.Setup(s => s.SyncAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(events);

        var result = await _controller.Sync(From, To, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(events);
    }

    [Fact]
    public async Task Sync_WithInvalidRange_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SyncAsync(From, To, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("A calendar range cannot exceed 100 days."));

        var result = await _controller.Sync(From, To, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Sync_WhenFeedFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.SyncAsync(From, To, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleCalendarException("Google Calendar could not be reached."));

        var result = await _controller.Sync(From, To, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }
}
