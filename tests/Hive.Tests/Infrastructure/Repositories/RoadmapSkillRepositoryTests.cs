using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class RoadmapSkillRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly RoadmapSkillRepository _repository;
    private readonly Guid _backend = Guid.NewGuid();
    private readonly Guid _frontend = Guid.NewGuid();

    public RoadmapSkillRepositoryTests()
    {
        _repository = new RoadmapSkillRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new RoadmapSkillRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetByRoadmapIdAsync_ReturnsThatRoadmapsSkillsInOrder()
    {
        await _repository.AddRangeAsync(
        [
            new RoadmapSkill(_backend, "Databases", "SQL", SkillCategory.Hard, 2),
            new RoadmapSkill(_backend, "Internet", "HTTP", SkillCategory.Hard, 0),
            new RoadmapSkill(_backend, "Internet", "DNS", SkillCategory.Hard, 0),
            new RoadmapSkill(_frontend, "CSS", "Layout", SkillCategory.Hard, 1)
        ]);

        var skills = await _repository.GetByRoadmapIdAsync(_backend);

        skills.Select(s => s.Title).Should().Equal("DNS", "HTTP", "SQL");
        (await _repository.GetAllAsync()).Should().HaveCount(4);
    }

    [Fact]
    public async Task DeleteByRoadmapIdAsync_RemovesOnlyThatRoadmapsSkills()
    {
        await _repository.AddAsync(new RoadmapSkill(_backend, "Databases", "SQL", SkillCategory.Hard, 0));
        await _repository.AddAsync(new RoadmapSkill(_frontend, "CSS", "Layout", SkillCategory.Hard, 0));

        await _repository.DeleteByRoadmapIdAsync(_backend);

        (await _repository.GetAllAsync()).Should().ContainSingle(s => s.RoadmapId == _frontend);
    }

    [Fact]
    public async Task AddAsync_SameSkillTwice_Throws()
    {
        var skill = await _repository.AddAsync(new RoadmapSkill(_backend, "Databases", "SQL", SkillCategory.Hard, 0));

        var act = () => _repository.AddAsync(skill);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
