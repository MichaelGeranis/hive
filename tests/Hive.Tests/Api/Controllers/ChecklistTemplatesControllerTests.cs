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

public class ChecklistTemplatesControllerTests
{
    private readonly Mock<IChecklistService> _serviceMock;
    private readonly Mock<ILogger<ChecklistTemplatesController>> _loggerMock;
    private readonly ChecklistTemplatesController _controller;

    public ChecklistTemplatesControllerTests()
    {
        _serviceMock = new Mock<IChecklistService>();
        _loggerMock = new Mock<ILogger<ChecklistTemplatesController>>();
        _controller = new ChecklistTemplatesController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAllTemplates()
    {
        // Arrange
        var templates = new List<ChecklistTemplateDto>
        {
            CreateTemplateDto(),
            CreateTemplateDto()
        };
        _serviceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTemplates = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistTemplateDto>>().Subject;
        returnedTemplates.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChecklistTemplateDto>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTemplates = okResult.Value.Should().BeAssignableTo<IEnumerable<ChecklistTemplateDto>>().Subject;
        returnedTemplates.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithTemplate()
    {
        // Arrange
        var dto = CreateTemplateDto();
        _serviceMock.Setup(s => s.GetTemplateByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistTemplateDto>().Subject;
        returnedDto.Id.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetTemplateByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChecklistTemplateDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateChecklistTemplateDto
        {
            Name = "Interview Template",
            Description = "Standard interview checklist",
            Type = ChecklistType.Interview
        };
        var resultDto = CreateTemplateDto();
        _serviceMock.Setup(s => s.CreateTemplateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));
        var returnedDto = createdResult.Value.Should().BeOfType<ChecklistTemplateDto>().Subject;
        returnedDto.Id.Should().Be(resultDto.Id);
    }

    [Fact]
    public async Task Create_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateChecklistTemplateDto
        {
            Name = "Duplicate Template",
            Type = ChecklistType.Interview
        };
        _serviceMock.Setup(s => s.CreateTemplateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Template already exists."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateChecklistTemplateDto
        {
            Name = string.Empty,
            Type = ChecklistType.Interview
        };
        _serviceMock.Setup(s => s.CreateTemplateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WhenExists_ReturnsOkWithUpdatedTemplate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateChecklistTemplateDto
        {
            Name = "Updated Template",
            Description = "Updated description"
        };
        var resultDto = CreateTemplateDto(name: "Updated Template");
        _serviceMock.Setup(s => s.UpdateTemplateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistTemplateDto>().Subject;
        returnedDto.Name.Should().Be("Updated Template");
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateChecklistTemplateDto { Name = "Updated Template" };
        _serviceMock.Setup(s => s.UpdateTemplateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", id));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateChecklistTemplateDto { Name = "Updated Template" };
        _serviceMock.Setup(s => s.UpdateTemplateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot update active template."));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteTemplateAsync(id, It.IsAny<CancellationToken>()))
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
        _serviceMock.Setup(s => s.DeleteTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", id));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WithInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete template with active instances."));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Activate Tests

    [Fact]
    public async Task Activate_WhenExists_ReturnsOkWithUpdatedTemplate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateTemplateDto(isActive: true);
        _serviceMock.Setup(s => s.ActivateTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Activate(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistTemplateDto>().Subject;
        returnedDto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.ActivateTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", id));

        // Act
        var result = await _controller.Activate(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public async Task Deactivate_WhenExists_ReturnsOkWithUpdatedTemplate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = CreateTemplateDto(isActive: false);
        _serviceMock.Setup(s => s.DeactivateTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Deactivate(id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<ChecklistTemplateDto>().Subject;
        returnedDto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeactivateTemplateAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("ChecklistTemplate", id));

        // Act
        var result = await _controller.Deactivate(id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private static ChecklistTemplateDto CreateTemplateDto(
        Guid? id = null,
        string name = "Test Template",
        bool isActive = true)
    {
        return new ChecklistTemplateDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = "Test description",
            Type = ChecklistType.Interview,
            TypeName = "Interview",
            IsActive = isActive,
            ItemCount = 5,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
