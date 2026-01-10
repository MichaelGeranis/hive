using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Api.Controllers;

public class TeamTasksControllerTests
{
    private readonly Mock<ITeamTaskService> _serviceMock;
    private readonly Mock<ILogger<TeamTasksController>> _loggerMock;
    private readonly TeamTasksController _controller;

    public TeamTasksControllerTests()
    {
        _serviceMock = new Mock<ITeamTaskService>();
        _loggerMock = new Mock<ILogger<TeamTasksController>>();
        _controller = new TeamTasksController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedTasks()
    {
        // Arrange
        var tasks = new List<TeamTaskDto> { CreateDto(), CreateDto("Task 2") };
        var pagedResult = new PagedResult<TeamTaskDto>
        {
            Items = tasks,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20
        };
        _serviceMock.Setup(s => s.GetAllPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<PagedResult<TeamTaskDto>>();
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithTask()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<TeamTaskDto>();
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTaskDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByAssignee_ReturnsOkWithTasks()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByAssigneeIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByAssignee(assigneeId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByProject_ReturnsOkWithTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByProject(projectId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_ReturnsOkWithTasks()
    {
        // Arrange
        var status = TaskStatus.InProgress;
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByStatus(status, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByPriority_ReturnsOkWithTasks()
    {
        // Arrange
        var priority = TaskPriority.High;
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetByPriorityAsync(priority, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetByPriority(priority, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOverdue_ReturnsOkWithOverdueTasks()
    {
        // Arrange
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetOverdue(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUnassigned_ReturnsOkWithUnassignedTasks()
    {
        // Arrange
        var tasks = new List<TeamTaskDto> { CreateDto() };
        _serviceMock.Setup(s => s.GetUnassignedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetUnassigned(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummary()
    {
        // Arrange
        var summary = new TaskSummaryDto { TotalTasks = 10 };
        _serviceMock.Setup(s => s.GetSummaryAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSummary(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<TaskSummaryDto>();
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateTeamTaskDto { Title = "New Task" };
        var resultDto = CreateDto("New Task");
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Create_WithNonExistentAssignee_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateTeamTaskDto { Title = "Task", AssigneeId = Guid.NewGuid() };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("DirectReport", createDto.AssigneeId.Value));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // DISABLED: Tasks are read-only. Update endpoints are disabled.
    /*
    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedTask()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateTeamTaskDto { Title = "Updated" };
        var resultDto = CreateDto("Updated");
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Update(id, updateDto, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Assign_WithValidAssignee_ReturnsOkWithUpdatedTask()
    {
        var id = Guid.NewGuid();
        var assignDto = new AssignTaskDto { AssigneeId = Guid.NewGuid() };
        var resultDto = CreateDto();
        _serviceMock.Setup(s => s.AssignAsync(id, assignDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Assign(id, assignDto, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Start_ReturnsOkWithUpdatedTask()
    {
        var id = Guid.NewGuid();
        var resultDto = CreateDto() with { Status = TaskStatus.InProgress };
        _serviceMock.Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Start(id, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Start_WhenInvalidState_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.StartAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot start"));
        var result = await _controller.Start(id, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task MoveToReview_ReturnsOkWithUpdatedTask()
    {
        var id = Guid.NewGuid();
        var resultDto = CreateDto() with { Status = TaskStatus.InReview };
        _serviceMock.Setup(s => s.MoveToReviewAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.MoveToReview(id, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Complete_ReturnsOkWithCompletedTask()
    {
        var id = Guid.NewGuid();
        var resultDto = CreateDto() with { Status = TaskStatus.Done };
        _serviceMock.Setup(s => s.CompleteAsync(id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Complete(id, null, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_ReturnsOkWithCancelledTask()
    {
        var id = Guid.NewGuid();
        var resultDto = CreateDto() with { Status = TaskStatus.Cancelled };
        _serviceMock.Setup(s => s.CancelAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Cancel(id, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reopen_ReturnsOkWithReopenedTask()
    {
        var id = Guid.NewGuid();
        var resultDto = CreateDto() with { Status = TaskStatus.Todo };
        _serviceMock.Setup(s => s.ReopenAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.Reopen(id, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LogHours_ReturnsOkWithUpdatedTask()
    {
        var id = Guid.NewGuid();
        var logDto = new LogHoursDto { Hours = 5 };
        var resultDto = CreateDto();
        _serviceMock.Setup(s => s.LogHoursAsync(id, logDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);
        var result = await _controller.LogHours(id, logDto, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
    */

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
            .ThrowsAsync(new NotFoundException("TeamTask", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteMany_WithValidIds_ReturnsNoContent()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _serviceMock.Setup(s => s.DeleteManyAsync(ids, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteMany(ids, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteMany_WithEmptyList_ReturnsBadRequest()
    {
        // Arrange
        var ids = new List<Guid>();

        // Act
        var result = await _controller.DeleteMany(ids, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteMany_WhenTaskNotFound_ReturnsNotFound()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _serviceMock.Setup(s => s.DeleteManyAsync(ids, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("TeamTask", ids[0]));

        // Act
        var result = await _controller.DeleteMany(ids, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static TeamTaskDto CreateDto(string title = "Test Task")
    {
        return new TeamTaskDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Description",
            Type = TaskType.Task,
            TypeName = "Task",
            Priority = TaskPriority.Medium,
            PriorityName = "Medium",
            Status = TaskStatus.Backlog,
            StatusName = "Backlog",
            CreatedAt = DateTime.UtcNow
        };
    }
}
