using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class SkillServiceTests
{
    private readonly Mock<ISkillRepository> _repositoryMock;
    private readonly Mock<ISkillCategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IActivityService> _activityServiceMock;
    private readonly SkillService _service;
    private readonly Guid _testCategoryId = Guid.NewGuid();

    public SkillServiceTests()
    {
        _repositoryMock = new Mock<ISkillRepository>();
        _categoryRepositoryMock = new Mock<ISkillCategoryRepository>();
        _activityServiceMock = new Mock<IActivityService>();
        _service = new SkillService(_repositoryMock.Object, _categoryRepositoryMock.Object, _activityServiceMock.Object);

        // Setup default category lookup
        _categoryRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => SkillCategoryEntity.CreateWithId(id, "Technical", "Technical skills", 0));
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillService(null!, _categoryRepositoryMock.Object, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullCategoryRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillService(_repositoryMock.Object, null!, _activityServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("categoryRepository");
    }

    [Fact]
    public void Constructor_WithNullActivityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillService(_repositoryMock.Object, _categoryRepositoryMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activityService");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new Skill("C#", "Programming language", _testCategoryId);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("C#");
        result.Description.Should().Be("Programming language");
        result.CategoryId.Should().Be(_testCategoryId);
        result.CategoryName.Should().Be("Technical");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_IncludeInactiveFalse_ReturnsActiveSkills()
    {
        // Arrange
        var entities = new List<Skill>
        {
            new Skill("C#", "Programming language", _testCategoryId),
            new Skill("Python", "Programming language", _testCategoryId)
        };
        _repositoryMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _categoryRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillCategoryEntity>
            {
                SkillCategoryEntity.CreateWithId(_testCategoryId, "Technical", "Technical skills", 0)
            });

        // Act
        var result = await _service.GetAllAsync(false);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_IncludeInactiveTrue_ReturnsAllSkills()
    {
        // Arrange
        var entities = new List<Skill>
        {
            new Skill("C#", "Programming language", _testCategoryId),
            new Skill("Java", "Programming language", _testCategoryId)
        };
        entities[1].Deactivate();
        _repositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _categoryRepositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SkillCategoryEntity>
            {
                SkillCategoryEntity.CreateWithId(_testCategoryId, "Technical", "Technical skills", 0)
            });

        // Act
        var result = await _service.GetAllAsync(true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCategoryIdAsync_ReturnsCategorySkills()
    {
        // Arrange
        var softSkillsCategoryId = Guid.NewGuid();
        var entities = new List<Skill>
        {
            new Skill("Communication", "Effective communication", softSkillsCategoryId),
            new Skill("Teamwork", "Team collaboration", softSkillsCategoryId)
        };
        _repositoryMock.Setup(r => r.GetByCategoryIdAsync(softSkillsCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _categoryRepositoryMock.Setup(r => r.GetByIdAsync(softSkillsCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SkillCategoryEntity.CreateWithId(softSkillsCategoryId, "Soft Skills", "Soft skills", 1));

        // Act
        var result = await _service.GetByCategoryIdAsync(softSkillsCategoryId);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.CategoryId == softSkillsCategoryId).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSkill()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "TypeScript",
            Description = "Typed JavaScript",
            CategoryId = _testCategoryId
        };
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill skill, CancellationToken _) => skill);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(_testCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TypeScript");
        result.Description.Should().Be("Typed JavaScript");
        result.CategoryId.Should().Be(_testCategoryId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameExists_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "C#",
            Description = "Programming language",
            CategoryId = _testCategoryId
        };
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WhenSkillExists_UpdatesSkill()
    {
        // Arrange
        var toolsCategoryId = Guid.NewGuid();
        var entity = new Skill("Old Name", "Old description", _testCategoryId);
        var dto = new UpdateSkillDto
        {
            Name = "New Name",
            Description = "New description",
            CategoryId = toolsCategoryId
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categoryRepositoryMock.Setup(r => r.GetByIdAsync(toolsCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SkillCategoryEntity.CreateWithId(toolsCategoryId, "Tools", "Tool skills", 4));
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(toolsCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(entity.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New description");
        result.CategoryId.Should().Be(toolsCategoryId);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSkillNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new UpdateSkillDto { Name = "Updated Skill" };
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenNameExistsForOtherSkill_ThrowsConflictException()
    {
        // Arrange
        var entity = new Skill("Old Name", "Description", _testCategoryId);
        var dto = new UpdateSkillDto
        {
            Name = "Existing Name",
            Description = "Description",
            CategoryId = _testCategoryId
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.UpdateAsync(entity.Id, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ActivateAsync_WhenSkillExists_ActivatesSkill()
    {
        // Arrange
        var entity = new Skill("Test Skill", "Description", _testCategoryId);
        entity.Deactivate();
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.ActivateAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WhenSkillNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = async () => await _service.ActivateAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_WhenSkillExists_DeactivatesSkill()
    {
        // Arrange
        var entity = new Skill("Test Skill", "Description", _testCategoryId);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.DeactivateAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenSkillNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = async () => await _service.DeactivateAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenSkillExists_DeletesSkill()
    {
        // Arrange
        var entity = new Skill("Test Skill", "Description", _testCategoryId);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSkillNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
