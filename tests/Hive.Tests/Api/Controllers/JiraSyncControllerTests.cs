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
/// Tests for JiraSyncController.
/// </summary>
public class JiraSyncControllerTests
{
    private readonly Mock<IJiraSyncService> _serviceMock = new();
    private readonly JiraSyncController _controller;

    public JiraSyncControllerTests()
    {
        _controller = new JiraSyncController(_serviceMock.Object, Mock.Of<ILogger<JiraSyncController>>());
    }

    [Fact]
    public async Task SyncProducts_OnSuccess_ReturnsOkWithResult()
    {
        var syncResult = new JiraProductSyncResultDto { JiraComponentCount = 4, UnchangedCount = 4 };
        _serviceMock.Setup(s => s.SyncProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(syncResult);

        var result = await _controller.SyncProducts(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(syncResult);
    }

    [Fact]
    public async Task SyncProducts_WhenNotConfigured_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SyncProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Jira sync is not configured."));

        var result = await _controller.SyncProducts(CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SyncProducts_WhenJiraFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.SyncProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("Jira rejected the credentials."));

        var result = await _controller.SyncProducts(CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task SyncSprintDates_OnSuccess_ReturnsOkWithResult()
    {
        var syncResult = new JiraSprintSyncResultDto { JiraSprintCount = 3, UnchangedCount = 3 };
        _serviceMock.Setup(s => s.SyncSprintDatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(syncResult);

        var result = await _controller.SyncSprintDates(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(syncResult);
    }

    [Fact]
    public async Task SyncSprintDates_WhenNotConfigured_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SyncSprintDatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Jira sync is not configured."));

        var result = await _controller.SyncSprintDates(CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SyncSprintDates_WhenJiraFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.SyncSprintDatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("Jira rejected the credentials."));

        var result = await _controller.SyncSprintDates(CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task SyncAll_ReturnsOkWithEveryPart()
    {
        var syncResult = new JiraSyncAllResultDto { ParentsError = "Jira sync is not configured." };
        _serviceMock.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(syncResult);

        var result = await _controller.SyncAll(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(syncResult);
    }

    [Fact]
    public async Task SyncParents_OnSuccess_ReturnsOk()
    {
        var syncResult = new JiraParentSyncResultDto { JiraParentCount = 46 };
        _serviceMock.Setup(s => s.SyncParentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(syncResult);

        var result = await _controller.SyncParents(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(syncResult);
    }

    [Fact]
    public async Task SyncParents_WhenNotConfigured_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SyncParentsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Jira sync is not configured. Add the Jira space key in Settings."));

        var result = await _controller.SyncParents(CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SyncParents_WhenJiraFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.SyncParentsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("The Jira API token is not allowed to read space MAR."));

        var result = await _controller.SyncParents(CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task SyncTasks_OnSuccess_ReturnsOk()
    {
        var syncResult = new JiraTaskSyncResultDto { JiraTaskCount = 291 };
        _serviceMock.Setup(s => s.SyncTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(syncResult);

        var result = await _controller.SyncTasks(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(syncResult);
    }

    [Fact]
    public async Task SyncTasks_WhenNotConfigured_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SyncTasksAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Jira sync is not configured. Add the Jira space key in Settings."));

        var result = await _controller.SyncTasks(CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SyncTasks_WhenJiraFails_ReturnsBadGateway()
    {
        _serviceMock.Setup(s => s.SyncTasksAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("This Jira site has no Sprint field, so tasks cannot be read by sprint."));

        var result = await _controller.SyncTasks(CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }
}
