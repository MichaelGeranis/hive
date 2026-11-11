using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for TaskReadinessController.
/// </summary>
public class TaskReadinessControllerTests
{
    private readonly Mock<ITaskReadinessService> _serviceMock = new();
    private readonly TaskReadinessController _controller;

    public TaskReadinessControllerTests()
    {
        _controller = new TaskReadinessController(_serviceMock.Object, Mock.Of<ILogger<TaskReadinessController>>());
    }

    [Fact]
    public async Task Get_ReturnsTheOverview()
    {
        var overview = new TaskReadinessOverviewDto { IsEnabled = true, PendingCount = 3 };
        _serviceMock.Setup(s => s.GetOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(overview);

        var result = await _controller.Get(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(overview);
    }

    [Fact]
    public async Task ReviewNext_ReturnsTheOverviewAfterTheReview()
    {
        var overview = new TaskReadinessOverviewDto { IsEnabled = true, PendingCount = 2 };
        _serviceMock.Setup(s => s.ReviewNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(overview);

        var result = await _controller.ReviewNext(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(overview);
    }

    [Fact]
    public async Task ReviewTask_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var overview = new TaskReadinessOverviewDto { IsEnabled = true };
        _serviceMock.Setup(s => s.ReviewTaskAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(overview);

        var result = await _controller.ReviewTask(id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(overview);
    }

    [Fact]
    public async Task ReviewTask_UnknownTask_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.ReviewTaskAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskReadinessOverviewDto?)null);

        var result = await _controller.ReviewTask(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
