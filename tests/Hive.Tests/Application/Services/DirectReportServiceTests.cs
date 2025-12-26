using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class DirectReportServiceTests
{
    private readonly Mock<IDirectReportRepository> _repositoryMock;
    private readonly DirectReportService _service;

    public DirectReportServiceTests()
    {
        _repositoryMock = new Mock<IDirectReportRepository>();
        _service = new DirectReportService(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DirectReportService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
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
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.DeleteAsync(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

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
