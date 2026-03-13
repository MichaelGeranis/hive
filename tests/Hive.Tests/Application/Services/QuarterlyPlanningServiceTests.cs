using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class QuarterlyPlanningServiceTests
{
    private readonly Mock<IQuarterRepository> _quarterRepoMock;
    private readonly Mock<IInitiativeRepository> _initiativeRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<ISprintGoalRepository> _sprintGoalRepoMock;
    private readonly Mock<IInitiativeDependencyRepository> _dependencyRepoMock;
    private readonly Mock<IInitiativeMemberRepository> _memberRepoMock;
    private readonly Mock<ISprintRepository> _sprintRepoMock;
    private readonly Mock<IDirectReportRepository> _directReportRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILeaveRepository> _leaveRepoMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly Mock<IAppSettingsService> _appSettingsServiceMock;
    private readonly QuarterlyPlanningService _service;

    public QuarterlyPlanningServiceTests()
    {
        _quarterRepoMock = new Mock<IQuarterRepository>();
        _initiativeRepoMock = new Mock<IInitiativeRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _sprintGoalRepoMock = new Mock<ISprintGoalRepository>();
        _dependencyRepoMock = new Mock<IInitiativeDependencyRepository>();
        _memberRepoMock = new Mock<IInitiativeMemberRepository>();
        _sprintRepoMock = new Mock<ISprintRepository>();
        _directReportRepoMock = new Mock<IDirectReportRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _leaveRepoMock = new Mock<ILeaveRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _appSettingsServiceMock = new Mock<IAppSettingsService>();

        _appSettingsServiceMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettingsDto
            {
                TshirtSizeMappings = new List<TshirtSizeMapping>
                {
                    new() { Size = "S", Sprints = 0.5m, Label = "Small" },
                    new() { Size = "M", Sprints = 1m, Label = "Medium" },
                    new() { Size = "L", Sprints = 2m, Label = "Large" },
                    new() { Size = "XL", Sprints = 4m, Label = "Extra Large" }
                }
            });

        _service = new QuarterlyPlanningService(
            _quarterRepoMock.Object,
            _initiativeRepoMock.Object,
            _allocationRepoMock.Object,
            _sprintGoalRepoMock.Object,
            _dependencyRepoMock.Object,
            _memberRepoMock.Object,
            _sprintRepoMock.Object,
            _directReportRepoMock.Object,
            _projectRepoMock.Object,
            _leaveRepoMock.Object,
            _activityServiceMock.Object,
            _appSettingsServiceMock.Object);
    }

    #region Initiative Member Tests

    [Fact]
    public async Task AddInitiativeMemberAsync_WithValidData_ReturnsMemberDto()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Test", "#FFF");
        var directReport = new DirectReport("Alice", "Johnson", "alice@test.com", "Engineer", "Eng", DateTime.UtcNow);

        _initiativeRepoMock.Setup(x => x.GetByIdAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initiative);
        _directReportRepoMock.Setup(x => x.GetByIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _memberRepoMock.Setup(x => x.ExistsAsync(initiative.Id, directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _memberRepoMock.Setup(x => x.AddAsync(It.IsAny<InitiativeMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InitiativeMember m, CancellationToken _) => m);

        var dto = new CreateInitiativeMemberDto { DirectReportId = directReport.Id };

        // Act
        var result = await _service.AddInitiativeMemberAsync(initiative.Id, dto);

        // Assert
        result.InitiativeId.Should().Be(initiative.Id);
        result.DirectReportId.Should().Be(directReport.Id);
        result.DirectReportName.Should().Be(directReport.FullName);
    }

    [Fact]
    public async Task AddInitiativeMemberAsync_WhenAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Test", "#FFF");
        var directReport = new DirectReport("Alice", "Johnson", "alice@test.com", "Engineer", "Eng", DateTime.UtcNow);

        _initiativeRepoMock.Setup(x => x.GetByIdAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initiative);
        _directReportRepoMock.Setup(x => x.GetByIdAsync(directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(directReport);
        _memberRepoMock.Setup(x => x.ExistsAsync(initiative.Id, directReport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateInitiativeMemberDto { DirectReportId = directReport.Id };

        // Act
        var act = () => _service.AddInitiativeMemberAsync(initiative.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AddInitiativeMemberAsync_WhenInitiativeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _initiativeRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Initiative?)null);

        var dto = new CreateInitiativeMemberDto { DirectReportId = Guid.NewGuid() };

        // Act
        var act = () => _service.AddInitiativeMemberAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveInitiativeMemberAsync_WhenExists_DeletesMember()
    {
        // Arrange
        var member = new InitiativeMember(Guid.NewGuid(), Guid.NewGuid());
        _memberRepoMock.Setup(x => x.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Act
        await _service.RemoveInitiativeMemberAsync(member.Id);

        // Assert
        _memberRepoMock.Verify(x => x.DeleteAsync(member.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveInitiativeMemberAsync_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _memberRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InitiativeMember?)null);

        // Act
        var act = () => _service.RemoveInitiativeMemberAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Sprint Assignment Tests

    [Fact]
    public async Task AssignInitiativeToSprintAsync_WithValidSprint_AssignsAndReturnsDto()
    {
        // Arrange
        var initiative = new Initiative(Guid.NewGuid(), "Test", "#FFF");
        var sprint = new Sprint("Sprint 1");

        _initiativeRepoMock.Setup(x => x.GetByIdAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initiative);
        _sprintRepoMock.Setup(x => x.GetByIdAsync(sprint.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _allocationRepoMock.Setup(x => x.GetByInitiativeAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation>());
        _memberRepoMock.Setup(x => x.GetByInitiativeAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InitiativeMember>());
        _directReportRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.AssignInitiativeToSprintAsync(initiative.Id, sprint.Id);

        // Assert
        result.StartSprintId.Should().Be(sprint.Id);
        result.SprintSpan.Should().Be(1); // M size = 1 sprint
        _initiativeRepoMock.Verify(x => x.UpdateAsync(initiative, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignInitiativeToSprintAsync_WithNull_ClearsAssignment()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var initiative = new Initiative(Guid.NewGuid(), "Test", "#FFF", startSprintId: sprintId);

        _initiativeRepoMock.Setup(x => x.GetByIdAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initiative);
        _allocationRepoMock.Setup(x => x.GetByInitiativeAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation>());
        _memberRepoMock.Setup(x => x.GetByInitiativeAsync(initiative.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InitiativeMember>());
        _directReportRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.AssignInitiativeToSprintAsync(initiative.Id, null);

        // Assert
        result.StartSprintId.Should().BeNull();
    }

    [Fact]
    public async Task AssignInitiativeToSprintAsync_WhenInitiativeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _initiativeRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Initiative?)null);

        // Act
        var act = () => _service.AssignInitiativeToSprintAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Create Initiative with WorkType Tests

    [Fact]
    public async Task CreateInitiativeAsync_WithWorkType_SetsWorkType()
    {
        // Arrange
        var quarter = new Quarter(2026, 1);
        _quarterRepoMock.Setup(x => x.GetByIdAsync(quarter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quarter);
        _initiativeRepoMock.Setup(x => x.GetByQuarterAsync(quarter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Initiative>());
        _initiativeRepoMock.Setup(x => x.AddAsync(It.IsAny<Initiative>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Initiative i, CancellationToken _) => i);

        var dto = new CreateInitiativeDto
        {
            QuarterId = quarter.Id,
            Name = "Test Initiative",
            WorkType = 2 // TechRoadmap
        };

        // Act
        var result = await _service.CreateInitiativeAsync(dto);

        // Assert
        result.WorkType.Should().Be(2);
        result.Name.Should().Be("Test Initiative");
    }

    #endregion
}
