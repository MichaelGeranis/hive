namespace Hive.Core.Entities;

public class ChecklistInstance
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public ChecklistType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ChecklistInstanceStatus Status { get; private set; }

    // Interview-specific fields
    public string? CandidateName { get; private set; }
    public string? Position { get; private set; }
    public DateTime? InterviewDate { get; private set; }

    // Onboarding-specific fields
    public string? NewHireName { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? TargetCompletionDate { get; private set; }

    public string Notes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ChecklistInstance() { }

    public static ChecklistInstance CreateInterview(
        Guid templateId,
        string title,
        string candidateName,
        string position,
        DateTime interviewDate)
    {
        ValidateTemplateId(templateId);
        ValidateTitle(title);

        return new ChecklistInstance
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Type = ChecklistType.Interview,
            Title = title.Trim(),
            Status = ChecklistInstanceStatus.NotStarted,
            CandidateName = candidateName?.Trim(),
            Position = position?.Trim(),
            InterviewDate = interviewDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ChecklistInstance CreateOnboarding(
        Guid templateId,
        string title,
        string newHireName,
        DateTime startDate,
        DateTime? targetCompletionDate = null)
    {
        ValidateTemplateId(templateId);
        ValidateTitle(title);

        return new ChecklistInstance
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Type = ChecklistType.Onboarding,
            Title = title.Trim(),
            Status = ChecklistInstanceStatus.NotStarted,
            NewHireName = newHireName?.Trim(),
            StartDate = startDate,
            TargetCompletionDate = targetCompletionDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status == ChecklistInstanceStatus.Completed || Status == ChecklistInstanceStatus.Cancelled)
            throw new InvalidOperationException("Cannot start a completed or cancelled checklist.");

        Status = ChecklistInstanceStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == ChecklistInstanceStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled checklist.");

        Status = ChecklistInstanceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ChecklistInstanceStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed checklist.");

        Status = ChecklistInstanceStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        ValidateTitle(title);
        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTemplateId(Guid templateId)
    {
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (title.Length > 300)
            throw new ArgumentException("Title cannot exceed 300 characters.", nameof(title));
    }
}
