namespace Hive.Core.Entities;

public class ChecklistTemplateItem
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public int SortOrder { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ChecklistItemType ItemType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? HelpText { get; private set; }
    public int? EstimatedMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ChecklistTemplateItem() { }

    public ChecklistTemplateItem(
        Guid templateId,
        int sortOrder,
        string content,
        ChecklistItemType itemType,
        bool isRequired = true,
        string? helpText = null,
        int? estimatedMinutes = null)
    {
        ValidateTemplateId(templateId);
        ValidateContent(content);
        ValidateSortOrder(sortOrder);

        Id = Guid.NewGuid();
        TemplateId = templateId;
        SortOrder = sortOrder;
        Content = content.Trim();
        ItemType = itemType;
        IsRequired = isRequired;
        HelpText = helpText?.Trim();
        EstimatedMinutes = estimatedMinutes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        int sortOrder,
        string content,
        ChecklistItemType itemType,
        bool isRequired,
        string? helpText,
        int? estimatedMinutes)
    {
        ValidateContent(content);
        ValidateSortOrder(sortOrder);

        SortOrder = sortOrder;
        Content = content.Trim();
        ItemType = itemType;
        IsRequired = isRequired;
        HelpText = helpText?.Trim();
        EstimatedMinutes = estimatedMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        ValidateSortOrder(sortOrder);
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTemplateId(Guid templateId)
    {
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Item content cannot be empty.", nameof(content));
        if (content.Length > 2000)
            throw new ArgumentException("Item content cannot exceed 2000 characters.", nameof(content));
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
    }
}
