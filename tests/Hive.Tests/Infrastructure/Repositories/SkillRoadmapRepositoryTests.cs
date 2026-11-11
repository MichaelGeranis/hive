using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class SkillRoadmapRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly SkillRoadmapRepository _repository;

    public SkillRoadmapRepositoryTests()
    {
        _repository = new SkillRoadmapRepository(_context);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new SkillRoadmapRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRoadmapsByNameIgnoringCase()
    {
        await _repository.AddAsync(new SkillRoadmap("frontend"));
        await _repository.AddAsync(new SkillRoadmap("Backend"));
        await _repository.AddAsync(new SkillRoadmap("DevOps"));

        var all = await _repository.GetAllAsync();

        all.Select(r => r.Name).Should().Equal("Backend", "DevOps", "frontend");
    }

    [Fact]
    public async Task NameExistsAsync_IgnoresCaseAndTheExcludedRoadmap()
    {
        var roadmap = await _repository.AddAsync(new SkillRoadmap("Backend Engineer"));

        (await _repository.NameExistsAsync(" backend engineer ")).Should().BeTrue();
        (await _repository.NameExistsAsync("Backend Engineer", roadmap.Id)).Should().BeFalse();
        (await _repository.NameExistsAsync("QA Engineer")).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UnknownRoadmap_Throws()
    {
        var act = () => _repository.UpdateAsync(new SkillRoadmap("Backend"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRoadmap()
    {
        var roadmap = await _repository.AddAsync(new SkillRoadmap("Backend"));

        await _repository.DeleteAsync(roadmap.Id);

        (await _repository.GetByIdAsync(roadmap.Id)).Should().BeNull();
    }
}
