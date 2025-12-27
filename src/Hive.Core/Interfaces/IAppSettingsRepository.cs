using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for AppSettings persistence.
/// </summary>
public interface IAppSettingsRepository
{
    Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task<AppSettings> AddAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task UpdateAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
