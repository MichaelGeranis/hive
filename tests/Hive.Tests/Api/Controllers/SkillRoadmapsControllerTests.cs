using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class SkillRoadmapsControllerTests
{
    private readonly Mock<ISkillRoadmapService> _serviceMock = new();
    private readonly SkillRoadmapsController _controller;

    public SkillRoadmapsControllerTests()
    {
        _controller = new SkillRoadmapsController(_serviceMock.Object, Mock.Of<ILogger<SkillRoadmapsController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithRoadmaps()
    {
        var roadmaps = new List<SkillRoadmapSummaryDto> { new() { Name = "Backend Engineer" } };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roadmaps);

        var result = await _controller.GetAll(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(roadmaps);
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
        var created = new SkillRoadmapDto { Id = Guid.NewGuid(), Name = "Platform" };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<SaveSkillRoadmapDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _controller.Create(new SaveSkillRoadmapDto { Name = "Platform" }, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<SaveSkillRoadmapDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("taken"));

        var result = await _controller.Create(new SaveSkillRoadmapDto { Name = "Platform" }, CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddSkill_WithInvalidInput_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.AddSkillAsync(It.IsAny<Guid>(), It.IsAny<SaveRoadmapSkillDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Skill title cannot be empty."));

        var result = await _controller.AddSkill(Guid.NewGuid(), new SaveRoadmapSkillDto(), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteSkill_WhenMissing_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteSkillAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("RoadmapSkill", Guid.NewGuid()));

        var result = await _controller.DeleteSkill(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddFromLibrary_ReturnsOkWithTheRoadmap()
    {
        var added = new SkillRoadmapDto { Name = "QA Engineer" };
        _serviceMock.Setup(s => s.AddFromLibraryAsync("qa", It.IsAny<CancellationToken>())).ReturnsAsync(added);

        var result = await _controller.AddFromLibrary("qa", CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(added);
    }

    [Fact]
    public async Task AddFromLibrary_WhenAlreadyAdded_ReturnsConflict()
    {
        _serviceMock.Setup(s => s.AddFromLibraryAsync("qa", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("already added"));

        var result = await _controller.AddFromLibrary("qa", CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }
}
