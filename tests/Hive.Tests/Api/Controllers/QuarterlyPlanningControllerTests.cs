using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

public class QuarterlyPlanningControllerTests
{
    private readonly Mock<IQuarterlyPlanningService> _serviceMock;
    private readonly Mock<IQuarterlyPlanningInsightsService> _insightsServiceMock;
    private readonly Mock<ILogger<QuarterlyPlanningController>> _loggerMock;
    private readonly QuarterlyPlanningController _controller;

    public QuarterlyPlanningControllerTests()
    {
        _serviceMock = new Mock<IQuarterlyPlanningService>();
        _insightsServiceMock = new Mock<IQuarterlyPlanningInsightsService>();
        _loggerMock = new Mock<ILogger<QuarterlyPlanningController>>();
        _controller = new QuarterlyPlanningController(
            _serviceMock.Object,
            _insightsServiceMock.Object,
            _loggerMock.Object);
    }

    #region Initiative Member Endpoint Tests

    [Fact]
    public async Task GetInitiativeMembers_ReturnsOkWithMembers()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var members = new List<InitiativeMemberDto>
        {
            new() { Id = Guid.NewGuid(), InitiativeId = initiativeId, DirectReportId = Guid.NewGuid(), DirectReportName = "Alice" },
            new() { Id = Guid.NewGuid(), InitiativeId = initiativeId, DirectReportId = Guid.NewGuid(), DirectReportName = "Bob" }
        };

        _serviceMock.Setup(s => s.GetInitiativeMembersByInitiativeAsync(initiativeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        // Act
        var result = await _controller.GetInitiativeMembers(initiativeId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var resultMembers = okResult.Value as IEnumerable<InitiativeMemberDto>;
        resultMembers.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddInitiativeMember_ReturnsCreated()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var dto = new CreateInitiativeMemberDto { DirectReportId = Guid.NewGuid() };
        var created = new InitiativeMemberDto
        {
            Id = Guid.NewGuid(),
            InitiativeId = initiativeId,
            DirectReportId = dto.DirectReportId,
            DirectReportName = "Alice"
        };

        _serviceMock.Setup(s => s.AddInitiativeMemberAsync(initiativeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.AddInitiativeMember(initiativeId, dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task AddInitiativeMember_WhenConflict_ReturnsConflict()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var dto = new CreateInitiativeMemberDto { DirectReportId = Guid.NewGuid() };

        _serviceMock.Setup(s => s.AddInitiativeMemberAsync(initiativeId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Already exists"));

        // Act
        var result = await _controller.AddInitiativeMember(initiativeId, dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddInitiativeMember_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var dto = new CreateInitiativeMemberDto { DirectReportId = Guid.NewGuid() };

        _serviceMock.Setup(s => s.AddInitiativeMemberAsync(initiativeId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Initiative", initiativeId));

        // Act
        var result = await _controller.AddInitiativeMember(initiativeId, dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveInitiativeMember_ReturnsNoContent()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act
        var result = await _controller.RemoveInitiativeMember(memberId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveInitiativeMember_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveInitiativeMemberAsync(memberId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("InitiativeMember", memberId));

        // Act
        var result = await _controller.RemoveInitiativeMember(memberId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Assign Sprint Endpoint Tests

    [Fact]
    public async Task AssignInitiativeToSprint_ReturnsOk()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();
        var dto = new AssignSprintDto { StartSprintId = sprintId };
        var updated = new InitiativeDto
        {
            Id = initiativeId,
            Name = "Test",
            StartSprintId = sprintId,
            SprintSpan = 2
        };

        _serviceMock.Setup(s => s.AssignInitiativeToSprintAsync(initiativeId, sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _controller.AssignInitiativeToSprint(initiativeId, dto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var resultDto = okResult.Value as InitiativeDto;
        resultDto!.StartSprintId.Should().Be(sprintId);
    }

    [Fact]
    public async Task AssignInitiativeToSprint_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var initiativeId = Guid.NewGuid();
        var dto = new AssignSprintDto { StartSprintId = Guid.NewGuid() };

        _serviceMock.Setup(s => s.AssignInitiativeToSprintAsync(initiativeId, dto.StartSprintId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Initiative", initiativeId));

        // Act
        var result = await _controller.AssignInitiativeToSprint(initiativeId, dto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
