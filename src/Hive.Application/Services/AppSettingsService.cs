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
            var defaultMappings = GetDefaultMappings();
            var defaultEntity = new AppSettings(SerializeMappings(defaultMappings));
            entity = await _repository.AddAsync(defaultEntity, cancellationToken);
        }

        return MapToDto(entity);
    }

    public async Task<AppSettingsDto> UpdateAsync(UpdateAppSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(cancellationToken);

        if (entity is null)
        {
            // Create new settings if none exist
            entity = new AppSettings(SerializeMappings(dto.StoryPointMappings));
            entity = await _repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            // Update story point mappings only if provided
            if (dto.StoryPointMappings.Count > 0)
            {
                entity.UpdateStoryPointMappings(SerializeMappings(dto.StoryPointMappings));
                await _repository.UpdateAsync(entity, cancellationToken);
            }
        }

        // Update T-shirt size mappings if provided
        if (dto.TshirtSizeMappings.Count > 0)
        {
            entity.UpdateTshirtSizeMappings(SerializeTshirtSizeMappings(dto.TshirtSizeMappings));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update sentiment analysis settings if provided
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

        // Update sprint team filter if provided or explicitly cleared
        if (dto.ClearSprintTeamFilter || dto.SprintTeamFilter is not null)
        {
            entity.UpdateSprintTeamFilter(dto.ClearSprintTeamFilter ? null : dto.SprintTeamFilter);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update dashboard thresholds if any provided
        if (dto.MaxInProgressTasks.HasValue || dto.MaxBlockedTasks.HasValue ||
            dto.MaxInReviewTasks.HasValue || dto.MinProjectMembers.HasValue)
        {
            entity.UpdateDashboardThresholds(
                dto.MaxInProgressTasks ?? entity.MaxInProgressTasks,
                dto.MaxBlockedTasks ?? entity.MaxBlockedTasks,
                dto.MaxInReviewTasks ?? entity.MaxInReviewTasks,
                dto.MinProjectMembers ?? entity.MinProjectMembers
            );
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update support labels if provided
        if (dto.SupportLabels is not null)
        {
            entity.UpdateSupportLabels(SerializeLabels(dto.SupportLabels));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update maintenance labels if provided
        if (dto.MaintenanceLabels is not null)
        {
            entity.UpdateMaintenanceLabels(SerializeLabels(dto.MaintenanceLabels));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        // Update Jira base URL if provided
        if (dto.JiraBaseUrl is not null)
        {
            entity.UpdateJiraBaseUrl(dto.JiraBaseUrl);
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        return MapToDto(entity);
    }

    private AppSettingsDto MapToDto(AppSettings entity)
    {
        var mappings = DeserializeMappings(entity.StoryPointMappings);
        var tshirtMappings = DeserializeTshirtSizeMappings(entity.TshirtSizeMappings);
        var supportLabels = DeserializeLabels(entity.SupportLabels);
        var maintenanceLabels = DeserializeLabels(entity.MaintenanceLabels);

        return new AppSettingsDto
        {
            Id = entity.Id,
            StoryPointMappings = mappings,
            TshirtSizeMappings = tshirtMappings,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            HasClaudeApiKey = entity.HasClaudeApiKey,
            SentimentAnalysisDays = entity.SentimentAnalysisDays,
            SentimentAnalysisEnabled = entity.SentimentAnalysisEnabled,
            SprintTeamFilter = entity.SprintTeamFilter,
            MaxInProgressTasks = entity.MaxInProgressTasks,
            MaxBlockedTasks = entity.MaxBlockedTasks,
            MaxInReviewTasks = entity.MaxInReviewTasks,
            MinProjectMembers = entity.MinProjectMembers,
            SupportLabels = supportLabels,
            MaintenanceLabels = maintenanceLabels,
            JiraBaseUrl = entity.JiraBaseUrl
        };
    }

    private static List<StoryPointMapping> GetDefaultMappings()
    {
        return new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 2, Label = "1 SP = 2 hours" },
            new() { Points = 2, Hours = 4, Label = "2 SP = 4 hours (4 hours)" },
            new() { Points = 3, Hours = 8, Label = "3 SP = 8 hours (1 day)" },
            new() { Points = 5, Hours = 24, Label = "5 SP = 24 hours (3 days)" },
            new() { Points = 8, Hours = 72, Label = "8 SP = 72 hours (1 sprint)" },
            new() { Points = 13, Hours = 150, Label = "13 SP = 150 hours (2 sprints)" },
            new() { Points = 21, Hours = 240, Label = "21 SP = 240 hours (1 month)" }
        };
    }

    private static string SerializeMappings(List<StoryPointMapping> mappings)
    {
        return JsonSerializer.Serialize(mappings);
    }

    private static List<StoryPointMapping> DeserializeMappings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StoryPointMapping>>(json) ?? GetDefaultMappings();
        }
        catch
        {
            return GetDefaultMappings();
        }
    }

    private static List<TshirtSizeMapping> GetDefaultTshirtSizeMappings()
    {
        return new List<TshirtSizeMapping>
        {
            new() { Size = "S", Sprints = 0.5m, Label = "S = 1/2 sprint" },
            new() { Size = "M", Sprints = 1, Label = "M = 1 sprint" },
            new() { Size = "L", Sprints = 2, Label = "L = 2 sprints" },
            new() { Size = "XL", Sprints = 4, Label = "XL = 4+ sprints" }
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
}
