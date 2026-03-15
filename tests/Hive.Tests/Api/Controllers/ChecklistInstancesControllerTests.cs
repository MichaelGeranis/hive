using FluentAssertions;
using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Api.Controllers;

public class ChecklistInstancesControllerTests
{
    private readonly Mock<IChecklistService> _serviceMock;
    private readonly Mock<ILogger<ChecklistInstancesController>> _loggerMock;
    private readonly ChecklistInstancesController _controller;

    public ChecklistInstancesControllerTests()
    {
        _serviceMock = new Mock<IChecklistService>();
        _loggerMock = new Mock<ILogger<ChecklistInstancesController>>();
        _controller = new ChecklistInstancesController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllInstances()
    {
        // Arrange
        var instances = new List<ChecklistInstanceDto>
        {
            CreateInstanceDto(),
            CreateInstanceDto()
        };
        _serviceMock.Setup(s => s.GetAllInstancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedInstances = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistInstanceDto>>().Subject;
        returnedInstances.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllInstancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistInstanceDto>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedInstances = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistInstanceDto>>().Subject;
        returnedInstances.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithInstance()
    {
        // Arrange
        var dto = CreateInstanceDto();
        _serviceMock.Setup(s => s.GetInstanceByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Id.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetInstanceByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistInstanceDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateInterview Tests

    [Fact]
    public async Task CreateInterview_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateInterviewInstanceDto
        {
            TemplateId = Guid.NewGuid(),
            Title = "Software Engineer Interview",
            CandidateName = "Jane Smith",
            Position = "Senior Developer",
            InterviewDate = DateTime.UtcNow.AddDays(3)
        };
        var resultDto = CreateInstanceDto();
        _serviceMock.Setup(s => s.CreateInterviewInstanceAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.CreateInterview(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Id.Should().Be(resultDto.Id);
    }

    [Fact]
    public async Task CreateInterview_WhenTemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateInterviewInstanceDto
        {
            TemplateId = Guid.NewGuid(),
            Title = "Interview",
            CandidateName = "Jane Smith",
            Position = "Developer",
            InterviewDate = DateTime.UtcNow.AddDays(3)
        };
        _serviceMock.Setup(s => s.CreateInterviewInstanceAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", createDto.TemplateId));

        // Act
        var result = await _controller.CreateInterview(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateInterview_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateInterviewInstanceDto
        {
            TemplateId = Guid.NewGuid(),
            Title = "Interview",
            CandidateName = "Jane Smith",
            Position = "Developer",
            InterviewDate = DateTime.UtcNow.AddDays(3)
        };
        _serviceMock.Setup(s => s.CreateInterviewInstanceAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Template is not active."));

        // Act
        var result = await _controller.CreateInterview(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CreateOnboarding Tests

    [Fact]
    public async Task CreateOnboarding_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateOnboardingInstanceDto
        {
            TemplateId = Guid.NewGuid(),
            Title = "New Hire Onboarding",
            NewHireName = "John Doe",
            StartDate = DateTime.UtcNow.AddDays(7),
            TargetCompletionDate = DateTime.UtcNow.AddDays(37)
        };
        var resultDto = CreateInstanceDto();
        _serviceMock.Setup(s => s.CreateOnboardingInstanceAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.CreateOnboarding(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Id.Should().Be(resultDto.Id);
    }

    [Fact]
    public async Task CreateOnboarding_WhenTemplateNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateOnboardingInstanceDto
        {
            TemplateId = Guid.NewGuid(),
            Title = "Onboarding",
            NewHireName = "John Doe",
            StartDate = DateTime.UtcNow.AddDays(7)
        };
        _serviceMock.Setup(s => s.CreateOnboardingInstanceAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", createDto.TemplateId));

        // Act
        var result = await _controller.CreateOnboarding(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Start Tests

    [Fact]
    public async Task Start_WhenExists_ReturnsOkWithUpdatedInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateInstanceDto(status: ChecklistInstanceStatus.InProgress);
        _serviceMock.Setup(s => s.StartInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Start(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Status.Should().Be(ChecklistInstanceStatus.InProgress);
    }

    [Fact]
    public async Task Start_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.StartInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistInstance", id));

        // Act
        var result = await _controller.Start(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Start_WithInvalidState_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.StartInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Instance is already started."));

        // Act
        var result = await _controller.Start(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Complete Tests

    [Fact]
    public async Task Complete_WhenExists_ReturnsOkWithUpdatedInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateInstanceDto(status: ChecklistInstanceStatus.Completed);
        _serviceMock.Setup(s => s.CompleteInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Complete(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Status.Should().Be(ChecklistInstanceStatus.Completed);
    }

    [Fact]
    public async Task Complete_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.CompleteInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistInstance", id));

        // Act
        var result = await _controller.Complete(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WhenExists_ReturnsOkWithUpdatedInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateInstanceDto(status: ChecklistInstanceStatus.Cancelled);
        _serviceMock.Setup(s => s.CancelInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Cancel(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistInstanceDto>().Subject;
        returnedDto.Status.Should().Be(ChecklistInstanceStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.CancelInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistInstance", id));

        // Act
        var result = await _controller.Cancel(id, CancellationToken.None);

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
        _serviceMock.Setup(s => s.DeleteInstanceAsync(id, It.IsAny<CancellationToken>()))
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
        _serviceMock.Setup(s => s.DeleteInstanceAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistInstance", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static ChecklistInstanceDto CreateInstanceDto(
        Guid? id = null,
        ChecklistInstanceStatus status = ChecklistInstanceStatus.NotStarted)
    {
        return new ChecklistInstanceDto
        {
            Id = id ?? Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            TemplateName = "Test Template",
            Type = ChecklistType.Interview,
            TypeName = "Interview",
            Title = "Test Instance",
            Status = status,
            StatusName = status.ToString(),
            CandidateName = "Test Candidate",
            Position = "Developer",
            InterviewDate = DateTime.UtcNow.AddDays(3),
            Notes = string.Empty,
            TotalItems = 5,
            CompletedItems = 0,
            ProgressPercent = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
