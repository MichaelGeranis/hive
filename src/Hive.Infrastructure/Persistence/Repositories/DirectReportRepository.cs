using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IDirectReportRepository.
/// Following Liskov Substitution Principle (LSP) - can be replaced with any other implementation.
/// </summary>
public class DirectReportRepository : IDirectReportRepository
{
    private readonly InMemoryDbContext _context;

    public DirectReportRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<DirectReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.DirectReports.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<DirectReport?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var entity = _context.DirectReports.Values
            .FirstOrDefault(x => x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<DirectReport>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.DirectReports.Values
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToList();
        return Task.FromResult<IReadOnlyList<DirectReport>>(entities);
    }

    public Task<DirectReport> AddAsync(DirectReport directReport, CancellationToken cancellationToken = default)
    {
        if (!_context.DirectReports.TryAdd(directReport.Id, directReport))
        {
            throw new InvalidOperationException($"DirectReport with id '{directReport.Id}' already exists.");
        }
        return Task.FromResult(directReport);
    }

    public Task UpdateAsync(DirectReport directReport, CancellationToken cancellationToken = default)
    {
        if (!_context.DirectReports.ContainsKey(directReport.Id))
        {
            throw new InvalidOperationException($"DirectReport with id '{directReport.Id}' not found.");
        }
        _context.DirectReports[directReport.Id] = directReport;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.DirectReports.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.DirectReports.ContainsKey(id));
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = _context.DirectReports.Values
            .Any(x => x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
