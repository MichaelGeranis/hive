using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IQuarterRepository.
/// </summary>
public class QuarterRepository : IQuarterRepository
{
    private readonly InMemoryDbContext _context;

    public QuarterRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Quarter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Quarters.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Quarter?> GetByYearQuarterAsync(int year, int quarterNumber, CancellationToken cancellationToken = default)
    {
        var entity = _context.Quarters.Values
            .FirstOrDefault(x => x.Year == year && x.QuarterNumber == quarterNumber);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Quarter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Quarters.Values
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.QuarterNumber)
            .ToList();
        return Task.FromResult<IReadOnlyList<Quarter>>(entities);
    }

    public Task<IReadOnlyList<Quarter>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var entities = _context.Quarters.Values
            .Where(x => x.Year == year)
            .OrderBy(x => x.QuarterNumber)
            .ToList();
        return Task.FromResult<IReadOnlyList<Quarter>>(entities);
    }

    public Task<Quarter?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var entity = _context.Quarters.Values
            .FirstOrDefault(x => x.Status == QuarterStatus.Active);
        return Task.FromResult(entity);
    }

    public Task<Quarter> AddAsync(Quarter quarter, CancellationToken cancellationToken = default)
    {
        if (!_context.Quarters.TryAdd(quarter.Id, quarter))
        {
            throw new InvalidOperationException($"Quarter with id '{quarter.Id}' already exists.");
        }
        return Task.FromResult(quarter);
    }

    public Task UpdateAsync(Quarter quarter, CancellationToken cancellationToken = default)
    {
        if (!_context.Quarters.ContainsKey(quarter.Id))
        {
            throw new InvalidOperationException($"Quarter with id '{quarter.Id}' not found.");
        }
        _context.Quarters[quarter.Id] = quarter;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Quarters.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(int year, int quarterNumber, CancellationToken cancellationToken = default)
    {
        var exists = _context.Quarters.Values
            .Any(x => x.Year == year && x.QuarterNumber == quarterNumber);
        return Task.FromResult(exists);
    }
}
