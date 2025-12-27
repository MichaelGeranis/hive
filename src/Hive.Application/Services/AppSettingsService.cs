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
            // Update existing settings
            entity.UpdateStoryPointMappings(SerializeMappings(dto.StoryPointMappings));
            await _repository.UpdateAsync(entity, cancellationToken);
        }

        return MapToDto(entity);
    }

    private AppSettingsDto MapToDto(AppSettings entity)
    {
        var mappings = DeserializeMappings(entity.StoryPointMappings);

        return new AppSettingsDto
        {
            Id = entity.Id,
            StoryPointMappings = mappings,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static List<StoryPointMapping> GetDefaultMappings()
    {
        return new List<StoryPointMapping>
        {
            new() { Points = 1, Hours = 4, Label = "1 SP = 4 hours" },
            new() { Points = 2, Hours = 8, Label = "2 SP = 8 hours (1 day)" },
            new() { Points = 3, Hours = 12, Label = "3 SP = 12 hours (1.5 days)" },
            new() { Points = 5, Hours = 24, Label = "5 SP = 24 hours (3 days)" },
            new() { Points = 8, Hours = 40, Label = "8 SP = 40 hours (1 week)" },
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
}
