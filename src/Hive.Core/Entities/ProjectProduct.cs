namespace Hive.Core.Entities;

/// <summary>
/// A project serving a product. A project can serve several products, and a product can be
/// served by several projects.
/// </summary>
public class ProjectProduct
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProjectProduct() { }

    public ProjectProduct(Guid projectId, Guid productId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("A project is required.", nameof(projectId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("A product is required.", nameof(productId));
        }

        Id = Guid.NewGuid();
        ProjectId = projectId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;
    }
}
