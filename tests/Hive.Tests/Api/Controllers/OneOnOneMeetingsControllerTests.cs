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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllMeetings()
    {
        // Arrange
        var meetings = new List<OneOnOneMeetingDto>
        {
            CreateDto(),
            CreateDto()
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<OneOnOneMeetingDto>>();
        var resultMeetings = okResult.Value as IEnumerable<OneOnOneMeetingDto>;
        resultMeetings.Should().HaveCount(2);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithMeeting()
    {
        // Arrange
        var dto = CreateDto();

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<OneOnOneMeetingDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeetingDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetDetails Tests

    [Fact]
    public async Task GetDetails_WhenExists_ReturnsOkWithDetails()
    {
        // Arrange
        var detailsDto = CreateDetailsDto();

        _serviceMock.Setup(s => s.GetDetailsAsync(detailsDto.Meeting.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailsDto);

        // Act
        var result = await _controller.GetDetails(detailsDto.Meeting.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<OneOnOneMeetingDetailsDto>();
    }

    [Fact]
    public async Task GetDetails_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOnOneMeetingDetailsDto?)null);

        // Act
        var result = await _controller.GetDetails(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByDirectReport Tests

    [Fact]
    public async Task GetByDirectReport_ReturnsOkWithMeetings()
    {
        // Arrange
        var directReportId = Guid.NewGuid();
        var meetings = new List<OneOnOneMeetingDto> { CreateDto() };

        _serviceMock.Setup(s => s.GetByDirectReportIdAsync(directReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meetings);

        // Act
        var result = await _controller.GetByDirectReport(directReportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<OneOnOneMeetingDto>>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateOneOnOneMeetingDto
        {
            DirectReportId = Guid.NewGuid(),
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Agenda = "Discuss progress"
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_WhenDirectReportNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateOneOnOneMeetingDto
        {
            DirectReportId = Guid.NewGuid(),
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("DirectReport", createDto.DirectReportId));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedMeeting()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateOneOnOneMeetingDto
        {
            DirectReportId = Guid.NewGuid(),
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Agenda = "Updated agenda"
        };
        var resultDto = CreateDto();

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateOneOnOneMeetingDto
        {
            DirectReportId = Guid.NewGuid(),
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("OneOnOneMeeting", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("OneOnOneMeeting", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static OneOnOneMeetingDto CreateDto()
    {
        return new OneOnOneMeetingDto
        {
            Id = Guid.NewGuid(),
            DirectReportId = Guid.NewGuid(),
            DirectReportName = "John Doe",
            MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Agenda = "Weekly check-in",
            Location = "Conference Room A",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            NoteCount = 0,
            OpenActionItemCount = 0
        };
    }

    private static OneOnOneMeetingDetailsDto CreateDetailsDto()
    {
        return new OneOnOneMeetingDetailsDto
        {
            Meeting = CreateDto(),
            Notes = new List<MeetingNoteDto>()
        };
    }

    #endregion
}
