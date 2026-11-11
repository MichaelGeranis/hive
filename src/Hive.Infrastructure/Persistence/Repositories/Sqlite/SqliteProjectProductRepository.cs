using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteProjectProductRepository : IProjectProductRepository
{
    private readonly HiveDbContext _context;

    public SqliteProjectProductRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ProjectProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProjectProducts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectProduct>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectProducts
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectProduct>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectProducts
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectProduct> AddAsync(ProjectProduct link, CancellationToken cancellationToken = default)
    {
        if (await _context.ProjectProducts.AnyAsync(x => x.ProjectId == link.ProjectId && x.ProductId == link.ProductId, cancellationToken))
        {
            throw new InvalidOperationException("The project already serves this product.");
        }

        _context.ProjectProducts.Add(link);
        await _context.SaveChangesAsync(cancellationToken);
        return link;
    }

    public async Task DeleteByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var links = await _context.ProjectProducts
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        _context.ProjectProducts.RemoveRange(links);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var links = await _context.ProjectProducts
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        _context.ProjectProducts.RemoveRange(links);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
