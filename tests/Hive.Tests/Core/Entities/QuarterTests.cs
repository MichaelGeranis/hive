using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class QuarterTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesQuarter()
    {
        // Arrange
        var year = 2024;
        var quarterNumber = 2;
        var okrReference = "OKR-2024-Q2";

        // Act
        var quarter = new Quarter(year, quarterNumber, okrReference);

        // Assert
        quarter.Id.Should().NotBeEmpty();
        quarter.Year.Should().Be(year);
        quarter.QuarterNumber.Should().Be(quarterNumber);
        quarter.Name.Should().Be("Q2 2024");
        quarter.Status.Should().Be(QuarterStatus.Planning);
        quarter.OkrReference.Should().Be(okrReference);
        quarter.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        quarter.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutOkrReference_DefaultsToEmpty()
    {
        // Act
        var quarter = new Quarter(2024, 1);

        // Assert
        quarter.OkrReference.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsOkrReference()
    {
        // Act
        var quarter = new Quarter(2024, 1, "  OKR Reference  ");

        // Assert
        quarter.OkrReference.Should().Be("OKR Reference");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Constructor_WithInvalidYear_ThrowsArgumentException(int year)
    {
        // Act
        var act = () => new Quarter(year, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("year");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Constructor_WithInvalidQuarterNumber_ThrowsArgumentException(int quarterNumber)
    {
        // Act
        var act = () => new Quarter(2024, quarterNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("quarterNumber");
    }

    [Theory]
    [InlineData(1, "Q1 2024")]
    [InlineData(2, "Q2 2024")]
    [InlineData(3, "Q3 2024")]
    [InlineData(4, "Q4 2024")]
    public void Constructor_GeneratesCorrectName(int quarterNumber, string expectedName)
    {
        // Act
        var quarter = new Quarter(2024, quarterNumber);

        // Assert
        quarter.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Update_UpdatesOkrReference()
    {
        // Arrange
        var quarter = new Quarter(2024, 1, "Old OKR");

        // Act
        quarter.Update("New OKR Reference");

        // Assert
        quarter.OkrReference.Should().Be("New OKR Reference");
        quarter.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithNullOkrReference_ClearsOkrReference()
    {
        // Arrange
        var quarter = new Quarter(2024, 1, "Some OKR");

        // Act
        quarter.Update(null);

        // Assert
        quarter.OkrReference.Should().BeEmpty();
    }

    [Fact]
    public void Activate_FromPlanning_ChangesToActive()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);

        // Act
        quarter.Activate();

        // Assert
        quarter.Status.Should().Be(QuarterStatus.Active);
        quarter.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);
        quarter.Activate();
        quarter.Complete();

        // Act
        var act = () => quarter.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromActive_ChangesToCompleted()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);
        quarter.Activate();

        // Act
        quarter.Complete();

        // Assert
        quarter.Status.Should().Be(QuarterStatus.Completed);
        quarter.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromPlanning_ThrowsInvalidOperationException()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);

        // Act
        var act = () => quarter.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResetToPlanning_ChangesStatusToPlanning()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);
        quarter.Activate();

        // Act
        quarter.ResetToPlanning();

        // Assert
        quarter.Status.Should().Be(QuarterStatus.Planning);
        quarter.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ResetToPlanning_FromCompleted_ChangesToPlanning()
    {
        // Arrange
        var quarter = new Quarter(2024, 1);
        quarter.Activate();
        quarter.Complete();

        // Act
        quarter.ResetToPlanning();

        // Assert
        quarter.Status.Should().Be(QuarterStatus.Planning);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2024)]
    [InlineData(2100)]
    public void Constructor_WithBoundaryYears_CreatesQuarter(int year)
    {
        // Act
        var quarter = new Quarter(year, 1);

        // Assert
        quarter.Year.Should().Be(year);
    }
}
