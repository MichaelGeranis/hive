using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _repository;

    public AppSettingsService(IAppSettingsRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(cancellationToken);

        if (entity is null)
        {
            // Create default settings if none exist
            entity = await _repository.AddAsync(new AppSettings(), cancellationToken);
        }

        return MapToDto(entity);
    }

    public async Task<AppSettingsDto> UpdateAsync(UpdateAppSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(cancellationToken);

        if (entity is null)
        {
            // Create new settings if none exist
            entity = await _repository.AddAsync(new AppSettings(), cancellationToken);
        }

        // Update T-shirt size mappings if provided
        if (dto.TshirtSizeMappings.Count > 0)
        {
            entity.UpdateTshirtSizeMappings(SerializeTshirtSizeMappings(dto.TshirtSizeMappings));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update sentiment analysis settings if provided
        if (dto.SentimentProvider is not null)
        {
            entity.UpdateSentimentProvider(dto.SentimentProvider);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.OllamaBaseUrl is not null || dto.OllamaModel is not null)
        {
            entity.UpdateOllamaConnection(
                dto.OllamaBaseUrl ?? entity.OllamaBaseUrl,
                dto.OllamaModel ?? entity.OllamaModel);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.ClaudeApiKey is not null)
        {
            entity.UpdateClaudeApiKey(dto.ClaudeApiKey);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.SentimentAnalysisDays.HasValue || dto.SentimentAnalysisEnabled.HasValue)
        {
            entity.UpdateSentimentSettings(
                dto.SentimentAnalysisDays ?? entity.SentimentAnalysisDays,
                dto.SentimentAnalysisEnabled ?? entity.SentimentAnalysisEnabled
            );
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.RetroSummaryEnabled.HasValue || dto.RetroSummaryCount.HasValue)
        {
            entity.UpdateRetroSummarySettings(
                dto.RetroSummaryEnabled ?? entity.RetroSummaryEnabled,
                dto.RetroSummaryCount ?? entity.RetroSummaryCount
            );
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.TaskReviewEnabled.HasValue)
        {
            entity.UpdateTaskReviewEnabled(dto.TaskReviewEnabled.Value);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update sprint team filter if provided or explicitly cleared
        if (dto.ClearSprintTeamFilter || dto.SprintTeamFilter is not null)
        {
            entity.UpdateSprintTeamFilter(dto.ClearSprintTeamFilter ? null : dto.SprintTeamFilter);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update dashboard thresholds if any provided
        if (dto.MaxInProgressTasks.HasValue || dto.MaxBlockedTasks.HasValue ||
            dto.MaxInReviewTasks.HasValue || dto.MinProductMembers.HasValue)
        {
            entity.UpdateDashboardThresholds(
                dto.MaxInProgressTasks ?? entity.MaxInProgressTasks,
                dto.MaxBlockedTasks ?? entity.MaxBlockedTasks,
                dto.MaxInReviewTasks ?? entity.MaxInReviewTasks,
                dto.MinProductMembers ?? entity.MinProductMembers
            );
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.OneOnOneCadenceDays.HasValue)
        {
            entity.UpdateOneOnOneCadence(dto.OneOnOneCadenceDays.Value);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.MinMemberProducts.HasValue)
        {
            entity.UpdateMinMemberProducts(dto.MinMemberProducts.Value);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.StaleTaskDays.HasValue)
        {
            entity.UpdateStaleTaskDays(dto.StaleTaskDays.Value);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update maintenance labels if provided
        if (dto.MaintenanceLabels is not null)
        {
            entity.UpdateMaintenanceLabels(SerializeLabels(dto.MaintenanceLabels));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update the epic names of the work categories if provided
        if (dto.WorkCategoryEpicNames is not null)
        {
            entity.UpdateWorkCategoryEpicNames(ParseWorkCategoryEpicNames(dto.WorkCategoryEpicNames));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update Jira base URL if provided
        if (dto.JiraBaseUrl is not null)
        {
            entity.UpdateJiraBaseUrl(dto.JiraBaseUrl);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.JiraEmail is not null)
        {
            entity.UpdateJiraEmail(dto.JiraEmail);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.JiraApiToken is not null)
        {
            entity.UpdateJiraApiToken(dto.JiraApiToken);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.JiraProjectKey is not null)
        {
            entity.UpdateJiraProjectKey(dto.JiraProjectKey);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.ClearJiraBoardId || dto.JiraBoardId.HasValue)
        {
            entity.UpdateJiraBoardId(dto.ClearJiraBoardId ? null : dto.JiraBoardId);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        if (dto.GoogleCalendarUrl is not null)
        {
            entity.UpdateGoogleCalendarUrl(dto.GoogleCalendarUrl);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        return MapToDto(entity);
    }

    private AppSettingsDto MapToDto(AppSettings entity)
    {
        var tshirtMappings = DeserializeTshirtSizeMappings(entity.TshirtSizeMappings);
        var maintenanceLabels = DeserializeLabels(entity.MaintenanceLabels);

        return new AppSettingsDto
        {
            Id = entity.Id,
            TshirtSizeMappings = tshirtMappings,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            SentimentProvider = entity.SentimentProvider.ToString(),
            HasClaudeApiKey = entity.HasClaudeApiKey,
            OllamaBaseUrl = entity.OllamaBaseUrl,
            OllamaModel = entity.OllamaModel,
            SentimentAnalysisDays = entity.SentimentAnalysisDays,
            SentimentAnalysisEnabled = entity.SentimentAnalysisEnabled,
            RetroSummaryEnabled = entity.RetroSummaryEnabled,
            RetroSummaryCount = entity.RetroSummaryCount,
            TaskReviewEnabled = entity.TaskReviewEnabled,
            SprintTeamFilter = entity.SprintTeamFilter,
            MaxInProgressTasks = entity.MaxInProgressTasks,
            MaxBlockedTasks = entity.MaxBlockedTasks,
            MaxInReviewTasks = entity.MaxInReviewTasks,
            MinProductMembers = entity.MinProductMembers,
            MinMemberProducts = entity.MinMemberProducts,
            OneOnOneCadenceDays = entity.OneOnOneCadenceDays,
            StaleTaskDays = entity.StaleTaskDays,
            MaintenanceLabels = maintenanceLabels,
            WorkCategoryEpicNames = entity.GetWorkCategoryEpicNames()
                .ToDictionary(entry => entry.Key.ToString(), entry => entry.Value.ToList()),
            JiraBaseUrl = entity.JiraBaseUrl,
            JiraBrowseUrl = entity.GetJiraBrowseUrl(),
            JiraEmail = entity.JiraEmail,
            HasJiraApiToken = entity.HasJiraApiToken,
            JiraBoardId = entity.JiraBoardId,
            JiraProjectKey = entity.JiraProjectKey,
            HasGoogleCalendarUrl = entity.HasGoogleCalendarUrl
        };
    }

    // XXL is an option that takes up the whole quarter
    private static List<TshirtSizeMapping> GetDefaultTshirtSizeMappings()
    {
        return new List<TshirtSizeMapping>
        {
            new() { Size = "S", Sprints = 1 },
            new() { Size = "M", Sprints = 2 },
            new() { Size = "L", Sprints = 4 },
            new() { Size = "XL", Sprints = 6 },
            new() { Size = "XXL", Sprints = 12 }
        };
    }

    private static string SerializeTshirtSizeMappings(List<TshirtSizeMapping> mappings)
    {
        return JsonSerializer.Serialize(mappings);
    }

    private static List<TshirtSizeMapping> DeserializeTshirtSizeMappings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return GetDefaultTshirtSizeMappings();
        }

        try
        {
            return JsonSerializer.Deserialize<List<TshirtSizeMapping>>(json) ?? GetDefaultTshirtSizeMappings();
        }
        catch
        {
            return GetDefaultTshirtSizeMappings();
        }
    }

    private static string SerializeLabels(List<string> labels)
    {
        return JsonSerializer.Serialize(labels);
    }

    private static List<string> DeserializeLabels(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static Dictionary<WorkCategory, IReadOnlyList<string>> ParseWorkCategoryEpicNames(Dictionary<string, List<string>> byName)
    {
        var parsed = new Dictionary<WorkCategory, IReadOnlyList<string>>();
        foreach (var (name, epicNames) in byName)
        {
            if (!Enum.TryParse<WorkCategory>(name, ignoreCase: true, out var category) || int.TryParse(name, out _))
            {
                throw new ArgumentException($"There is no work category called {name}.");
            }

            parsed[category] = epicNames ?? [];
        }

        return parsed;
    }
}
