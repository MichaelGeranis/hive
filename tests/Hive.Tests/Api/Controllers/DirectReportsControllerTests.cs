using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hive.Tests.Api.Controllers;

public class DirectReportsControllerTests
{
    private readonly Mock<IDirectReportService> _serviceMock;
    private readonly Mock<ILogger<DirectReportsController>> _loggerMock;
    private readonly DirectReportsController _controller;

    public DirectReportsControllerTests()
    {
        _serviceMock = new Mock<IDirectReportService>();
        _loggerMock = new Mock<ILogger<DirectReportsController>>();
        _controller = new DirectReportsController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAllReports()
    {
        // Arrange
        var reports = new List<DirectReportDto>
        {
            CreateDto("John", "Doe"),
            CreateDto("Jane", "Smith")
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReports = okResult.Value.Should().BeAssignableTo<IEnumerable<DirectReportDto>>().Subject;
        returnedReports.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithReport()
    {
        // Arrange
        var dto = CreateDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<DirectReportDto>().Subject;
        returnedDto.Id.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReportDto?)null);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateDirectReportDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            JobTitle = "Dev",
            Department = "Eng",
            HireDate = DateTime.UtcNow
        };
        var resultDto = CreateDto("John", "Doe");
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<DirectReportDto>().Subject;
        returnedDto.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateDirectReportDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@test.com",
            HireDate = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Email already exists"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateDirectReportDto { FirstName = "", HireDate = DateTime.UtcNow };
        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid data"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedReport()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateDirectReportDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@test.com",
            HireDate = DateTime.UtcNow
        };
        var resultDto = CreateDto("Updated", "Name");
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<DirectReportDto>().Subject;
        returnedDto.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateDirectReportDto { FirstName = "Test", LastName = "User", Email = "test@test.com", HireDate = DateTime.UtcNow };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("DirectReport", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateDirectReportDto { FirstName = "Test", LastName = "User", Email = "existing@test.com", HireDate = DateTime.UtcNow };
        _serviceMock.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Email already exists"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

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
            .ThrowsAsync(new NotFoundException("DirectReport", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static DirectReportDto CreateDto(string firstName = "John", string lastName = "Doe")
    {
        return new DirectReportDto
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}",
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            JobTitle = "Engineer",
            Department = "Engineering",
            HireDate = DateTime.UtcNow.AddYears(-1),
            CreatedAt = DateTime.UtcNow
        };
    }
}
