using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteCareerPlanRepository : ICareerPlanRepository
{
    private readonly HiveDbContext _context;

    public SqliteCareerPlanRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CareerPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CareerPlans.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<CareerPlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CareerPlans
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CareerPlan?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.CareerPlans.AsNoTracking().FirstOrDefaultAsync(x => x.DirectReportId == directReportId, cancellationToken);
    }

    public async Task<CareerPlan> AddAsync(CareerPlan plan, CancellationToken cancellationToken = default)
    {
        _context.CareerPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task UpdateAsync(CareerPlan plan, CancellationToken cancellationToken = default)
    {
        _context.CareerPlans.UpdateUntracked(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CareerPlans.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.CareerPlans.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsForDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.CareerPlans.AnyAsync(x => x.DirectReportId == directReportId, cancellationToken);
    }
}
