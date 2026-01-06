namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for DirectReport - used for API responses.
/// </summary>
public record DirectReportDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime HireDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new DirectReport.
/// </summary>
public record CreateDirectReportDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime HireDate { get; init; }
}

/// <summary>
/// DTO for updating an existing DirectReport.
/// </summary>
public record UpdateDirectReportDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public DateTime HireDate { get; init; }
}

/// <summary>
/// DTO for bulk importing direct reports from CSV.
/// </summary>
public record BulkImportDirectReportsDto
{
    public string CsvContent { get; init; } = string.Empty;
    public bool SkipDuplicates { get; init; } = true;
}

/// <summary>
/// Result of a bulk import operation.
/// </summary>
public record BulkImportResultDto
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public List<BulkImportRowResult> Results { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Result for individual row in bulk import.
/// </summary>
public record BulkImportRowResult
{
    public int RowNumber { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // "Created", "Skipped", "Error"
    public string? Message { get; init; }
    public DirectReportDto? DirectReport { get; init; }
}
