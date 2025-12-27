using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteSkillAssessmentRepository : ISkillAssessmentRepository
{
    private readonly HiveDbContext _context;

    public SqliteSkillAssessmentRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SkillAssessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments.FindAsync([id], cancellationToken);
    }

    public async Task<SkillAssessment?> GetByDirectReportAndSkillAsync(Guid directReportId, Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments
            .FirstOrDefaultAsync(x => x.DirectReportId == directReportId && x.SkillId == skillId, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments
            .OrderBy(x => x.DirectReportId)
            .ThenBy(x => x.SkillId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessment>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments
            .Where(x => x.DirectReportId == directReportId)
            .OrderBy(x => x.SkillId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessment>> GetBySkillIdAsync(Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments
            .Where(x => x.SkillId == skillId)
            .OrderBy(x => x.DirectReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillAssessment> AddAsync(SkillAssessment assessment, CancellationToken cancellationToken = default)
    {
        _context.SkillAssessments.Add(assessment);
        await _context.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public async Task UpdateAsync(SkillAssessment assessment, CancellationToken cancellationToken = default)
    {
        _context.SkillAssessments.Update(assessment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SkillAssessments.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.SkillAssessments.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> AssessmentExistsAsync(Guid directReportId, Guid skillId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.SkillAssessments
            .AnyAsync(x => x.DirectReportId == directReportId
                        && x.SkillId == skillId
                        && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    }
}
