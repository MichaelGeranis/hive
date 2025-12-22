using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of ISkillAssessmentRepository.
/// </summary>
public class SkillAssessmentRepository : ISkillAssessmentRepository
{
    private readonly InMemoryDbContext _context;

    public SkillAssessmentRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SkillAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillAssessments.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<SkillAssessment?> GetByDirectReportAndSkillAsync(Guid directReportId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var entity = _context.SkillAssessments.Values
            .FirstOrDefault(x => x.DirectReportId == directReportId && x.SkillId == skillId);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<SkillAssessment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.SkillAssessments.Values
            .OrderBy(x => x.DirectReportId)
            .ThenBy(x => x.SkillId)
            .ToList();

        return Task.FromResult<IReadOnlyList<SkillAssessment>>(entities);
    }

    public Task<IReadOnlyList<SkillAssessment>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = _context.SkillAssessments.Values
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.SkillId)
            .ToList();

        return Task.FromResult<IReadOnlyList<SkillAssessment>>(entities);
    }

    public Task<IReadOnlyList<SkillAssessment>> GetBySkillIdAsync(Guid skillId, CancellationToken cancellationToken = default)
    {
        var entities = _context.SkillAssessments.Values
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.DirectReportId)
            .ToList();

        return Task.FromResult<IReadOnlyList<SkillAssessment>>(entities);
    }

    public Task<SkillAssessment> AddAsync(SkillAssessment assessment, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillAssessments.TryAdd(assessment.Id, assessment))
        {
            throw new InvalidOperationException($"SkillAssessment with id '{assessment.Id}' already exists.");
        }
        return Task.FromResult(assessment);
    }

    public Task UpdateAsync(SkillAssessment assessment, CancellationToken cancellationToken = default)
    {
        if (!_context.SkillAssessments.ContainsKey(assessment.Id))
        {
            throw new InvalidOperationException($"SkillAssessment with id '{assessment.Id}' not found.");
        }
        _context.SkillAssessments[assessment.Id] = assessment;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.SkillAssessments.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.SkillAssessments.ContainsKey(id));
    }

    public Task<bool> AssessmentExistsAsync(Guid directReportId, Guid skillId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _context.SkillAssessments.Values
            .Any(x => x.DirectReportId == directReportId
                      && x.SkillId == skillId
                      && (!excludeId.HasValue || x.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}
