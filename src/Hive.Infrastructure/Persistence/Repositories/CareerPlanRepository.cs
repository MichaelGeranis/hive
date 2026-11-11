using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ICareerPlanRepository.
/// </summary>
public class CareerPlanRepository : ICareerPlanRepository
{
    private readonly InMemoryDbContext _context;

    public CareerPlanRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<CareerPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.CareerPlans.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<CareerPlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Ordered(_context.CareerPlans.Values));
    }

    public Task<CareerPlan?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.CareerPlans.Values.FirstOrDefault(x => x.DirectReportId == directReportId));
    }

    public Task<CareerPlan> AddAsync(CareerPlan plan, CancellationToken cancellationToken = default)
    {
        if (_context.CareerPlans.Values.Any(x => x.DirectReportId == plan.DirectReportId && x.Id != plan.Id))
        {
            throw new InvalidOperationException($"Direct report '{plan.DirectReportId}' already has a career plan.");
        }

        if (!_context.CareerPlans.TryAdd(plan.Id, plan))
        {
            throw new InvalidOperationException($"CareerPlan with id '{plan.Id}' already exists.");
        }
        return Task.FromResult(plan);
    }

    public Task UpdateAsync(CareerPlan plan, CancellationToken cancellationToken = default)
    {
        if (!_context.CareerPlans.ContainsKey(plan.Id))
        {
            throw new InvalidOperationException($"CareerPlan with id '{plan.Id}' not found.");
        }
        _context.CareerPlans[plan.Id] = plan;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.CareerPlans.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsForDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.CareerPlans.Values.Any(x => x.DirectReportId == directReportId));
    }

    private static IReadOnlyList<CareerPlan> Ordered(IEnumerable<CareerPlan> plans) =>
        plans.OrderByDescending(x => x.CreatedAt).ToList();
}
