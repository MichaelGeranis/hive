using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class DirectReportServiceTests
{
    private readonly Mock<IDirectReportRepository> _repositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly DirectReportService _service;

    public DirectReportServiceTests()
    {
        _repositoryMock = new Mock<IDirectReportRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new DirectReportService(_repositoryMock.Object, _activityServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DirectReportService(null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DirectReportService(_repositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = CreateDirectReport();
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.FirstName.Should().Be(entity.FirstName);
        result.LastName.Should().Be(entity.LastName);
        result.Email.Should().Be(entity.Email);
        result.FullName.Should().Be(entity.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        // Arrange
        var entities = new List<DirectReport>
        {
            CreateDirectReport("John", "Doe"),
            CreateDirectReport("Jane", "Smith")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("John");
        result[1].FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DirectReport>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAndReturnsDto()
    {
        // Arrange
        var dto = new CreateDirectReportDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            JobTitle = "Engineer",
            Department = "Engineering",
            HireDate = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be(dto.FirstName);
        result.LastName.Should().Be(dto.LastName);
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateDirectReportDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@test.com",
            JobTitle = "Engineer",
            Department = "Engineering",
            HireDate = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsDto()
    {
        // Arrange
        var entity = CreateDirectReport();
        var dto = new UpdateDirectReportDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@test.com",
            JobTitle = "Senior Engineer",
            Department = "Platform",
            HireDate = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.EmailExistsAsync(dto.Email, entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(entity.Id, dto);

        // Assert
        result.FirstName.Should().Be(dto.FirstName);
        result.LastName.Should().Be(dto.LastName);
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        _repositoryMock.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        var dto = new UpdateDirectReportDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@test.com",
            JobTitle = "Engineer",
            Department = "Eng",
            HireDate = DateTime.UtcNow
        };

        // Act
        var act = () => _service.UpdateAsync(id, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var entity = CreateDirectReport();
        var dto = new UpdateDirectReportDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "existing@test.com",
            JobTitle = "Engineer",
            Department = "Eng",
            HireDate = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.EmailExistsAsync(dto.Email, entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.UpdateAsync(entity.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesEntity()
    {
        // Arrange
        var entity = CreateDirectReport();
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport?)null);

        // Act
        var act = () => _service.DeleteAsync(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region BulkImportAsync Tests

    [Fact]
    public async Task BulkImportAsync_WithEmptyCsvContent_ReturnsErrorResult()
    {
        // Arrange
        var dto = new BulkImportDirectReportsDto { CsvContent = "" };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("CSV content is empty");
    }

    [Fact]
    public async Task BulkImportAsync_WithWhitespaceCsvContent_ReturnsErrorResult()
    {
        // Arrange
        var dto = new BulkImportDirectReportsDto { CsvContent = "   " };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("CSV content is empty");
    }

    [Fact]
    public async Task BulkImportAsync_WithOnlyHeaderRow_ReturnsErrorResult()
    {
        // Arrange
        var dto = new BulkImportDirectReportsDto { CsvContent = "FirstName,LastName,Email" };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().Contain("CSV must have a header row and at least one data row");
    }

    [Fact]
    public async Task BulkImportAsync_WithMissingRequiredColumns_ReturnsErrorResult()
    {
        // Arrange
        var dto = new BulkImportDirectReportsDto { CsvContent = "FirstName,LastName\nJohn,Doe" };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().ContainMatch("*Missing required columns*Email*");
    }

    [Fact]
    public async Task BulkImportAsync_WithValidCsv_CreatesDirectReports()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,JobTitle,Department,HireDate,IsDirect\nJohn,Doe,john@test.com,Engineer,Engineering,2024-01-15,true";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.Results.Should().HaveCount(1);
        result.Results[0].Status.Should().Be("Created");
        result.Results[0].Email.Should().Be("john@test.com");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkImportAsync_WithMinimalRequiredFields_CreatesDirectReport()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJane,Smith,jane@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.Results[0].Status.Should().Be("Created");
    }

    [Fact]
    public async Task BulkImportAsync_WithMultipleRows_CreatesAllDirectReports()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@test.com\nJane,Smith,jane@test.com\nBob,Wilson,bob@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BulkImportAsync_WithDuplicateEmail_AndSkipDuplicatesTrue_SkipsRow()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,existing@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv, SkipDuplicates = true };

        _repositoryMock.Setup(r => r.EmailExistsAsync("existing@test.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.Results[0].Status.Should().Be("Skipped");
        result.Results[0].Message.Should().Contain("Email already exists");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkImportAsync_WithDuplicateEmail_AndSkipDuplicatesFalse_ReturnsError()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,existing@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv, SkipDuplicates = false };

        _repositoryMock.Setup(r => r.EmailExistsAsync("existing@test.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Results[0].Status.Should().Be("Error");
        result.Results[0].Message.Should().Contain("Email already exists");
    }

    [Fact]
    public async Task BulkImportAsync_WithEmptyRequiredFields_ReturnsError()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\n,,";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Results[0].Status.Should().Be("Error");
        result.Results[0].Message.Should().Contain("FirstName, LastName, and Email are required");
    }

    [Fact]
    public async Task BulkImportAsync_WithMissingFirstName_ReturnsError()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\n,Doe,john@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.ErrorCount.Should().Be(1);
        result.Results[0].Status.Should().Be("Error");
        result.Results[0].Message.Should().Contain("FirstName, LastName, and Email are required");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("no", false)]
    [InlineData("0", false)]
    [InlineData("", true)] // Default is true when not specified
    public async Task BulkImportAsync_ParsesIsDirectCorrectly(string isDirectValue, bool expectedIsDirect)
    {
        // Arrange
        var csv = $"FirstName,LastName,Email,IsDirect\nJohn,Doe,john@test.com,{isDirectValue}";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        await _service.BulkImportAsync(dto);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.IsDirect.Should().Be(expectedIsDirect);
    }

    [Fact]
    public async Task BulkImportAsync_WithValidDate_ParsesCorrectly()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,HireDate\nJohn,Doe,john@test.com,2024-06-15";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        await _service.BulkImportAsync(dto);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.HireDate.Should().Be(new DateTime(2024, 6, 15));
    }

    [Fact]
    public async Task BulkImportAsync_WithInvalidDate_DefaultsToToday()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,HireDate\nJohn,Doe,john@test.com,invalid-date";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        await _service.BulkImportAsync(dto);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.HireDate.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task BulkImportAsync_WithEmptyDate_DefaultsToToday()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,HireDate\nJohn,Doe,john@test.com,";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        await _service.BulkImportAsync(dto);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.HireDate.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task BulkImportAsync_WithQuotedValues_ParsesCorrectly()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,JobTitle\n\"John\",\"Doe\",\"john@test.com\",\"Senior Engineer\"";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.SuccessCount.Should().Be(1);
        capturedEntity.Should().NotBeNull();
        capturedEntity!.FirstName.Should().Be("John");
        capturedEntity.LastName.Should().Be("Doe");
        capturedEntity.JobTitle.Should().Be("Senior Engineer");
    }

    [Fact]
    public async Task BulkImportAsync_WithCommaInQuotedValue_ParsesCorrectly()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,JobTitle\nJohn,Doe,john@test.com,\"Engineer, Senior\"";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.SuccessCount.Should().Be(1);
        capturedEntity!.JobTitle.Should().Be("Engineer, Senior");
    }

    [Fact]
    public async Task BulkImportAsync_WithEscapedQuotes_ParsesCorrectly()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,JobTitle\nJohn,Doe,john@test.com,\"The \"\"Best\"\" Engineer\"";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        DirectReport? capturedEntity = null;
        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .Callback<DirectReport, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.SuccessCount.Should().Be(1);
        capturedEntity!.JobTitle.Should().Be("The \"Best\" Engineer");
    }

    [Fact]
    public async Task BulkImportAsync_WithCaseInsensitiveHeaders_ParsesCorrectly()
    {
        // Arrange
        var csv = "FIRSTNAME,lastname,EMAIL\nJohn,Doe,john@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task BulkImportAsync_WithWindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\r\nJohn,Doe,john@test.com\r\nJane,Smith,jane@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task BulkImportAsync_WithMixedSuccessAndErrors_ReturnsCorrectCounts()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@test.com\n,,\nJane,Smith,jane@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(3);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(1);
        result.Results.Should().HaveCount(3);
        result.Results.Count(r => r.Status == "Created").Should().Be(2);
        result.Results.Count(r => r.Status == "Error").Should().Be(1);
    }

    [Fact]
    public async Task BulkImportAsync_RowNumbersAreCorrect()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@test.com\nJane,Smith,jane@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.Results[0].RowNumber.Should().Be(2); // First data row is row 2 (after header)
        result.Results[1].RowNumber.Should().Be(3);
    }

    [Fact]
    public async Task BulkImportAsync_WhenRepositoryThrows_ReturnsErrorForRow()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@test.com";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.TotalRows.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Results[0].Status.Should().Be("Error");
        result.Results[0].Message.Should().Contain("Database error");
    }

    [Fact]
    public async Task BulkImportAsync_WithExtraColumnsInCsv_IgnoresExtras()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,ExtraColumn,AnotherExtra\nJohn,Doe,john@test.com,value1,value2";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task BulkImportAsync_ReturnedDirectReportDto_HasCorrectValues()
    {
        // Arrange
        var csv = "FirstName,LastName,Email,JobTitle,Department\nJohn,Doe,john@test.com,Engineer,Engineering";
        var dto = new BulkImportDirectReportsDto { CsvContent = csv };

        _repositoryMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DirectReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DirectReport entity, CancellationToken _) => entity);

        // Act
        var result = await _service.BulkImportAsync(dto);

        // Assert
        result.Results[0].DirectReport.Should().NotBeNull();
        result.Results[0].DirectReport!.FirstName.Should().Be("John");
        result.Results[0].DirectReport!.LastName.Should().Be("Doe");
        result.Results[0].DirectReport!.Email.Should().Be("john@test.com");
        result.Results[0].DirectReport!.JobTitle.Should().Be("Engineer");
        result.Results[0].DirectReport!.Department.Should().Be("Engineering");
    }

    #endregion

    private static DirectReport CreateDirectReport(string firstName = "John", string lastName = "Doe")
    {
        return new DirectReport(
            firstName,
            lastName,
            $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            "Software Engineer",
            "Engineering",
            DateTime.UtcNow.AddYears(-1));
    }
}
