using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for OneOnOneMeetingsController.
/// </summary>
public class OneOnOneMeetingsControllerTests
{
    private readonly Mock<IOneOnOneMeetingService> _serviceMock;
    private readonly Mock<ILogger<OneOnOneMeetingsController>> _loggerMock;
    private readonly OneOnOneMeetingsController _controller;

    public OneOnOneMeetingsControllerTests()
    {
        _serviceMock = new Mock<IOneOnOneMeetingService>();
        _loggerMock = new Mock<ILogger<OneOnOneMeetingsController>>();
        _controller = new OneOnOneMeetingsController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedMeetings()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetFilteredPagedAsync(It.IsAny<MeetingPaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<OneOnOneMeetingDto> { Items = new[] { CreateDto() }, TotalCount = 1 });

        // Act
        var result = await _controller.GetAll(1, 20, null, false, null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        (okResult.Value as PagedResult<OneOnOneMeetingDto>)!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAll_PassesTheFiltersToTheService()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        MeetingPaginationParams? captured = null;
        _serviceMock.Setup(s => s.GetFilteredPagedAsync(It.IsAny<MeetingPaginationParams>(), It.IsAny<CancellationToken>()))
            .Callback<MeetingPaginationParams, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(new PagedResult<OneOnOneMeetingDto>());

        // Act
        await _controller.GetAll(2, 10, reportId, true, "career", CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.DirectReportId.Should().Be(reportId);
        captured.UnlinkedOnly.Should().BeTrue();
        captured.SearchTerm.Should().Be("career");
    }

    [Fact]
    public async Task GetCount_ReturnsHowMany1on1sWereLogged()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);

        // Act
        var result = await _controller.GetCount(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(7);
    }

    [Fact]
    public async Task GetCounts_ReturnsOkWithCountsPerPerson()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeetingCountDto> { new() { DirectReportId = Guid.NewGuid(), Count = 3 } });

        // Act
        var result = await _controller.GetCounts(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeetingDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByDirectReport_ReturnsOkWithTheirMeetings()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByDirectReportIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OneOnOneMeetingDto> { CreateDto() });

        // Act
        var result = await _controller.GetByDirectReport(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateBlank_ReturnsCreated1on1()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.CreateBlankAsync(It.IsAny<CreateBlankMeetingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.CreateBlank(new CreateBlankMeetingDto(), CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task CreateBlank_WithoutABody_StillCreatesA1on1()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateBlankAsync(It.IsAny<CreateBlankMeetingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDto());

        // Act
        var result = await _controller.CreateBlank(null, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateContent_ReturnsOkWithTheSavedNote()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.UpdateContentAsync(It.IsAny<Guid>(), It.IsAny<UpdateMeetingContentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateContent(dto.Id, new UpdateMeetingContentDto { Content = "x" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateContent_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateContentAsync(It.IsAny<Guid>(), It.IsAny<UpdateMeetingContentDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("OneOnOneMeeting", Guid.NewGuid()));

        // Act
        var result = await _controller.UpdateContent(Guid.NewGuid(), new UpdateMeetingContentDto(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTags_ReturnsOkWithTheRelinked1on1()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.UpdateTagsAsync(It.IsAny<Guid>(), It.IsAny<UpdateMeetingTagsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateTags(dto.Id, new UpdateMeetingTagsDto { Tags = "#badredin" }, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateTags_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateTagsAsync(It.IsAny<Guid>(), It.IsAny<UpdateMeetingTagsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("OneOnOneMeeting", Guid.NewGuid()));

        // Act
        var result = await _controller.UpdateTags(Guid.NewGuid(), new UpdateMeetingTagsDto(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateDate_ReturnsOkWithTheMoved1on1()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.UpdateDateAsync(It.IsAny<Guid>(), It.IsAny<UpdateMeetingDateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateDate(
            dto.Id,
            new UpdateMeetingDateDto { MeetingDate = DateOnly.FromDateTime(DateTime.Today) },
            CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("OneOnOneMeeting", Guid.NewGuid()));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static OneOnOneMeetingDto CreateDto()
    {
        return new OneOnOneMeetingDto
        {
            Id = Guid.NewGuid(),
            DirectReportId = Guid.NewGuid(),
            DirectReportName = "Panagiotis Badredin",
            IsUnlinked = false,
            MeetingDate = DateOnly.FromDateTime(DateTime.Today),
            Title = "Weekly sync",
            Content = "Weekly sync\n\n- Went well",
            Tags = "#badredin",
            TagsList = new[] { "#badredin" },
            Snippet = "- Went well",
            CreatedAt = DateTime.UtcNow
        };
    }
}
