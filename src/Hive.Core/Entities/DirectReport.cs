namespace Hive.Core.Entities;

/// <summary>
/// Represents a direct report (team member) under an engineering manager.
/// This is the core domain entity with critical business rules.
/// </summary>
public class DirectReport
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public DateTime HireDate { get; private set; }
    public bool IsDirect { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core / serialization
    private DirectReport() { }

    public DirectReport(
        string firstName,
        string lastName,
        string email,
        string jobTitle,
        string department,
        DateTime hireDate,
        bool isDirect = true)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));
        ValidateEmail(email);

        Id = Guid.NewGuid();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        JobTitle = jobTitle?.Trim() ?? string.Empty;
        Department = department?.Trim() ?? string.Empty;
        HireDate = hireDate;
        IsDirect = isDirect;
        CreatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void Update(
        string firstName,
        string lastName,
        string email,
        string jobTitle,
        string department,
        DateTime hireDate,
        bool isDirect)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));
        ValidateEmail(email);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        JobTitle = jobTitle?.Trim() ?? string.Empty;
        Department = department?.Trim() ?? string.Empty;
        HireDate = hireDate;
        IsDirect = isDirect;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
        }

        if (name.Length > 100)
        {
            throw new ArgumentException($"{parameterName} cannot exceed 100 characters.", parameterName);
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        if (email.Length > 255)
        {
            throw new ArgumentException("Email cannot exceed 255 characters.", nameof(email));
        }
    }
}
