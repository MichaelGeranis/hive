using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Quarter entity.
/// </summary>
public interface IQuarterRepository
{
    Task<Quarter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Quarter?> GetByYearQuarterAsync(int year, int quarterNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quarter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quarter>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<Quarter?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Quarter> AddAsync(Quarter quarter, CancellationToken cancellationToken = default);
    Task UpdateAsync(Quarter quarter, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int year, int quarterNumber, CancellationToken cancellationToken = default);
}
