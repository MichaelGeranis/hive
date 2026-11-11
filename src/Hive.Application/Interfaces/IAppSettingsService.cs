using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for managing application settings.
/// </summary>
public interface IAppSettingsService
{
    Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<AppSettingsDto> UpdateAsync(UpdateAppSettingsDto dto, CancellationToken cancellationToken = default);
}
