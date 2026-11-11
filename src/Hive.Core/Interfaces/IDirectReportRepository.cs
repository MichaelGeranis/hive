using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for DirectReport entity.
/// Following Interface Segregation Principle (ISP) and Dependency Inversion Principle (DIP).
/// </summary>
public interface IDirectReportRepository
{
    Task<DirectReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DirectReport?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DirectReport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DirectReport> AddAsync(DirectReport directReport, CancellationToken cancellationToken = default);
    Task UpdateAsync(DirectReport directReport, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
