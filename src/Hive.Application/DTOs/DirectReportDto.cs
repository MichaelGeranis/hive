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
