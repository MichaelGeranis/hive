using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hive.Tests.Application.Services;

public class TaskReadinessServiceTests
{
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock = new();
    private readonly Mock<ITeamTaskRepository> _taskRepositoryMock = new();
    private readonly Mock<ISprintRepository> _sprintRepositoryMock = new();
    private readonly Mock<ITaskReadinessReviewRepository> _reviewRepositoryMock = new();
    private readonly Mock<IClaudeApiService> _claudeApiServiceMock = new();
    private readonly Mock<IOllamaApiService> _ollamaApiServiceMock = new();
    private readonly TaskReadinessService _service;

    private readonly AppSettings _settings = new();
    private readonly Sprint _current;
    private readonly List<TeamTask> _tasks = new();
    private readonly List<TaskReadinessReview> _reviews = new();

    private static readonly TaskReviewResult NeedsWork = new(
        TaskReadinessVerdict.NeedsWork, "Adds draft saving.", ["Acceptance criteria", "What happens offline"]);

    public TaskReadinessServiceTests()
    {
        _service = new TaskReadinessService(
            _settingsRepositoryMock.Object,
            _taskRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _claudeApiServiceMock.Object,
            _ollamaApiServiceMock.Object,
            Mock.Of<ILogger<TaskReadinessService>>());

        var today = DateTime.UtcNow.Date;
        _current = new Sprint("MAR_3Q26_S6");
        _current.UpdateDates(today.AddDays(-3), today.AddDays(10));
        var next = new Sprint("MAR_4Q26_S1");
        next.UpdateDates(today.AddDays(11), today.AddDays(24));

        _settings.UpdateTaskReviewEnabled(true);
        _settingsRepositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_settings);
        _sprintRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_current, next]);
        _taskRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _tasks.ToList());
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _tasks.FirstOrDefault(t => t.Id == id));
        _reviewRepositoryMock.Setup(r => r.GetByTaskIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => _reviews.Where(r => ids.Contains(r.TeamTaskId)).ToList());
        _reviewRepositoryMock.Setup(r => r.GetByTaskIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _reviews.FirstOrDefault(r => r.TeamTaskId == id));
        _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskReadinessReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskReadinessReview review, CancellationToken _) => { _reviews.Add(review); return review; });
        _ollamaApiServiceMock
            .Setup(s => s.ReviewTaskAsync(It.IsAny<OllamaConnection>(), It.IsAny<TaskForReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NeedsWork);
    }

    private TeamTask GivenTask(string title, string description = "", TaskType type = TaskType.Story, string? sprint = null)
    {
        var task = new TeamTask(title, description, type, sprint: sprint ?? _current.Name, tags: $"jira:MAR-{_tasks.Count + 1}");
        _tasks.Add(task);
        return task;
    }

    [Fact]
    public async Task GetOverviewAsync_ListsTheUpcomingTasksWithTheirFacts_AndAsksNoModel()
    {
        GivenTask("Save drafts", "## Acceptance criteria\n- Kept on reload");
        GivenTask("Export CSV", "As a manager I want a CSV");
        GivenTask("Later", sprint: "MAR_4Q26_S5");

        var overview = await _service.GetOverviewAsync();

        overview.IsEnabled.Should().BeTrue();
        overview.CurrentSprint.Should().Be("MAR_3Q26_S6");
        overview.NextSprint.Should().Be("MAR_4Q26_S1");
        overview.Tasks.Select(t => t.Title).Should().BeEquivalentTo(["Save drafts", "Export CSV"]);
        overview.Tasks.Single(t => t.Title == "Save drafts").HasAcceptanceCriteria.Should().BeTrue();
        overview.Tasks.Single(t => t.Title == "Export CSV").HasAcceptanceCriteria.Should().BeFalse();
        overview.Tasks[0].JiraKey.Should().StartWith("MAR-");
        overview.PendingCount.Should().Be(2);
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOverviewAsync_Disabled_HasNothingPending()
    {
        _settings.UpdateTaskReviewEnabled(false);
        GivenTask("Save drafts");

        var overview = await _service.GetOverviewAsync();

        overview.IsEnabled.Should().BeFalse();
        overview.Tasks.Should().ContainSingle("the facts are shown whether or not the AI reviews");
        overview.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOverviewAsync_RemovesReviewsOfTasksThatAreGone()
    {
        var task = GivenTask("Save drafts");

        await _service.GetOverviewAsync();

        _reviewRepositoryMock.Verify(r => r.DeleteForMissingTasksAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { task.Id })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewNextAsync_ReviewsOneTask_SendsTitleTypeAndDescription()
    {
        GivenTask("Save drafts", "Keep what I typed");
        GivenTask("Export CSV");
        TaskForReview? sent = null;
        _ollamaApiServiceMock
            .Setup(s => s.ReviewTaskAsync(It.IsAny<OllamaConnection>(), It.IsAny<TaskForReview>(), It.IsAny<CancellationToken>()))
            .Callback((OllamaConnection _, TaskForReview task, CancellationToken _) => sent = task)
            .ReturnsAsync(NeedsWork);

        var overview = await _service.ReviewNextAsync();

        _ollamaApiServiceMock.Verify(s => s.ReviewTaskAsync(It.IsAny<OllamaConnection>(), It.IsAny<TaskForReview>(), It.IsAny<CancellationToken>()), Times.Once);
        sent!.Type.Should().Be("Story");
        sent.Title.Should().Be("Export CSV", "equal priority orders by title");
        sent.HasAcceptanceCriteria.Should().BeFalse();
        _reviews.Should().ContainSingle();
        overview.PendingCount.Should().Be(1);
        var reviewed = overview.Tasks.Single(t => t.Review is not null).Review!;
        reviewed.Verdict.Should().Be("NeedsWork");
        reviewed.Missing.Should().Equal("Acceptance criteria", "What happens offline");
        reviewed.Model.Should().Be(AppSettings.DefaultOllamaModel);
        reviewed.IsUpToDate.Should().BeTrue();
    }

    [Fact]
    public async Task ReviewNextAsync_EverythingUpToDate_AsksNoModel()
    {
        var task = GivenTask("Save drafts");
        _reviews.Add(new TaskReadinessReview(task.Id, TaskReadinessVerdict.Ready, "Fine.", "[]", "m", TaskReadiness.Fingerprint(task)));

        var overview = await _service.ReviewNextAsync();

        overview.PendingCount.Should().Be(0);
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReviewNextAsync_TaskChangedSinceItsReview_ReviewsItAgain()
    {
        var task = GivenTask("Save drafts");
        var old = new TaskReadinessReview(task.Id, TaskReadinessVerdict.Ready, "Fine.", "[]", "m", "OLD");
        _reviews.Add(old);

        var before = await _service.GetOverviewAsync();
        before.Tasks.Single().Review!.IsUpToDate.Should().BeFalse();

        await _service.ReviewNextAsync();

        _reviewRepositoryMock.Verify(r => r.UpdateAsync(old, It.IsAny<CancellationToken>()), Times.Once);
        old.Verdict.Should().Be(TaskReadinessVerdict.NeedsWork);
    }

    [Fact]
    public async Task ReviewNextAsync_ModelFails_ReturnsTheErrorAndKeepsTheOldReview()
    {
        var task = GivenTask("Save drafts");
        var old = new TaskReadinessReview(task.Id, TaskReadinessVerdict.Ready, "Fine.", "[]", "m", "OLD");
        _reviews.Add(old);
        _ollamaApiServiceMock
            .Setup(s => s.ReviewTaskAsync(It.IsAny<OllamaConnection>(), It.IsAny<TaskForReview>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaApiException("Could not reach Ollama."));

        var overview = await _service.ReviewNextAsync();

        overview.Error.Should().Be("Could not reach Ollama.");
        overview.Tasks.Single().Review!.Summary.Should().Be("Fine.");
        _reviewRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskReadinessReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewNextAsync_ClaudeWithoutKey_AsksNoModel()
    {
        _settings.UpdateSentimentProvider("Claude");
        GivenTask("Save drafts");

        var overview = await _service.ReviewNextAsync();

        overview.IsConfigured.Should().BeFalse();
        _claudeApiServiceMock.VerifyNoOtherCalls();
        _ollamaApiServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReviewTaskAsync_ReviewsEvenAnUpToDateTask()
    {
        var task = GivenTask("Save drafts");
        _reviews.Add(new TaskReadinessReview(task.Id, TaskReadinessVerdict.Ready, "Fine.", "[]", "m", TaskReadiness.Fingerprint(task)));

        var overview = await _service.ReviewTaskAsync(task.Id);

        overview!.Tasks.Single().Review!.Verdict.Should().Be("NeedsWork");
    }

    [Fact]
    public async Task ReviewTaskAsync_UnknownTask_ReturnsNull()
    {
        (await _service.ReviewTaskAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public void MapReview_NoReview_IsNull()
    {
        TaskReadinessService.MapReview(null, new TeamTask("Work")).Should().BeNull();
    }
}
