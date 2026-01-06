using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface defining use cases for DirectReport management.
/// Following Interface Segregation Principle (ISP).
/// </summary>
public interface IDirectReportService
{
    Task<DirectReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DirectReportDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DirectReportDto> CreateAsync(CreateDirectReportDto dto, CancellationToken cancellationToken = default);
    Task<DirectReportDto> UpdateAsync(Guid id, UpdateDirectReportDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BulkImportResultDto> BulkImportAsync(BulkImportDirectReportsDto dto, CancellationToken cancellationToken = default);
}
