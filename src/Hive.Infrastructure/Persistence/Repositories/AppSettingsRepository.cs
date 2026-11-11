using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IAppSettingsRepository.
/// </summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly InMemoryDbContext _context;

    public AppSettingsRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = _context.AppSettings.FirstOrDefault();
        return Task.FromResult(settings);
    }

    public Task<AppSettings> AddAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _context.AppSettings.Add(settings);
        return Task.FromResult(settings);
    }

    public Task UpdateAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        // In-memory update is automatic since we're working with the same reference
        return Task.CompletedTask;
    }
}
