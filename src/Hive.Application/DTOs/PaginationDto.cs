namespace Hive.Application.DTOs;

/// <summary>
/// Parameters for paginated queries.
/// </summary>
public record PaginationParams
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    public int PageNumber { get; init; } = 1;

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? DefaultPageSize : value;
    }

    public int Skip => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Filter options for notes.
/// </summary>
public enum NoteFilter
{
    All,
    Pending,
    Completed,
    Overdue
}

/// <summary>
/// Filter options for tasks.
/// </summary>
public enum TaskFilter
{
    All,
    Overdue
    // Individual statuses are handled via TaskStatus enum
}

/// <summary>
/// Parameters for paginated and filtered task queries.
/// </summary>
public record TaskPaginationParams : PaginationParams
{
    public TaskFilter Filter { get; init; } = TaskFilter.All;
    public Hive.Core.Entities.TaskStatus? Status { get; init; }
    public string? SearchTerm { get; init; }
    public string? Label { get; init; }
    public string? Sprint { get; init; }
}

/// <summary>
/// Parameters for paginated and filtered note queries.
/// </summary>
public record NotePaginationParams : PaginationParams
{
    public NoteFilter Filter { get; init; } = NoteFilter.All;
    public string? SearchTerm { get; init; }
    public string? Tag { get; init; }
}

/// <summary>
/// Wrapper for paginated results.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, PaginationParams pagination)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }
}
