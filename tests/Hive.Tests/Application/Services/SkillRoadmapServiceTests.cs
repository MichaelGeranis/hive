using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class SkillRoadmapServiceTests
{
    private readonly Mock<ISkillRoadmapRepository> _roadmaps = new();
    private readonly Mock<IRoadmapSkillRepository> _skills = new();
    private readonly Mock<IActivityService> _activity = new();
    private readonly SkillRoadmapService _service;

    public SkillRoadmapServiceTests()
    {
        _skills.Setup(r => r.GetByRoadmapIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _skills.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _roadmaps.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _service = new SkillRoadmapService(_roadmaps.Object, _skills.Object, _activity.Object);
    }

    private SkillRoadmap GivenRoadmap(params RoadmapSkill[] skills)
    {
        var roadmap = new SkillRoadmap("Backend Engineer");
        _roadmaps.Setup(r => r.GetByIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(roadmap);
        _skills.Setup(r => r.GetByRoadmapIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(skills);
        return roadmap;
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        var act = () => new SkillRoadmapService(null!, _skills.Object, _activity.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAllAsync_CountsEachRoadmapsSkills()
    {
        var roadmap = new SkillRoadmap("Backend Engineer");
        _roadmaps.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([roadmap]);
        _skills.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new RoadmapSkill(roadmap.Id, "Databases", "SQL", SkillCategory.Hard, 0),
            new RoadmapSkill(roadmap.Id, "databases", "Indexes", SkillCategory.Hard, 1),
            new RoadmapSkill(roadmap.Id, "Communication", "Writing", SkillCategory.Soft, 2),
            new RoadmapSkill(Guid.NewGuid(), "Other", "Elsewhere", SkillCategory.Hard, 0)
        ]);

        var result = await _service.GetAllAsync();

        var summary = result.Single();
        summary.SkillCount.Should().Be(3);
        summary.SectionCount.Should().Be(2);
        summary.HardSkillCount.Should().Be(2);
        summary.SoftSkillCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_GroupsSkillsIntoSectionsInOrder()
    {
        var id = Guid.NewGuid();
        var roadmap = GivenRoadmap();
        _skills.Setup(r => r.GetByRoadmapIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new RoadmapSkill(roadmap.Id, "Internet", "HTTP", SkillCategory.Hard, 0),
            new RoadmapSkill(roadmap.Id, "Databases", "SQL", SkillCategory.Hard, 1),
            new RoadmapSkill(roadmap.Id, "Internet", "DNS", SkillCategory.Hard, 2)
        ]);

        var result = await _service.GetByIdAsync(roadmap.Id);

        result!.Sections.Select(s => s.Name).Should().Equal("Internet", "Databases");
        result.Sections[0].Skills.Select(s => s.Title).Should().Equal("HTTP", "DNS");
        result.Sections[0].Skills[0].Category.Should().Be("Hard");
        (await _service.GetByIdAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithADuplicateName_ThrowsConflict()
    {
        _roadmaps.Setup(r => r.NameExistsAsync("Backend", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.CreateAsync(new SaveSkillRoadmapDto { Name = "Backend" });

        await act.Should().ThrowAsync<ConflictException>();
        _roadmaps.Verify(r => r.AddAsync(It.IsAny<SkillRoadmap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SavesAndLogs()
    {
        var result = await _service.CreateAsync(new SaveSkillRoadmapDto { Name = "Platform", Description = "Infra" });

        result.Name.Should().Be("Platform");
        _roadmaps.Verify(r => r.AddAsync(It.Is<SkillRoadmap>(x => x.Name == "Platform"), It.IsAny<CancellationToken>()), Times.Once);
        _activity.Verify(a => a.LogActivityAsync(ActivityType.Created, EntityType.SkillRoadmap, It.IsAny<Guid>(),
            "Platform", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UnknownRoadmap_ThrowsNotFound()
    {
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new SaveSkillRoadmapDto { Name = "X" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRoadmapAndItsSkills()
    {
        var roadmap = GivenRoadmap();

        await _service.DeleteAsync(roadmap.Id);

        _skills.Verify(r => r.DeleteByRoadmapIdAsync(roadmap.Id, It.IsAny<CancellationToken>()), Times.Once);
        _roadmaps.Verify(r => r.DeleteAsync(roadmap.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSkillAsync_GoesAfterTheLastSkill()
    {
        var roadmap = GivenRoadmap(
            new RoadmapSkill(Guid.NewGuid(), "Internet", "HTTP", SkillCategory.Hard, 4));

        await _service.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "Internet", Title = "DNS", Category = "hard" });

        _skills.Verify(r => r.AddAsync(It.Is<RoadmapSkill>(s => s.SortOrder == 5 && s.Title == "DNS" && s.Category == SkillCategory.Hard),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Medium")]
    [InlineData("7")]
    public async Task AddSkillAsync_UnknownCategory_ThrowsArgumentException(string category)
    {
        var roadmap = GivenRoadmap();

        var act = () => _service.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "A", Title = "B", Category = category });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateSkillAsync_SkillOfAnotherRoadmap_ThrowsNotFound()
    {
        var roadmap = GivenRoadmap();
        var foreign = new RoadmapSkill(Guid.NewGuid(), "A", "B", SkillCategory.Hard, 0);
        _skills.Setup(r => r.GetByIdAsync(foreign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreign);

        var act = () => _service.UpdateSkillAsync(roadmap.Id, foreign.Id, new SaveRoadmapSkillDto { Section = "A", Title = "C" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLibraryAsync_MarksTheRoadmapsAlreadyAdded()
    {
        _roadmaps.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SkillRoadmap("My backend", libraryKey: "backend")]);

        var library = await _service.GetLibraryAsync();

        library.Should().HaveCount(RoadmapLibrary.All.Count);
        library.Single(l => l.Key == "backend").IsAdded.Should().BeTrue();
        library.Single(l => l.Key == "frontend").IsAdded.Should().BeFalse();
        library.Should().OnlyContain(l => l.SkillCount == l.HardSkillCount + l.SoftSkillCount);
    }

    [Fact]
    public async Task AddFromLibraryAsync_CopiesTheRoadmapAndItsSkills()
    {
        IEnumerable<RoadmapSkill>? saved = null;
        _skills.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RoadmapSkill>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RoadmapSkill>, CancellationToken>((s, _) => saved = s.ToList());

        await _service.AddFromLibraryAsync("qa");

        _roadmaps.Verify(r => r.AddAsync(It.Is<SkillRoadmap>(x => x.LibraryKey == "qa" && x.Name == "QA Engineer"), It.IsAny<CancellationToken>()), Times.Once);
        saved.Should().HaveCount(RoadmapLibrary.Find("qa")!.Skills.Count());
    }

    [Fact]
    public async Task AddFromLibraryAsync_UnknownKey_ThrowsNotFound()
    {
        var act = () => _service.AddFromLibraryAsync("astronaut");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddFromLibraryAsync_NameTaken_ThrowsConflict()
    {
        _roadmaps.Setup(r => r.NameExistsAsync("QA Engineer", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.AddFromLibraryAsync("qa");

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AddSkillAsync_ReadsTheStage()
    {
        var roadmap = GivenRoadmap();

        await _service.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "Leadership", Title = "Mentoring", Category = "Soft", FromStage = "senior" });

        _skills.Verify(r => r.AddAsync(It.Is<RoadmapSkill>(s => s.FromStage == CareerStage.Senior), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSkillAsync_UnknownStage_ThrowsArgumentException()
    {
        var roadmap = GivenRoadmap();

        var act = () => _service.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "A", Title = "B", FromStage = "Principal" });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_CountsTheSkillsEachStageWouldCopy()
    {
        var roadmap = GivenRoadmap();
        _skills.Setup(r => r.GetByRoadmapIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new RoadmapSkill(roadmap.Id, "Databases", "SQL", SkillCategory.Hard, 0),
            new RoadmapSkill(roadmap.Id, "Leadership", "Mentoring", SkillCategory.Soft, 1, fromStage: CareerStage.Senior),
            new RoadmapSkill(roadmap.Id, "Influence", "Strategy", SkillCategory.Soft, 2, fromStage: CareerStage.Staff)
        ]);

        var result = await _service.GetByIdAsync(roadmap.Id);

        result!.SkillCountsByStage.Should().Equal(new Dictionary<string, int> { ["Junior"] = 1, ["Mid"] = 1, ["Senior"] = 2, ["Staff"] = 3 });
        result.Sections[1].Skills.Single().FromStage.Should().Be("Senior");
    }
}
