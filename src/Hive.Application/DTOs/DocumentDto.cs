using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for Document.
/// </summary>
public record DocumentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string Tags { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new document.
/// </summary>
public record CreateDocumentDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? Tags { get; init; }
}

/// <summary>
/// DTO for updating a document.
/// </summary>
public record UpdateDocumentDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string? Tags { get; init; }
}