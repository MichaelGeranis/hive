using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class SkillServiceTests
{
    private readonly Mock<ISkillRepository> _repositoryMock;
    private readonly SkillService _service;

    public SkillServiceTests()
    {
        _repositoryMock = new Mock<ISkillRepository>();
        _service = new SkillService(_repositoryMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SkillService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var entity = new Skill("C#", "Programming language", SkillCategory.Technical);
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("C#");
        result.Description.Should().Be("Programming language");
        result.Category.Should().Be(SkillCategory.Technical);
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
            new Skill("C#", "Programming language", SkillCategory.Technical),
            new Skill("Python", "Programming language", SkillCategory.Technical)
        };
        _repositoryMock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

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
            new Skill("C#", "Programming language", SkillCategory.Technical),
            new Skill("Java", "Programming language", SkillCategory.Technical)
        };
        entities[1].Deactivate();
        _repositoryMock.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync(true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsCategorySkills()
    {
        // Arrange
        var entities = new List<Skill>
        {
            new Skill("Communication", "Effective communication", SkillCategory.SoftSkills),
            new Skill("Teamwork", "Team collaboration", SkillCategory.SoftSkills)
        };
        _repositoryMock.Setup(r => r.GetByCategoryAsync(SkillCategory.SoftSkills, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByCategoryAsync(SkillCategory.SoftSkills);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.Category == SkillCategory.SoftSkills).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSkill()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "TypeScript",
            Description = "Typed JavaScript",
            Category = SkillCategory.Technical
        };
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill skill, CancellationToken _) => skill);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TypeScript");
        result.Description.Should().Be("Typed JavaScript");
        result.Category.Should().Be(SkillCategory.Technical);
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
            Category = SkillCategory.Technical
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
        var entity = new Skill("Old Name", "Old description", SkillCategory.Technical);
        var dto = new UpdateSkillDto
        {
            Name = "New Name",
            Description = "New description",
            Category = SkillCategory.Tools
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.NameExistsAsync(dto.Name, entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(entity.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New description");
        result.Category.Should().Be(SkillCategory.Tools);
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
        var entity = new Skill("Old Name", "Description", SkillCategory.Technical);
        var dto = new UpdateSkillDto
        {
            Name = "Existing Name",
            Description = "Description",
            Category = SkillCategory.Technical
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
        var entity = new Skill("Test Skill", "Description", SkillCategory.Technical);
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
        var entity = new Skill("Test Skill", "Description", SkillCategory.Technical);
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
        var skillId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(skillId);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(skillId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSkillNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
