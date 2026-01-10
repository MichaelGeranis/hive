namespace Hive.Core.Entities;

public class ChecklistInstanceItem
{
    public Guid Id { get; private set; }
    public Guid InstanceId { get; private set; }
    public Guid TemplateItemId { get; private set; }
    public int SortOrder { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ChecklistItemType ItemType { get; private set; }
    public bool IsRequired { get; private set; }
    public ChecklistItemStatus Status { get; private set; }

    // Completion tracking
    public string Notes { get; private set; } = string.Empty;
    public int? Score { get; private set; }
    public string? Assignee { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ChecklistInstanceItem() { }

    public ChecklistInstanceItem(
        Guid instanceId,
        Guid templateItemId,
        int sortOrder,
        string content,
        ChecklistItemType itemType,
        bool isRequired)
    {
        ValidateInstanceId(instanceId);

        Id = Guid.NewGuid();
        InstanceId = instanceId;
        TemplateItemId = templateItemId;
        SortOrder = sortOrder;
        Content = content;
        ItemType = itemType;
        IsRequired = isRequired;
        Status = ChecklistItemStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkInProgress()
    {
        if (Status == ChecklistItemStatus.Completed)
            throw new InvalidOperationException("Cannot change status of a completed item.");

        Status = ChecklistItemStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkComplete(string? notes = null, int? score = null)
    {
        Status = ChecklistItemStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        if (notes != null)
            Notes = notes.Trim();

        if (score.HasValue)
            SetScore(score.Value);

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSkipped(string? notes = null)
    {
        if (IsRequired)
            throw new InvalidOperationException("Cannot skip a required item.");

        Status = ChecklistItemStatus.Skipped;

        if (notes != null)
            Notes = notes.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNotApplicable(string? notes = null)
    {
        Status = ChecklistItemStatus.NotApplicable;

        if (notes != null)
            Notes = notes.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAssignee(string? assignee, DateTime? dueDate = null)
    {
        Assignee = assignee?.Trim();
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetScore(int score)
    {
        if (score < 1 || score > 5)
            throw new ArgumentException("Score must be between 1 and 5.", nameof(score));

        Score = score;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue()
    {
        return DueDate.HasValue
            && DueDate < DateTime.UtcNow
            && Status != ChecklistItemStatus.Completed
            && Status != ChecklistItemStatus.Skipped
            && Status != ChecklistItemStatus.NotApplicable;
    }

    private static void ValidateInstanceId(Guid instanceId)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("Instance ID cannot be empty.", nameof(instanceId));
    }
}
