using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class CareerPlanSkillRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly CareerPlanSkillRepository _repository;
    private readonly Guid _planA = Guid.NewGuid();
    private readonly Guid _planB = Guid.NewGuid();

    public CareerPlanSkillRepositoryTests()
    {
        _repository = new CareerPlanSkillRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new CareerPlanSkillRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByCareerPlanIdAsync_ReturnsThatPlansSkillsInOrder()
    {
        await _repository.AddRangeAsync(
        [
            new CareerPlanSkill(_planA, "Databases", "SQL", SkillCategory.Hard, 1),
            new CareerPlanSkill(_planA, "Internet", "HTTP", SkillCategory.Hard, 0),
            new CareerPlanSkill(_planB, "CSS", "Layout", SkillCategory.Hard, 0)
        ]);

        (await _repository.GetByCareerPlanIdAsync(_planA)).Select(s => s.Title).Should().Equal("HTTP", "SQL");
        (await _repository.GetAllAsync()).Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_StoresTheNewLevel()
    {
        var skill = await _repository.AddAsync(new CareerPlanSkill(_planA, "Databases", "SQL", SkillCategory.Hard, 0));
        skill.SetLevel(2);

        await _repository.UpdateAsync(skill);

        (await _repository.GetByIdAsync(skill.Id))!.Level.Should().Be(2);
    }

    [Fact]
    public async Task DeleteByCareerPlanIdAsync_RemovesOnlyThatPlansSkills()
    {
        await _repository.AddAsync(new CareerPlanSkill(_planA, "Databases", "SQL", SkillCategory.Hard, 0));
        await _repository.AddAsync(new CareerPlanSkill(_planB, "CSS", "Layout", SkillCategory.Hard, 0));

        await _repository.DeleteByCareerPlanIdAsync(_planA);

        (await _repository.GetAllAsync()).Should().ContainSingle(s => s.CareerPlanId == _planB);
    }
}
