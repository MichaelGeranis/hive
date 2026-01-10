using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for JiraImportController.
/// </summary>
public class JiraImportControllerTests
{
    private readonly Mock<IJiraImportService> _serviceMock;
    private readonly Mock<ILogger<JiraImportController>> _loggerMock;
    private readonly JiraImportController _controller;

    public JiraImportControllerTests()
    {
        _serviceMock = new Mock<IJiraImportService>();
        _loggerMock = new Mock<ILogger<JiraImportController>>();
        _controller = new JiraImportController(_serviceMock.Object, _loggerMock.Object);
    }

    #region PreviewImport Tests

    [Fact]
    public async Task PreviewImport_WithValidCsv_ReturnsOkWithPreview()
    {
        // Arrange
        var csvContent = "Issue key,Summary,Status\nPROJ-123,Task 1,Done";
        var request = new CsvContentDto { CsvContent = csvContent };

        var preview = new JiraImportPreviewDto
        {
            TotalRows = 1,
            ValidRows = 1,
            InvalidRows = 0,
            DetectedColumns = new List<string> { "Issue key", "Summary", "Status" },
            MappingWarnings = new List<string>(),
            SampleRows = new List<JiraImportPreviewRowDto>
            {
                new() { RowNumber = 1, IssueKey = "PROJ-123", Summary = "Task 1", Status = "Done", IsValid = true }
            }
        };

        _serviceMock.Setup(s => s.PreviewImportAsync(csvContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);

        // Act
        var result = await _controller.PreviewImport(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<JiraImportPreviewDto>();
        var previewResult = okResult.Value as JiraImportPreviewDto;
        previewResult!.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task PreviewImport_WithEmptyCsv_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvContentDto { CsvContent = "" };

        // Act
        var result = await _controller.PreviewImport(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PreviewImport_WhenServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvContentDto { CsvContent = "invalid csv" };

        _serviceMock.Setup(s => s.PreviewImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("CSV parsing error"));

        // Act
        var result = await _controller.PreviewImport(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Import Tests

    [Fact]
    public async Task Import_WithValidRequest_ReturnsOkWithResult()
    {
        // Arrange
        var request = new JiraImportRequestDto
        {
            CsvContent = "Issue key,Summary,Status\nPROJ-123,Task 1,Done",
            UpdateExisting = false,
            MatchField = "IssueKey"
        };

        var importResult = new JiraImportResultDto
        {
            TotalRows = 1,
            SuccessCount = 1,
            ErrorCount = 0,
            SkippedCount = 0,
            ImportedTasks = new List<JiraImportedTaskDto>(),
            Errors = new List<string>(),
            Warnings = new List<string>()
        };

        _serviceMock.Setup(s => s.ImportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.Import(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<JiraImportResultDto>();
        var resultDto = okResult.Value as JiraImportResultDto;
        resultDto!.SuccessCount.Should().Be(1);
        resultDto.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task Import_WithEmptyCsv_ReturnsBadRequest()
    {
        // Arrange
        var request = new JiraImportRequestDto { CsvContent = "" };

        // Act
        var result = await _controller.Import(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Import_WhenServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var request = new JiraImportRequestDto
        {
            CsvContent = "invalid csv",
            UpdateExisting = false
        };

        _serviceMock.Setup(s => s.ImportAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Import error"));

        // Act
        var result = await _controller.Import(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Import_WithPartialSuccess_ReturnsOkWithErrors()
    {
        // Arrange
        var request = new JiraImportRequestDto
        {
            CsvContent = "Issue key,Summary,Status\nPROJ-123,Task 1,Done\nPROJ-124,Task 2,Invalid",
            UpdateExisting = false
        };

        var importResult = new JiraImportResultDto
        {
            TotalRows = 2,
            SuccessCount = 1,
            ErrorCount = 1,
            SkippedCount = 0,
            ImportedTasks = new List<JiraImportedTaskDto>(),
            Errors = new List<string> { "Row 2: Invalid status" },
            Warnings = new List<string>()
        };

        _serviceMock.Setup(s => s.ImportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.Import(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var resultDto = okResult.Value as JiraImportResultDto;
        resultDto!.SuccessCount.Should().Be(1);
        resultDto.ErrorCount.Should().Be(1);
        resultDto.Errors.Should().HaveCount(1);
    }

    #endregion
}
