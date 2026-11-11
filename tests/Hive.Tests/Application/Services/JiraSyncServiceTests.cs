using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for JiraSyncService.
/// </summary>
public class JiraSyncServiceTests
{
    // Midday UTC lands on the same calendar day in every inhabited time zone, so these tests
    // do not depend on where they run.
    private static readonly DateTimeOffset Oct6 = new(2025, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Oct17 = new(2025, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock = new();
    private readonly Mock<ISprintRepository> _sprintRepositoryMock = new();
    private readonly Mock<ISprintService> _sprintServiceMock = new();
    private readonly Mock<IParentRepository> _parentRepositoryMock = new();
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock = new();
    private readonly Mock<IActivityService> _activityServiceMock = new();
    private readonly Mock<IJiraApiClient> _jiraApiClientMock = new();
    private readonly Mock<ITeamTaskService> _taskServiceMock = new();
    private readonly Mock<IParentService> _parentServiceMock = new();
    private readonly Mock<IDirectReportRepository> _directReportRepositoryMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IProjectProductRepository> _projectProductRepositoryMock = new();
    private readonly JiraSyncService _service;

    public JiraSyncServiceTests()
    {
        _service = new JiraSyncService(
            _settingsRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _sprintServiceMock.Object,
            _parentRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _activityServiceMock.Object,
            _jiraApiClientMock.Object,
            _taskServiceMock.Object,
            _parentServiceMock.Object,
            _directReportRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _productRepositoryMock.Object,
            _projectProductRepositoryMock.Object);

        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());
        _jiraApiClientMock
            .Setup(c => c.GetComponentsAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JiraComponent>());

        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Parent>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DirectReport>());
        _projectRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project>());
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamTask>());
        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamTask t, CancellationToken _) => t);
        _jiraApiClientMock
            .Setup(c => c.GetSprintWorkItemsAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JiraWorkItem>());

        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamTask>());

        _sprintServiceMock
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateSprintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto());
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenNotConfigured_ThrowsNamingWhatIsMissing()
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl("https://acme.atlassian.net/browse");
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var act = () => _service.SyncSprintDatesAsync();

        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("*Jira email, Jira API token, Jira board ID*");
        _jiraApiClientMock.Verify(
            c => c.GetBoardSprintsAsync(It.IsAny<JiraConnection>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenNoSettingsRow_Throws()
    {
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

        var act = () => _service.SyncSprintDatesAsync();

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SyncSprintDatesAsync_CallsJiraWithSiteRootCredentialsAndBoard()
    {
        SetupConfiguredSettings();
        SetupSprints(Array.Empty<Sprint>(), Array.Empty<JiraSprint>());

        await _service.SyncSprintDatesAsync();

        _jiraApiClientMock.Verify(c => c.GetBoardSprintsAsync(
            new JiraConnection("https://acme.atlassian.net", "em@acme.com", "token-123"),
            42,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WithMatchingSprint_UpdatesDatesThroughSprintService()
    {
        SetupConfiguredSettings();
        var sprint = new Sprint("LP_4Q25_S1");
        SetupSprints(new[] { sprint }, new[] { new JiraSprint(1, "LP_4Q25_S1", "active", Oct6, Oct17) });

        var result = await _service.SyncSprintDatesAsync();

        _sprintServiceMock.Verify(s => s.UpdateAsync(
            sprint.Id,
            It.Is<UpdateSprintDto>(d => d.StartDate == new DateTime(2025, 10, 6) && d.EndDate == new DateTime(2025, 10, 17)),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Updated.Should().ContainSingle();
        result.Updated[0].Name.Should().Be("LP_4Q25_S1");
        result.Updated[0].PreviousStartDate.Should().BeNull();
        result.Updated[0].StartDate.Should().Be(new DateTime(2025, 10, 6));
        result.Updated[0].EndDate.Should().Be(new DateTime(2025, 10, 17));
        result.JiraSprintCount.Should().Be(1);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_MatchesNamesCaseInsensitively()
    {
        SetupConfiguredSettings();
        var sprint = new Sprint("LP_4Q25_S1");
        SetupSprints(new[] { sprint }, new[] { new JiraSprint(1, " lp_4q25_s1 ", "closed", Oct6, Oct17) });

        var result = await _service.SyncSprintDatesAsync();

        result.Updated.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenDatesAlreadyMatch_CountsUnchangedAndDoesNotUpdate()
    {
        SetupConfiguredSettings();
        var sprint = new Sprint("LP_4Q25_S1");
        sprint.UpdateDates(new DateTime(2025, 10, 6), new DateTime(2025, 10, 17));
        SetupSprints(new[] { sprint }, new[] { new JiraSprint(1, "LP_4Q25_S1", "closed", Oct6, Oct17) });

        var result = await _service.SyncSprintDatesAsync();

        result.UnchangedCount.Should().Be(1);
        result.Updated.Should().BeEmpty();
        _sprintServiceMock.Verify(
            s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateSprintDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_OverwritesManuallyEnteredDates()
    {
        SetupConfiguredSettings();
        var sprint = new Sprint("LP_4Q25_S1");
        sprint.UpdateDates(new DateTime(2025, 10, 1), new DateTime(2025, 10, 14));
        SetupSprints(new[] { sprint }, new[] { new JiraSprint(1, "LP_4Q25_S1", "closed", Oct6, Oct17) });

        var result = await _service.SyncSprintDatesAsync();

        result.Updated.Should().ContainSingle();
        result.Updated[0].PreviousStartDate.Should().Be(new DateTime(2025, 10, 1));
        result.Updated[0].PreviousEndDate.Should().Be(new DateTime(2025, 10, 14));
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenJiraSprintHasNoDates_LeavesSprintUntouched()
    {
        SetupConfiguredSettings();
        SetupSprints(
            new[] { new Sprint("LP_1Q26_S1") },
            new[] { new JiraSprint(1, "LP_1Q26_S1", "future", null, null) });

        var result = await _service.SyncSprintDatesAsync();

        result.MissingDatesInJira.Should().Equal("LP_1Q26_S1");
        result.Updated.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncSprintDatesAsync_ReportsHiveSprintsMissingFromJira()
    {
        SetupConfiguredSettings();
        SetupSprints(new[] { new Sprint("LP_4Q25_S9") }, Array.Empty<JiraSprint>());

        var result = await _service.SyncSprintDatesAsync();

        result.NotFoundInJira.Should().Equal("LP_4Q25_S9");
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenNameAppearsTwiceInJira_DoesNotGuess()
    {
        SetupConfiguredSettings();
        SetupSprints(
            new[] { new Sprint("LP_4Q25_S1") },
            new[]
            {
                new JiraSprint(1, "LP_4Q25_S1", "closed", Oct6, Oct17),
                new JiraSprint(2, "LP_4Q25_S1", "closed", Oct17, Oct17.AddDays(14))
            });

        var result = await _service.SyncSprintDatesAsync();

        result.AmbiguousInJira.Should().Equal("LP_4Q25_S1");
        result.Updated.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenSprintRejectsDates_RecordsErrorAndContinues()
    {
        SetupConfiguredSettings();
        var rejected = new Sprint("LP_4Q25_S1");
        var accepted = new Sprint("LP_4Q25_S2");
        SetupSprints(
            new[] { rejected, accepted },
            new[]
            {
                new JiraSprint(1, "LP_4Q25_S1", "closed", Oct17, Oct6),
                new JiraSprint(2, "LP_4Q25_S2", "closed", Oct6, Oct17)
            });
        _sprintServiceMock
            .Setup(s => s.UpdateAsync(rejected.Id, It.IsAny<UpdateSprintDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Start date cannot be after end date."));

        var result = await _service.SyncSprintDatesAsync();

        result.Errors.Should().ContainSingle().Which.Should().Contain("LP_4Q25_S1");
        result.Updated.Select(u => u.Name).Should().Equal("LP_4Q25_S2");
    }

    [Fact]
    public async Task SyncSprintDatesAsync_CreatesUpcomingTeamSprintsHiveDoesNotHave()
    {
        // Arrange — S5 exists; S6 is future with dates, S7 is future without; an old closed
        // sprint, another team's sprint and a non-team queue are all left alone.
        SetupConfiguredSettings(teamFilter: "LP");
        var existing = new Sprint("LP_4Q25_S5");
        SetupSprints(
            new[] { existing },
            new[]
            {
                new JiraSprint(1, "LP_4Q25_S5", "active", Oct6, Oct17),
                new JiraSprint(2, "LP_4Q25_S6", "future", Oct17.AddDays(3), Oct17.AddDays(14)),
                new JiraSprint(3, "LP_4Q25_S7", "future", null, null),
                new JiraSprint(4, "LP_3Q25_S1", "closed", Oct6.AddDays(-90), Oct6.AddDays(-77)),
                new JiraSprint(5, "XY_4Q25_S6", "future", Oct6, Oct17),
                new JiraSprint(6, "REFINEMENT QUEUE", "future", null, null)
            });
        var s6Id = Guid.NewGuid();
        var s7Id = Guid.NewGuid();
        _sprintRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _sprintServiceMock.Setup(s => s.GetOrCreateAsync("LP_4Q25_S6", It.IsAny<CancellationToken>())).ReturnsAsync(new SprintDto { Id = s6Id });
        _sprintServiceMock.Setup(s => s.GetOrCreateAsync("LP_4Q25_S7", It.IsAny<CancellationToken>())).ReturnsAsync(new SprintDto { Id = s7Id });

        // Act
        var result = await _service.SyncSprintDatesAsync();

        // Assert
        result.Created.Should().Equal("LP_4Q25_S6", "LP_4Q25_S7");
        _sprintServiceMock.Verify(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sprintServiceMock.Verify(s => s.UpdateAsync(
            s6Id,
            It.Is<UpdateSprintDto>(d => d.StartDate == new DateTime(2025, 10, 20) && d.EndDate == new DateTime(2025, 10, 31)),
            It.IsAny<CancellationToken>()), Times.Once);
        // Without dates in Jira, Hive estimates them from the name
        _sprintServiceMock.Verify(s => s.UpdateAsync(s7Id, It.IsAny<UpdateSprintDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_DoesNotCreateAnUpcomingSprintWhoseNameIsDuplicated()
    {
        SetupConfiguredSettings();
        SetupSprints(
            Array.Empty<Sprint>(),
            new[]
            {
                new JiraSprint(1, "LP_1Q26_S1", "future", null, null),
                new JiraSprint(2, "LP_1Q26_S1", "future", null, null)
            });

        var result = await _service.SyncSprintDatesAsync();

        result.Created.Should().BeEmpty();
        _sprintServiceMock.Verify(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSprintDatesAsync_WhenJiraFails_PropagatesJiraApiException()
    {
        SetupConfiguredSettings();
        _jiraApiClientMock
            .Setup(c => c.GetBoardSprintsAsync(It.IsAny<JiraConnection>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("Jira rejected the credentials."));

        var act = () => _service.SyncSprintDatesAsync();

        await act.Should().ThrowAsync<JiraApiException>();
    }

    // ============== SyncParentsAsync ==============

    [Fact]
    public async Task SyncParentsAsync_WithoutSpaceKey_ThrowsNamingIt()
    {
        SetupConfiguredSettings(projectKey: null);

        var act = () => _service.SyncParentsAsync();

        (await act.Should().ThrowAsync<ValidationException>()).WithMessage("*Jira space key*");
    }

    [Fact]
    public async Task SyncParentsAsync_AsksJiraForTheConfiguredSpace()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());

        await _service.SyncParentsAsync();

        _jiraApiClientMock.Verify(c => c.GetParentIssuesAsync(
            new JiraConnection("https://acme.atlassian.net", "em@acme.com", "token-123"), "MAR", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncParentsAsync_CreatesAParentForANewEpic()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments revamp") });

        var result = await _service.SyncParentsAsync();

        _parentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Parent>(p => p.JiraKey == "MAR-1" && p.Name == "Payments revamp"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Created.Should().ContainSingle().Which.JiraKey.Should().Be("MAR-1");
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Created, EntityType.Parent, It.IsAny<Guid>(), "Payments revamp",
            "Parent created from Jira Epic MAR-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncParentsAsync_AdoptsACsvParentByNameAndLinksItToTheEpic()
    {
        SetupConfiguredSettings();
        var csvParent = new Parent("Payments revamp", "payments");
        SetupParents(new[] { csvParent }, new[] { Epic("MAR-1", "Payments revamp") });

        var result = await _service.SyncParentsAsync();

        csvParent.JiraKey.Should().Be("MAR-1");
        csvParent.Labels.Should().Be("payments", "labels are the manager's");
        _parentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Updated.Should().ContainSingle().Which.PreviousName.Should().BeNull();
    }

    [Fact]
    public async Task SyncParentsAsync_LinksAStoryWithSubTasksAndRecordsItsType()
    {
        SetupConfiguredSettings();
        var csvParent = new Parent("Creation and backfill of \"created_at\" field");
        SetupParents(new[] { csvParent }, new[] { Story("MAR-111", "Creation and backfill of \"created_at\" field") });

        var result = await _service.SyncParentsAsync();

        csvParent.JiraKey.Should().Be("MAR-111");
        csvParent.JiraIssueType.Should().Be("Story");
        result.Updated.Should().ContainSingle();
        _activityServiceMock.Verify(a => a.LogActivityAsync(
            ActivityType.Updated, EntityType.Parent, csvParent.Id, csvParent.Name,
            "Parent updated from Jira Story MAR-111", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncParentsAsync_WhenAnEpicsTypeWasNotRecordedYet_UpdatesIt()
    {
        SetupConfiguredSettings();
        // Parents linked before issue types were stored carry the key but no type
        var parent = Parent.FromJira("MAR-1", "Epic", "Payments");
        typeof(Parent).GetProperty(nameof(Parent.JiraIssueType))!.SetValue(parent, null);
        SetupParents(new[] { parent }, new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncParentsAsync();

        parent.JiraIssueType.Should().Be("Epic");
        result.Updated.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncParentsAsync_WhenEpicIsRenamed_RenamesTheLinkedParent()
    {
        SetupConfiguredSettings();
        var parent = Parent.FromJira("MAR-1", "Epic", "Old name");
        SetupParents(new[] { parent }, new[] { Epic("MAR-1", "New name") });

        var result = await _service.SyncParentsAsync();

        parent.Name.Should().Be("New name");
        result.Updated.Should().ContainSingle().Which.PreviousName.Should().Be("Old name");
        _parentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncParentsAsync_WhenNothingChanged_CountsUnchanged()
    {
        SetupConfiguredSettings();
        SetupParents(new[] { Parent.FromJira("MAR-1", "Epic", "Payments") }, new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncParentsAsync();

        result.UnchangedCount.Should().Be(1);
        result.Updated.Should().BeEmpty();
        _parentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncParentsAsync_WhenEpicNameIsTakenByAnotherParent_ReportsItAndCarriesOn()
    {
        SetupConfiguredSettings();
        SetupParents(
            new[] { Parent.FromJira("MAR-1", "Epic", "Maintenance") },
            new[] { Epic("MAR-1", "Maintenance"), Epic("MAR-2", "Maintenance"), Epic("MAR-3", "Onboarding") });

        var result = await _service.SyncParentsAsync();

        result.Errors.Should().ContainSingle().Which.Should().StartWith("MAR-2:").And.Contain("MAR-1");
        result.Created.Select(c => c.JiraKey).Should().Equal("MAR-3");
    }

    [Fact]
    public async Task SyncParentsAsync_LinksTheEpicsOwnImportedTask()
    {
        SetupConfiguredSettings();
        var epicTask = new TeamTask("Payments revamp", description: "", type: TaskType.Epic, priority: TaskPriority.Medium, assigneeId: null, projectId: null, dueDate: null, storyPoints: null, tags: "jira:MAR-1", labels: "", components: null, sprint: null);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamTask> { epicTask });
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments revamp") });

        await _service.SyncParentsAsync();

        _parentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Parent>(p => p.TeamTaskId == epicTask.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncParentsAsync_NeverDeletesParents()
    {
        SetupConfiguredSettings();
        SetupParents(new[] { new Parent("Hand-made parent") }, Array.Empty<JiraIssue>());

        await _service.SyncParentsAsync();

        _parentRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============== SyncAllAsync ==============

    [Fact]
    public async Task SyncAllAsync_RunsEveryPart()
    {
        SetupConfiguredSettings();
        SetupSprints(Array.Empty<Sprint>(), Array.Empty<JiraSprint>());
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncAllAsync();

        result.SprintDates.Should().NotBeNull();
        result.Parents!.Created.Should().ContainSingle();
        result.SprintDatesError.Should().BeNull();
        result.ParentsError.Should().BeNull();
    }

    [Fact]
    public async Task SyncAllAsync_WhenOnePartIsNotConfigured_StillRunsTheOthers()
    {
        SetupConfiguredSettings(boardId: null);
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncAllAsync();

        result.SprintDates.Should().BeNull();
        result.SprintDatesError.Should().Contain("Jira board ID");
        result.Parents!.Created.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncAllAsync_WhenJiraRejectsARequest_ReportsItAsThatPartsError()
    {
        SetupConfiguredSettings();
        SetupSprints(Array.Empty<Sprint>(), Array.Empty<JiraSprint>());
        _jiraApiClientMock
            .Setup(c => c.GetParentIssuesAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JiraApiException("The Jira API token is not allowed to read space MAR."));

        var result = await _service.SyncAllAsync();

        result.SprintDates.Should().NotBeNull();
        result.ParentsError.Should().Be("The Jira API token is not allowed to read space MAR.");
    }

    // ============== Parent removal ==============

    [Fact]
    public async Task SyncParentsAsync_RemovesAParentWhoseIssueIsGoneAndUnlinksItsTasks()
    {
        SetupConfiguredSettings();
        var gone = Parent.FromJira("MAR-9", "Epic", "Deleted epic");
        var handMade = new Parent("Hand-made group");
        SetupParents(new[] { Parent.FromJira("MAR-1", "Epic", "Payments"), gone, handMade }, new[] { Epic("MAR-1", "Payments") });
        var orphan = new TeamTask("Orphan", tags: "jira:MAR-50");
        orphan.AssignToParent(gone.Id);
        _taskRepositoryMock.Setup(r => r.GetByParentIdAsync(gone.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TeamTask> { orphan });

        var result = await _service.SyncParentsAsync();

        result.Removed.Select(r => r.JiraKey).Should().Equal("MAR-9");
        orphan.ParentId.Should().BeNull();
        _parentServiceMock.Verify(s => s.DeleteAsync(gone.Id, It.IsAny<CancellationToken>()), Times.Once);
        _parentServiceMock.Verify(s => s.DeleteAsync(handMade.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncParentsAsync_WhenJiraReturnsNothing_RemovesNothing()
    {
        SetupConfiguredSettings();
        SetupParents(new[] { Parent.FromJira("MAR-1", "Epic", "Payments") }, Array.Empty<JiraIssue>());

        var result = await _service.SyncParentsAsync();

        result.Removed.Should().BeEmpty();
        _parentServiceMock.Verify(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============== SyncTasksAsync ==============

    [Fact]
    public async Task SyncTasksAsync_CreatesATaskMappedLikeTheCsvImport()
    {
        SetupConfiguredSettings();
        var epic = Parent.FromJira("MAR-1", "Epic", "Payments");
        SetupParents(new[] { epic }, new[] { Epic("MAR-1", "Payments") });
        var alice = new DirectReport("Alice", "Smith", "alice@acme.com", "Engineer", "Eng", new DateTime(2020, 1, 1));
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DirectReport> { alice });
        SetupTasks(Array.Empty<TeamTask>(), WorkItem("MAR-10", parent: "MAR-1", sprints: new[] { "MAR_1Q26_S1", "MAR_1Q26_S2" }) with
        {
            Description = "First line\n\nSecond line",
            IssueType = "Story",
            Status = "In Progress",
            Priority = "Highest",
            AssigneeEmail = "ALICE@acme.com",
            Labels = new[] { "payments", " payments", "api" },
            StoryPoints = 2.5
        });

        var result = await _service.SyncTasksAsync();

        result.Created.Should().ContainSingle().Which.JiraKey.Should().Be("MAR-10");
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t =>
            t.Title == "Summary of MAR-10" &&
            t.Description == "First line\n\nSecond line" &&
            t.Type == TaskType.Story &&
            t.Status == TaskStatus.InProgress &&
            t.Priority == TaskPriority.Critical &&
            t.AssigneeId == alice.Id &&
            t.ParentId == epic.Id &&
            t.Tags == "jira:MAR-10" &&
            t.Labels == "payments,api" &&
            t.Sprint == "MAR_1Q26_S1,MAR_1Q26_S2" &&
            t.StoryPoints == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncTasksAsync_LongDescription_IsStoredWhole()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        _directReportRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DirectReport>());
        var description = "## Acceptance criteria\n" + new string('x', 10_000);
        SetupTasks(Array.Empty<TeamTask>(), WorkItem("MAR-11", sprints: new[] { "MAR_1Q26_S1" }) with { Description = description });

        await _service.SyncTasksAsync();

        _taskRepositoryMock.Verify(r => r.AddAsync(
            It.Is<TeamTask>(t => t.Description == description), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncTasksAsync_KeepsOnlyWorkInTheTeamsSprints()
    {
        SetupConfiguredSettings(teamFilter: "MAR");
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        SetupTasks(Array.Empty<TeamTask>(),
            WorkItem("MAR-10", sprints: new[] { "MAR_1Q26_S1" }),
            WorkItem("MAR-11", sprints: new[] { "REFINEMENT QUEUE" }),
            WorkItem("MAR-12", sprints: new[] { "OPS_1Q26_S1" }),
            WorkItem("MAR-13", sprints: new[] { "READY FOR DELIVERY", "MAR_1Q26_S2" }));

        var result = await _service.SyncTasksAsync();

        result.Created.Select(c => c.JiraKey).Should().Equal("MAR-10", "MAR-13");
        result.OutOfScopeCount.Should().Be(2);
        _taskRepositoryMock.Verify(r => r.AddAsync(It.Is<TeamTask>(t => t.Sprint == "MAR_1Q26_S2"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncTasksAsync_UpdatesAnExistingTaskAndDropsTheCsvMarker()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        var existing = new TeamTask("Old title", tags: "jira:MAR-10,imported-from-jira", sprint: "MAR_1Q26_S1");
        SetupTasks(new[] { existing }, WorkItem("MAR-10", sprints: new[] { "MAR_1Q26_S1" }));

        var result = await _service.SyncTasksAsync();

        existing.Title.Should().Be("Summary of MAR-10");
        existing.Tags.Should().Be("jira:MAR-10");
        result.Updated.Should().ContainSingle();
        _taskRepositoryMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncTasksAsync_WhenNothingChanged_DoesNotRewriteTheTask()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        var existing = new TeamTask("Summary of MAR-10", tags: "jira:MAR-10", sprint: "MAR_1Q26_S1");
        SetupTasks(new[] { existing }, WorkItem("MAR-10", sprints: new[] { "MAR_1Q26_S1" }) with { Status = "Backlog" });

        var result = await _service.SyncTasksAsync();

        result.UnchangedCount.Should().Be(1);
        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TeamTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncTasksAsync_KeepsFieldsTheManagerOverrode()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        var existing = new TeamTask("Summary of MAR-10", storyPoints: 8, tags: "jira:MAR-10", sprint: "MAR_1Q26_S1");
        existing.SetOverride("StoryPoints");
        SetupTasks(new[] { existing }, WorkItem("MAR-10", sprints: new[] { "MAR_1Q26_S1" }) with { StoryPoints = 2 });

        await _service.SyncTasksAsync();

        existing.StoryPoints.Should().Be(8);
    }

    [Fact]
    public async Task SyncTasksAsync_ReopensATaskJiraMovedBackFromDone()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        var existing = new TeamTask("Summary of MAR-44", tags: "jira:MAR-44", sprint: "MAR_1Q26_S1");
        existing.Complete();
        SetupTasks(new[] { existing }, WorkItem("MAR-44", sprints: new[] { "MAR_1Q26_S1" }) with { Status = "Ready to Release" });

        var result = await _service.SyncTasksAsync();

        existing.Status.Should().Be(TaskStatus.ReadyToRelease);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncTasksAsync_RemovesTasksNoLongerInTheSprintsAndDuplicateCopies()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        var kept = new TeamTask("Summary of MAR-10", tags: "jira:MAR-10", sprint: "MAR_1Q26_S1");
        var duplicate = new TeamTask("Summary of MAR-10", tags: "jira:MAR-10,imported-from-jira", sprint: "MAR_1Q26_S1");
        typeof(TeamTask).GetProperty(nameof(TeamTask.CreatedAt))!.SetValue(duplicate, kept.CreatedAt.AddSeconds(1));
        var gone = new TeamTask("Moved to backlog", tags: "jira:MAR-20");
        var handMade = new TeamTask("Hand-made task");
        SetupTasks(new[] { duplicate, kept, gone, handMade }, WorkItem("MAR-10", sprints: new[] { "MAR_1Q26_S1" }));

        var result = await _service.SyncTasksAsync();

        result.Removed.Select(r => r.TaskId).Should().BeEquivalentTo(new[] { gone.Id, duplicate.Id });
        _taskServiceMock.Verify(s => s.DeleteAsync(kept.Id, It.IsAny<CancellationToken>()), Times.Never);
        _taskServiceMock.Verify(s => s.DeleteAsync(handMade.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncTasksAsync_WhenJiraReturnsNothingInScope_RemovesNothing()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        SetupTasks(new[] { new TeamTask("Existing", tags: "jira:MAR-10") }, WorkItem("MAR-11", sprints: new[] { "REFINEMENT QUEUE" }));

        var result = await _service.SyncTasksAsync();

        result.Removed.Should().BeEmpty();
        _taskServiceMock.Verify(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncTasksAsync_CreatesMissingSprintsWithJirasDates()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        SetupTasks(Array.Empty<TeamTask>(), WorkItem("MAR-10", sprints: new[] { "MAR_2Q26_S1" }));
        var sprintId = Guid.NewGuid();
        _sprintServiceMock.Setup(s => s.GetOrCreateAsync("MAR_2Q26_S1", It.IsAny<CancellationToken>())).ReturnsAsync(new SprintDto { Id = sprintId });

        var result = await _service.SyncTasksAsync();

        result.SprintsCreated.Should().Equal("MAR_2Q26_S1");
        _sprintServiceMock.Verify(s => s.UpdateAsync(sprintId,
            It.Is<UpdateSprintDto>(d => d.StartDate == new DateTime(2025, 10, 6) && d.EndDate == new DateTime(2025, 10, 17)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncTasksAsync_SyncsParentsFirst()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncTasksAsync();

        result.Parents!.Created.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncAllAsync_IncludesTasksWithoutSyncingParentsTwice()
    {
        SetupConfiguredSettings();
        SetupSprints(Array.Empty<Sprint>(), Array.Empty<JiraSprint>());
        SetupParents(Array.Empty<Parent>(), new[] { Epic("MAR-1", "Payments") });

        var result = await _service.SyncAllAsync();

        result.Tasks.Should().NotBeNull();
        _jiraApiClientMock.Verify(c => c.GetParentIssuesAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncProductsAsync_WhenSpaceKeyMissing_ThrowsWithoutCallingJira()
    {
        SetupConfiguredSettings(projectKey: null);

        var act = () => _service.SyncProductsAsync();

        await act.Should().ThrowAsync<ValidationException>();
        _jiraApiClientMock.Verify(
            c => c.GetComponentsAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncProductsAsync_CreatesAProductForANewComponent()
    {
        SetupConfiguredSettings();
        SetupProducts(Array.Empty<Product>(), new JiraComponent("10042", "Matching", "Matches works"));

        var result = await _service.SyncProductsAsync();

        _jiraApiClientMock.Verify(c => c.GetComponentsAsync(
            new JiraConnection("https://acme.atlassian.net", "em@acme.com", "token-123"), "MAR", It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Product>(p => p.JiraComponentId == "10042" && p.Name == "Matching" && p.Description == "Matches works"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.JiraComponentCount.Should().Be(1);
        result.Created.Should().ContainSingle(c => c.Name == "Matching" && c.JiraComponentId == "10042");
    }

    [Fact]
    public async Task SyncProductsAsync_RenamedComponent_RenamesTheProduct()
    {
        SetupConfiguredSettings();
        var product = Product.FromJira("10042", "Matching", null);
        SetupProducts(new[] { product }, new JiraComponent("10042", "Matching Engine", null));

        var result = await _service.SyncProductsAsync();

        product.Name.Should().Be("Matching Engine");
        _productRepositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        result.Updated.Should().ContainSingle(c => c.PreviousName == "Matching" && c.Name == "Matching Engine");
    }

    [Fact]
    public async Task SyncProductsAsync_UnchangedComponent_IsNotWritten()
    {
        SetupConfiguredSettings();
        SetupProducts(new[] { Product.FromJira("10042", "Matching", null) }, new JiraComponent("10042", "Matching", null));

        var result = await _service.SyncProductsAsync();

        result.UnchangedCount.Should().Be(1);
        _productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncProductsAsync_LinksAProductMadeByHandWithTheSameName()
    {
        SetupConfiguredSettings();
        var handMade = new Product("matching");
        SetupProducts(new[] { handMade }, new JiraComponent("10042", "Matching", null));

        var result = await _service.SyncProductsAsync();

        handMade.JiraComponentId.Should().Be("10042");
        handMade.Name.Should().Be("Matching");
        _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Updated.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncProductsAsync_NameHeldByAnotherProduct_IsReportedNotApplied()
    {
        SetupConfiguredSettings();
        SetupProducts(new[] { Product.FromJira("10001", "Matching", null) }, new JiraComponent("10001", "Matching", null), new JiraComponent("10042", "MATCHING", null));

        var result = await _service.SyncProductsAsync();

        result.Errors.Should().ContainSingle().Which.Should().Contain("another product already has this name");
        _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncProductsAsync_RemovesProductsFromJiraWhoseComponentIsGone_KeepsHandMadeOnes()
    {
        SetupConfiguredSettings();
        var gone = Product.FromJira("10099", "Retired", null);
        var handMade = new Product("Internal tooling");
        SetupProducts(new[] { Product.FromJira("10042", "Matching", null), gone, handMade }, new JiraComponent("10042", "Matching", null));

        var result = await _service.SyncProductsAsync();

        result.Removed.Should().ContainSingle(r => r.ProductId == gone.Id);
        _projectProductRepositoryMock.Verify(r => r.DeleteByProductAsync(gone.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(r => r.DeleteAsync(gone.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(r => r.DeleteAsync(handMade.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncProductsAsync_WhenJiraHasNoComponents_RemovesNothing()
    {
        SetupConfiguredSettings();
        SetupProducts(new[] { Product.FromJira("10042", "Matching", null) });

        var result = await _service.SyncProductsAsync();

        result.Removed.Should().BeEmpty("an empty answer must never empty Hive");
        _productRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAllAsync_IncludesProducts()
    {
        SetupConfiguredSettings();
        SetupSprints(Array.Empty<Sprint>(), Array.Empty<JiraSprint>());
        SetupParents(Array.Empty<Parent>(), Array.Empty<JiraIssue>());
        SetupProducts(Array.Empty<Product>(), new JiraComponent("10042", "Matching", null));

        var result = await _service.SyncAllAsync();

        result.Products.Should().NotBeNull();
        result.Products!.Created.Should().ContainSingle();
        result.ProductsError.Should().BeNull();
    }

    [Fact]
    public async Task SyncParentsAsync_CopiesTheEpicsComponents()
    {
        SetupConfiguredSettings();
        SetupParents(Array.Empty<Parent>(), new[]
        {
            new JiraIssue("MAR-1", "Payments revamp", "Epic", "In Progress", Components: new[] { "Matching", "IPI" })
        });

        await _service.SyncParentsAsync();

        _parentRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Parent>(p => p.JiraKey == "MAR-1" && p.Components == "Matching,IPI"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncParentsAsync_ChangedComponents_UpdateTheParent()
    {
        SetupConfiguredSettings();
        var parent = Parent.FromJira("MAR-1", "Epic", "Payments", new[] { "Matching" });
        SetupParents(new[] { parent }, new[]
        {
            new JiraIssue("MAR-1", "Payments", "Epic", "In Progress", Components: new[] { "IPI" })
        });

        var result = await _service.SyncParentsAsync();

        parent.Components.Should().Be("IPI");
        result.Updated.Should().ContainSingle();
    }

    private void SetupProducts(IReadOnlyList<Product> products, params JiraComponent[] components)
    {
        _productRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.ToList());
        _productRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);
        _jiraApiClientMock
            .Setup(c => c.GetComponentsAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(components);
    }

    private static JiraWorkItem WorkItem(string key, string? parent = null, string[]? sprints = null) => new(
        key, $"Summary of {key}", string.Empty, "Task", "Backlog", "Medium", null, null, parent, null, null,
        Array.Empty<string>(), Array.Empty<string>(), null,
        (sprints ?? Array.Empty<string>()).Select((name, i) => new JiraSprint(i + 1, name, "closed", Oct6, Oct17)).ToList());

    private void SetupTasks(IReadOnlyList<TeamTask> tasks, params JiraWorkItem[] items)
    {
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks.ToList());
        _jiraApiClientMock
            .Setup(c => c.GetSprintWorkItemsAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _sprintServiceMock.Setup(s => s.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SprintDto { Id = Guid.NewGuid() });
    }

    private static JiraIssue Epic(string key, string summary) => new(key, summary, "Epic", "In Progress");

    private static JiraIssue Story(string key, string summary) => new(key, summary, "Story", "Done");

    private void SetupParents(IReadOnlyList<Parent> parents, IReadOnlyList<JiraIssue> epics)
    {
        _parentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(parents.ToList());
        _parentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Parent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parent p, CancellationToken _) => p);
        _jiraApiClientMock
            .Setup(c => c.GetParentIssuesAsync(It.IsAny<JiraConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epics);
    }

    private void SetupConfiguredSettings(string? projectKey = "MAR", int? boardId = 42, string? teamFilter = null)
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl("https://acme.atlassian.net/browse");
        settings.UpdateJiraEmail("em@acme.com");
        settings.UpdateJiraApiToken("token-123");
        settings.UpdateJiraBoardId(boardId);
        settings.UpdateJiraProjectKey(projectKey);
        settings.UpdateSprintTeamFilter(teamFilter);
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
    }

    private void SetupSprints(IReadOnlyList<Sprint> hiveSprints, IReadOnlyList<JiraSprint> jiraSprints)
    {
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(hiveSprints);
        _jiraApiClientMock
            .Setup(c => c.GetBoardSprintsAsync(It.IsAny<JiraConnection>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraSprints);
    }
}
