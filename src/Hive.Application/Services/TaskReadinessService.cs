using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Application.Services;

/// <summary>
/// Reviews the upcoming tasks with the model chosen for sentiment, one task per call, and
/// caches each review until the task's title, type or description changes.
/// </summary>
public class TaskReadinessService : ITaskReadinessService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ITaskReadinessReviewRepository _reviewRepository;
    private readonly IClaudeApiService _claudeApiService;
    private readonly IOllamaApiService _ollamaApiService;
    private readonly ILogger<TaskReadinessService> _logger;

    public TaskReadinessService(
        IAppSettingsRepository settingsRepository,
        ITeamTaskRepository taskRepository,
        ISprintRepository sprintRepository,
        ITaskReadinessReviewRepository reviewRepository,
        IClaudeApiService claudeApiService,
        IOllamaApiService ollamaApiService,
        ILogger<TaskReadinessService> logger)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
        _claudeApiService = claudeApiService ?? throw new ArgumentNullException(nameof(claudeApiService));
        _ollamaApiService = ollamaApiService ?? throw new ArgumentNullException(nameof(ollamaApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskReadinessOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(cancellationToken);
        return state.Overview();
    }

    public async Task<TaskReadinessOverviewDto> ReviewNextAsync(CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(cancellationToken);
        if (!state.CanReview)
        {
            return state.Overview();
        }

        var next = state.Upcoming.FirstOrDefault(t => !state.IsUpToDate(t));
        if (next is null)
        {
            return state.Overview();
        }

        var error = await ReviewAsync(state, next, cancellationToken);
        return (await LoadAsync(cancellationToken)).Overview(error);
    }

    public async Task<TaskReadinessOverviewDto?> ReviewTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var state = await LoadAsync(cancellationToken);
        if (!state.CanReview)
        {
            return state.Overview();
        }

        var error = await ReviewAsync(state, task, cancellationToken);
        return (await LoadAsync(cancellationToken)).Overview(error);
    }

    /// <summary>
    /// The review as the API shows it, or null for a task never reviewed.
    /// </summary>
    public static TaskAiReviewDto? MapReview(TaskReadinessReview? review, TeamTask task)
    {
        if (review is null)
        {
            return null;
        }

        return new TaskAiReviewDto
        {
            Verdict = review.Verdict.ToString(),
            Summary = review.Summary,
            Missing = Deserialize(review.MissingJson),
            Model = review.Model,
            AnalyzedAt = review.AnalyzedAt,
            IsUpToDate = !review.IsStaleFor(TaskReadiness.Fingerprint(task))
        };
    }

    /// <summary>
    /// Reviews one task and saves the review. Returns the model's error instead of throwing,
    /// and leaves an earlier review in place when it fails.
    /// </summary>
    private async Task<string?> ReviewAsync(State state, TeamTask task, CancellationToken cancellationToken)
    {
        var settings = state.Settings;
        var forReview = new TaskForReview(task.Title, task.Type.ToString(), task.Description, task.HasAcceptanceCriteria());

        TaskReviewResult result;
        try
        {
            result = settings.SentimentProvider == SentimentProvider.Claude
                ? await _claudeApiService.ReviewTaskAsync(settings.ClaudeApiKey!, forReview, cancellationToken)
                : await _ollamaApiService.ReviewTaskAsync(
                    new OllamaConnection(settings.OllamaBaseUrl, settings.OllamaModel), forReview, cancellationToken);
        }
        catch (AiProviderException ex)
        {
            _logger.LogWarning(ex, "Could not review task {TaskId}", task.Id);
            return ex.Message;
        }

        var model = settings.SentimentProvider == SentimentProvider.Claude ? "Claude" : settings.OllamaModel;
        var missingJson = JsonSerializer.Serialize(result.Missing);
        var fingerprint = TaskReadiness.Fingerprint(task);

        var existing = await _reviewRepository.GetByTaskIdAsync(task.Id, cancellationToken);
        if (existing is null)
        {
            await _reviewRepository.AddAsync(
                new TaskReadinessReview(task.Id, result.Verdict, result.Summary, missingJson, model, fingerprint),
                cancellationToken);
        }
        else
        {
            existing.Update(result.Verdict, result.Summary, missingJson, model, fingerprint);
            await _reviewRepository.UpdateAsync(existing, cancellationToken);
        }

        return null;
    }

    private async Task<State> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken) ?? new AppSettings();
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        // A task Jira no longer returns takes its review with it
        await _reviewRepository.DeleteForMissingTasksAsync(tasks.Select(t => t.Id).ToList(), cancellationToken);

        var upcoming = TaskReadiness.FindUpcoming(tasks, sprints, today);
        var (current, next) = TaskReadiness.FindSprints(sprints, today);
        var reviews = (await _reviewRepository.GetByTaskIdsAsync(upcoming.Select(t => t.Id).ToList(), cancellationToken))
            .ToDictionary(r => r.TeamTaskId);

        return new State(settings, upcoming, reviews, current, next);
    }

    private static IReadOnlyList<string> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record State(
        AppSettings Settings,
        IReadOnlyList<TeamTask> Upcoming,
        IReadOnlyDictionary<Guid, TaskReadinessReview> Reviews,
        Sprint? Current,
        Sprint? Next)
    {
        public bool CanReview => Settings.TaskReviewEnabled && Settings.IsSentimentProviderConfigured;

        public bool IsUpToDate(TeamTask task) =>
            Reviews.TryGetValue(task.Id, out var review) && !review.IsStaleFor(TaskReadiness.Fingerprint(task));

        public TaskReadinessOverviewDto Overview(string? error = null) => new()
        {
            IsEnabled = Settings.TaskReviewEnabled,
            IsConfigured = Settings.IsSentimentProviderConfigured,
            CurrentSprint = Current?.Name,
            NextSprint = Next?.Name,
            Tasks = Upcoming.Select(task => new TaskReadinessItemDto
            {
                TaskId = task.Id,
                Title = task.Title,
                TypeName = task.Type.ToString(),
                StatusName = task.Status.ToString(),
                Sprint = task.Sprint,
                JiraKey = task.GetJiraKey(),
                NeedsAcceptanceCriteria = task.NeedsAcceptanceCriteria(),
                HasAcceptanceCriteria = task.HasAcceptanceCriteria(),
                Review = MapReview(Reviews.GetValueOrDefault(task.Id), task)
            }).ToList(),
            PendingCount = CanReview ? Upcoming.Count(t => !IsUpToDate(t)) : 0,
            Error = error
        };
    }
}
