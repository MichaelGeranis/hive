using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IDocumentRepository.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly InMemoryDbContext _context;

    public DocumentRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Documents.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.Documents.Values
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(entities);
    }

    public Task<IReadOnlyList<Document>> GetByTagsAsync(string tags, CancellationToken cancellationToken = default)
    {
        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var entities = _context.Documents.Values
            .Where(x => tagList.Any(tag => x.Tags.ToLowerInvariant().Contains(tag)))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<Document>>(entities);
    }

    public Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.Documents.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.Documents.ContainsKey(id));
    }
}