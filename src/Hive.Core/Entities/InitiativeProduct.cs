namespace Hive.Core.Entities;

/// <summary>
/// An initiative touching a product. Through the product's projects it tells who knows the
/// code, and through its tasks who has worked on it.
/// </summary>
public class InitiativeProduct
{
    public Guid Id { get; private set; }
    public Guid InitiativeId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InitiativeProduct() { }

    public InitiativeProduct(Guid initiativeId, Guid productId)
    {
        if (initiativeId == Guid.Empty)
        {
            throw new ArgumentException("An initiative is required.", nameof(initiativeId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("A product is required.", nameof(productId));
        }

        Id = Guid.NewGuid();
        InitiativeId = initiativeId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;
    }
}
