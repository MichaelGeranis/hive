using Hive.Core.Exceptions;

namespace Hive.Tests.Core.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void DomainException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new DomainException("Test message");

        // Assert
        exception.Message.Should().Be("Test message");
    }

    [Fact]
    public void DomainException_WithInnerException_SetsInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new DomainException("Outer", innerException);

        // Assert
        exception.Message.Should().Be("Outer");
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void NotFoundException_SetsFormattedMessage()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var exception = new NotFoundException("DirectReport", entityId);

        // Assert
        exception.Message.Should().Contain("DirectReport");
        exception.Message.Should().Contain(entityId.ToString());
    }

    [Fact]
    public void NotFoundException_InheritsFromDomainException()
    {
        // Arrange & Act
        var exception = new NotFoundException("Entity", "123");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void ConflictException_SetsMessage()
    {
        // Arrange & Act
        var exception = new ConflictException("Email already exists");

        // Assert
        exception.Message.Should().Be("Email already exists");
    }

    [Fact]
    public void ConflictException_InheritsFromDomainException()
    {
        // Arrange & Act
        var exception = new ConflictException("Conflict");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void ValidationException_WithMessage_SetsMessageAndEmptyErrors()
    {
        // Arrange & Act
        var exception = new ValidationException("Validation failed");

        // Assert
        exception.Message.Should().Be("Validation failed");
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_WithErrors_SetsDefaultMessageAndErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email is invalid" } },
            { "Name", new[] { "Name is required" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Contain("validation errors");
        exception.Errors.Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(2);
        exception.Errors["Name"].Should().HaveCount(1);
    }

    [Fact]
    public void ValidationException_InheritsFromDomainException()
    {
        // Arrange & Act
        var exception = new ValidationException("Validation");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
