using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

public class QuarterlyPlanningControllerTests
{
    private readonly Mock<IQuarterlyPlanningService> _serviceMock = new();
    private readonly Mock<IPlanningBoardService> _boardMock = new();
    private readonly QuarterlyPlanningController _controller;

    public QuarterlyPlanningControllerTests()
    {
        _controller = new QuarterlyPlanningController(
            _serviceMock.Object,
            _boardMock.Object,
            new Mock<ILogger<QuarterlyPlanningController>>().Object);
    }

    [Fact]
    public async Task GetQuarterById_Missing_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetQuarterByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuarterDto?)null);

        var result = await _controller.GetQuarterById(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateQuarter_Valid_ReturnsCreated()
    {
        var dto = new CreateQuarterDto { Year = 2026, QuarterNumber = 4 };
        _serviceMock.Setup(s => s.CreateQuarterAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuarterDto { Id = Guid.NewGuid(), Name = "Q4 2026" });

        var result = await _controller.CreateQuarter(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateQuarter_Duplicate_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.CreateQuarterAsync(It.IsAny<CreateQuarterDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Quarter Q4 2026 already exists."));

        var result = await _controller.CreateQuarter(new CreateQuarterDto(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UpdateQuarter_MixNotHundred_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.UpdateQuarterAsync(It.IsAny<Guid>(), It.IsAny<UpdateQuarterDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("The mix must add up to 100 percent."));

        var result = await _controller.UpdateQuarter(Guid.NewGuid(), new UpdateQuarterDto(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBoard_Existing_ReturnsOkWithBoard()
    {
        var board = new PlanningBoardDto { Quarter = new QuarterDto { Name = "Q4 2026" } };
        _boardMock.Setup(s => s.GetBoardAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(board);

        var result = await _controller.GetBoard(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(board);
    }

    [Fact]
    public async Task GetBoard_MissingQuarter_ReturnsNotFound()
    {
        _boardMock.Setup(s => s.GetBoardAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Quarter", Guid.NewGuid()));

        var result = await _controller.GetBoard(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Draft_PassesApplyToService(bool apply)
    {
        var id = Guid.NewGuid();
        _boardMock.Setup(s => s.DraftAsync(id, apply, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanDraftDto { Applied = apply });

        var result = await _controller.Draft(id, apply, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.As<PlanDraftDto>().Applied.Should().Be(apply);
    }

    [Fact]
    public async Task Commit_CompletedQuarter_ReturnsBadRequest()
    {
        _boardMock.Setup(s => s.CommitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("A completed quarter cannot be committed."));

        var result = await _controller.Commit(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetCells_OverAllocated_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SetCellsAsync(It.IsAny<Guid>(), It.IsAny<SetCellsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("A person's sprint cannot be allocated beyond 100%."));

        var result = await _controller.SetCells(Guid.NewGuid(), new SetCellsDto(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetCells_Valid_ReturnsNoContent()
    {
        var result = await _controller.SetCells(Guid.NewGuid(), new SetCellsDto(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateInitiative_Valid_ReturnsCreatedWithId()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.CreateInitiativeAsync(It.IsAny<CreateInitiativeDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(id);

        var result = await _controller.CreateInitiative(new CreateInitiativeDto { Name = "Euterpe" }, CancellationToken.None);

        result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeEquivalentTo(new { id });
    }

    [Fact]
    public async Task ChangeStatus_ReadyWithOpenIssues_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.ChangeInitiativeStatusAsync(It.IsAny<Guid>(), It.IsAny<ChangeInitiativeStatusDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("An option with open readiness issues cannot be ready."));

        var result = await _controller.ChangeStatus(Guid.NewGuid(), new ChangeInitiativeStatusDto(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddMember_AlreadyOnInitiative_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.AddInitiativeMemberAsync(It.IsAny<Guid>(), It.IsAny<CreateInitiativeMemberDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Already on it."));

        var result = await _controller.AddMember(Guid.NewGuid(), new CreateInitiativeMemberDto(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Export_ReturnsSpreadsheet()
    {
        _boardMock.Setup(s => s.ExportToExcelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 1, 2 });

        var result = await _controller.Export(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
