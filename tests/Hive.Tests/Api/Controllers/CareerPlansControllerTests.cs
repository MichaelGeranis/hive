using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class CareerPlansControllerTests
{
    private readonly Mock<ICareerPlanService> _serviceMock = new();
    private readonly CareerPlansController _controller;

    public CareerPlansControllerTests()
    {
        _controller = new CareerPlansController(_serviceMock.Object, Mock.Of<ILogger<CareerPlansController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPlans()
    {
        var plans = new List<CareerPlanSummaryDto> { new() { DirectReportName = "Alice Johnson" } };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plans);

        var result = await _controller.GetAll(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(plans);
    }

    [Fact]
    public async Task GetByDirectReport_WhenNone_ReturnsNotFound()
    {
        var result = await _controller.GetByDirectReport(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetSkillFocus_ReturnsThePlan()
    {
        var plan = new CareerPlanDto { WorkingOnCount = 1 };
        _serviceMock.Setup(s => s.SetSkillFocusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SetSkillFocusDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _controller.SetSkillFocus(Guid.NewGuid(), Guid.NewGuid(), new SetSkillFocusDto { IsFocus = true }, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(plan);
    }

    [Fact]
    public async Task GetOverview_ReturnsOk()
    {
        var overview = new CareerOverviewDto();
        _serviceMock.Setup(s => s.GetOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(overview);

        var result = await _controller.GetOverview(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(overview);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var created = new CareerPlanDto { Id = Guid.NewGuid() };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateCareerPlanDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _controller.Create(new CreateCareerPlanDto(), CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().Be(created);
    }

    [Fact]
    public async Task SetSkillLevel_OutOfRange_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.SetSkillLevelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SetSkillLevelDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Skill level must be between 0 and 5."));

        var result = await _controller.SetSkillLevel(Guid.NewGuid(), Guid.NewGuid(), new SetSkillLevelDto { Level = 9 }, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenThePersonHasAPlan_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateCareerPlanDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("already active"));

        var result = await _controller.Create(new CreateCareerPlanDto(), CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_UnknownRoadmap_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateCareerPlanDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("SkillRoadmap", Guid.NewGuid()));

        var result = await _controller.Create(new CreateCareerPlanDto(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetSkillLevel_ReturnsTheChange()
    {
        var change = new SkillLevelChangeDto { SkillLevelledUp = true };
        _serviceMock.Setup(s => s.SetSkillLevelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<SetSkillLevelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(change);

        var result = await _controller.SetSkillLevel(Guid.NewGuid(), Guid.NewGuid(), new SetSkillLevelDto { Level = 2 }, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(change);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("CareerPlan", Guid.NewGuid()));

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
