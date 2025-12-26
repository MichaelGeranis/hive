using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class DirectReportTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesDirectReport()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@company.com";
        var jobTitle = "Software Engineer";
        var department = "Engineering";
        var hireDate = new DateTime(2023, 1, 15);

        // Act
        var directReport = new DirectReport(firstName, lastName, email, jobTitle, department, hireDate);

        // Assert
        directReport.Id.Should().NotBeEmpty();
        directReport.FirstName.Should().Be(firstName);
        directReport.LastName.Should().Be(lastName);
        directReport.Email.Should().Be(email.ToLowerInvariant());
        directReport.JobTitle.Should().Be(jobTitle);
        directReport.Department.Should().Be(department);
        directReport.HireDate.Should().Be(hireDate);
        directReport.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        directReport.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var directReport = new DirectReport(
            "  John  ",
            "  Doe  ",
            "  john.doe@company.com  ",
            "  Engineer  ",
            "  Engineering  ",
            DateTime.Now);

        // Assert
        directReport.FirstName.Should().Be("John");
        directReport.LastName.Should().Be("Doe");
        directReport.Email.Should().Be("john.doe@company.com");
        directReport.JobTitle.Should().Be("Engineer");
        directReport.Department.Should().Be("Engineering");
    }

    [Fact]
    public void FullName_ReturnsCorrectFormat()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);

        // Act & Assert
        directReport.FullName.Should().Be("John Doe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyFirstName_ThrowsArgumentException(string? firstName)
    {
        // Act
        var act = () => new DirectReport(firstName!, "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("firstName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyLastName_ThrowsArgumentException(string? lastName)
    {
        // Act
        var act = () => new DirectReport("John", lastName!, "john@test.com", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("lastName");
    }

    [Fact]
    public void Constructor_WithFirstNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => new DirectReport(longName, "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("firstName");
    }

    [Fact]
    public void Constructor_WithLastNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act
        var act = () => new DirectReport("John", longName, "john@test.com", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("lastName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException(string? email)
    {
        // Act
        var act = () => new DirectReport("John", "Doe", email!, "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid")]
    [InlineData("invalid.com")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string email)
    {
        // Act
        var act = () => new DirectReport("John", "Doe", email, "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Fact]
    public void Constructor_WithEmailTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com";

        // Act
        var act = () => new DirectReport("John", "Doe", longEmail, "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Fact]
    public void Constructor_WithNullJobTitle_DefaultsToEmpty()
    {
        // Act
        var directReport = new DirectReport("John", "Doe", "john@test.com", null!, "Eng", DateTime.Now);

        // Assert
        directReport.JobTitle.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullDepartment_DefaultsToEmpty()
    {
        // Act
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Dev", null!, DateTime.Now);

        // Assert
        directReport.Department.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);
        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newEmail = "jane.smith@company.com";
        var newJobTitle = "Senior Engineer";
        var newDepartment = "Platform";
        var newHireDate = new DateTime(2022, 5, 1);

        // Act
        directReport.Update(newFirstName, newLastName, newEmail, newJobTitle, newDepartment, newHireDate);

        // Assert
        directReport.FirstName.Should().Be(newFirstName);
        directReport.LastName.Should().Be(newLastName);
        directReport.Email.Should().Be(newEmail.ToLowerInvariant());
        directReport.JobTitle.Should().Be(newJobTitle);
        directReport.Department.Should().Be(newDepartment);
        directReport.HireDate.Should().Be(newHireDate);
        directReport.UpdatedAt.Should().NotBeNull();
        directReport.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithEmptyFirstName_ThrowsArgumentException()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);

        // Act
        var act = () => directReport.Update("", "Smith", "jane@test.com", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("firstName");
    }

    [Fact]
    public void Update_WithInvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var directReport = new DirectReport("John", "Doe", "john@test.com", "Dev", "Eng", DateTime.Now);

        // Act
        var act = () => directReport.Update("Jane", "Smith", "invalid", "Dev", "Eng", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }

    [Fact]
    public void Email_IsNormalizedToLowercase()
    {
        // Arrange & Act
        var directReport = new DirectReport("John", "Doe", "JOHN@TEST.COM", "Dev", "Eng", DateTime.Now);

        // Assert
        directReport.Email.Should().Be("john@test.com");
    }
}
