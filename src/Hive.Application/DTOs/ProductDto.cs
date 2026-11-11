namespace Hive.Application.DTOs;

/// <summary>
/// A product (a Jira component), with the delivery work that names it and the projects
/// that serve it.
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>The Jira component id; null for a product made by hand.</summary>
    public string? JiraComponentId { get; init; }

    /// <summary>Whether the product comes from Jira. Jira owns its name and description.</summary>
    public bool IsFromJira { get; init; }

    /// <summary>Tasks whose components name the product.</summary>
    public int TotalTasks { get; init; }

    /// <summary>Tasks that are done or cancelled ("Won't Do").</summary>
    public int CompletedTasks { get; init; }
    public int OpenTasks { get; init; }

    /// <summary>Parents whose components name the product.</summary>
    public int ParentCount { get; init; }

    /// <summary>The projects serving the product, by name.</summary>
    public List<ProductProjectDto> Projects { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// A project as listed on a product.
/// </summary>
public record ProductProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// A product as listed on a project.
/// </summary>
public record ProjectProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// DTO for creating a product by hand.
/// </summary>
public record CreateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// DTO for editing a product made by hand.
/// </summary>
public record UpdateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
