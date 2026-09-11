using Hive.Api.Controllers;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Hive.Tests.Api.Controllers;

/// <summary>
/// Tests for BackupController.
/// </summary>
public class BackupControllerTests
{
    private readonly Mock<IBackupService> _serviceMock;
    private readonly Mock<ILogger<BackupController>> _loggerMock;
    private readonly BackupController _controller;

    public BackupControllerTests()
    {
        _serviceMock = new Mock<IBackupService>();
        _loggerMock = new Mock<ILogger<BackupController>>();
        _controller = new BackupController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Export Tests

    [Fact]
    public async Task Export_ReturnsOkWithBackupData()
    {
        // Arrange
        var backup = new BackupDto
        {
            ExportedAt = DateTime.UtcNow,
            Version = "1.0",
            DirectReports = new List<DirectReportBackup>(),
            Projects = new List<ProjectBackup>(),
            Tasks = new List<TeamTaskBackup>(),
            Leaves = new List<LeaveBackup>(),
            PerformanceReviews = new List<PerformanceReviewBackup>(),
            Meetings = new List<OneOnOneMeetingBackup>(),
            MeetingNotes = new List<MeetingNoteBackup>(),
            ManagerNotes = new List<ManagerNoteBackup>(),
            Sprints = new List<SprintBackup>(),
            SprintCapacities = new List<SprintCapacityBackup>(),
            Settings = null
        };

        _serviceMock.Setup(s => s.ExportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(backup);

        // Act
        var result = await _controller.Export(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<BackupDto>();
        var backupResult = okResult.Value as BackupDto;
        backupResult.Should().NotBeNull();
    }

    #endregion

    #region Import Tests

    [Fact]
    public async Task Import_WithValidBackup_ReturnsOkWithResult()
    {
        // Arrange
        var backup = new BackupDto
        {
            ExportedAt = DateTime.UtcNow,
            Version = "1.0",
            DirectReports = new List<DirectReportBackup>(),
            Projects = new List<ProjectBackup>(),
            Tasks = new List<TeamTaskBackup>(),
            Leaves = new List<LeaveBackup>(),
            PerformanceReviews = new List<PerformanceReviewBackup>(),
            Meetings = new List<OneOnOneMeetingBackup>(),
            MeetingNotes = new List<MeetingNoteBackup>(),
            ManagerNotes = new List<ManagerNoteBackup>(),
            Sprints = new List<SprintBackup>(),
            SprintCapacities = new List<SprintCapacityBackup>(),
            Settings = null
        };

        var restoreResult = new RestoreResultDto
        {
            Success = true,
            DirectReportsRestored = 0,
            ProjectsRestored = 0,
            TasksRestored = 0,
            PerformanceReviewsRestored = 0,
            MeetingsRestored = 0,
            MeetingNotesRestored = 0,
            LeavesRestored = 0,
            ManagerNotesRestored = 0,
            SprintsRestored = 0,
            SprintCapacitiesRestored = 0,
            DocumentsRestored = 0,
            SettingsRestored = false,
            Errors = new List<string>(),
            Warnings = new List<string>()
        };

        _serviceMock.Setup(s => s.ImportAsync(backup, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restoreResult);

        // Act
        var result = await _controller.Import(backup, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<RestoreResultDto>();
        var resultDto = okResult.Value as RestoreResultDto;
        resultDto!.Success.Should().BeTrue();
    }

    #endregion
}
