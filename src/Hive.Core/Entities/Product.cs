namespace Hive.Core.Entities;

/// <summary>
/// A product the team builds and runs. In Jira a product is a component: tasks and parents
/// name the products they belong to in their components, and that is the only link between
/// a product and delivery work. Projects (code repositories) serve products through
/// <see cref="ProjectProduct"/>.
/// </summary>
public class Product
{
    private const int MaxNameLength = 255;
    private const int MaxDescriptionLength = 4000;
    private const int MaxJiraComponentIdLength = 50;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The id of the Jira component this product is, e.g. <c>10042</c>. Null for a product
    /// created by hand. Once set it is the product's identity for the Jira sync, so renaming
    /// the component in Jira renames the product instead of creating a second one.
    /// </summary>
    public string? JiraComponentId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsFromJira => JiraComponentId is not null;

    private Product() { }

    public Product(string name, string? description = null)
    {
        ValidateName(name);
        ValidateDescription(description);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates the product for a Jira component Hive has not seen before.
    /// </summary>
    public static Product FromJira(string componentId, string name, string? description)
    {
        var product = new Product(name, description);
        product.JiraComponentId = NormalizeComponentId(componentId);
        return product;
    }

    /// <summary>
    /// Edits a product made by hand. A product from Jira is edited in Jira: the sync owns its
    /// name and description.
    /// </summary>
    public void Update(string name, string? description)
    {
        if (IsFromJira)
        {
            throw new InvalidOperationException($"Product '{Name}' comes from Jira. Edit the component in Jira instead.");
        }

        ValidateName(name);
        ValidateDescription(description);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Brings the product in line with its Jira component. A product never changes to
    /// another component. A product made by hand is linked the first time it is synced.
    /// </summary>
    public void SyncFromJira(string componentId, string name, string? description)
    {
        var id = NormalizeComponentId(componentId);
        ValidateName(name);
        ValidateDescription(description);

        if (JiraComponentId is not null && JiraComponentId != id)
        {
            throw new InvalidOperationException($"Product '{Name}' is Jira component {JiraComponentId}, not {id}.");
        }

        JiraComponentId = id;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Whether syncing from Jira would change anything.
    /// </summary>
    public bool MatchesJira(string componentId, string name, string? description) =>
        JiraComponentId == NormalizeComponentId(componentId) &&
        Name == name.Trim() &&
        Description == (description?.Trim() ?? string.Empty);

    /// <summary>
    /// Whether a comma-separated component list, as a task or parent carries it, names this
    /// product. Component names are compared ignoring case and surrounding spaces.
    /// </summary>
    public bool IsNamedIn(string? components) =>
        !string.IsNullOrWhiteSpace(components) &&
        components
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(component => string.Equals(component, Name, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeComponentId(string componentId)
    {
        var id = componentId?.Trim() ?? string.Empty;
        if (id.Length == 0 || id.Length > MaxJiraComponentIdLength)
        {
            throw new ArgumentException(
                $"A Jira component id must be 1 to {MaxJiraComponentIdLength} characters.", nameof(componentId));
        }

        return id;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Product name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        if (name.Contains(','))
        {
            // Tasks list their components separated by commas, so a comma would split the name
            throw new ArgumentException("Product name cannot contain a comma.", nameof(name));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description?.Trim().Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Product description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }
    }
}
