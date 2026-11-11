using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for QuarterMember entity.
/// </summary>
public interface IQuarterMemberRepository
{
    Task<IReadOnlyList<QuarterMember>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<QuarterMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuarterMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<QuarterMember?> GetByQuarterAndDirectReportAsync(Guid quarterId, Guid directReportId, CancellationToken cancellationToken = default);
    Task DeleteByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default);
    Task<QuarterMember> AddAsync(QuarterMember entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(QuarterMember entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
