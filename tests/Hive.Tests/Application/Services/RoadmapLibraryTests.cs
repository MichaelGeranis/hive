using Hive.Application.Services;
using Hive.Core.Entities;

namespace Hive.Tests.Application.Services;

public class RoadmapLibraryTests
{
    public static TheoryData<string> Keys()
    {
        var data = new TheoryData<string>();
        foreach (var roadmap in RoadmapLibrary.All) data.Add(roadmap.Key);
        return data;
    }

    [Fact]
    public void All_HasUniqueKeysAndNames()
    {
        RoadmapLibrary.All.Select(r => r.Key).Should().OnlyHaveUniqueItems();
        RoadmapLibrary.All.Select(r => r.Name).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryRoadmap_HasUniqueSectionsAndSkills_AndBothKindsOfSkill(string key)
    {
        var roadmap = RoadmapLibrary.Find(key)!;

        roadmap.Sections.Select(s => s.Name.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
        roadmap.Skills.Select(s => s.Title.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
        roadmap.Sections.Should().OnlyContain(s => s.Skills.Count > 0);
        roadmap.HardSkillCount.Should().BeGreaterThan(0);
        roadmap.SoftSkillCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void Instantiate_BuildsValidEntitiesInOrder(string key)
    {
        var source = RoadmapLibrary.Find(key)!;

        var (roadmap, skills) = RoadmapLibrary.Instantiate(source);

        roadmap.Name.Should().Be(source.Name);
        roadmap.LibraryKey.Should().Be(source.Key);
        skills.Should().HaveCount(source.Skills.Count());
        skills.Should().OnlyContain(s => s.RoadmapId == roadmap.Id);
        skills.Select(s => s.SortOrder).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        skills.Select(s => s.Section).Distinct().Should().Equal(source.Sections.Select(s => s.Name));
    }

    [Fact]
    public void Instantiate_TakesTheCategoryFromTheSection()
    {
        var (_, skills) = RoadmapLibrary.Instantiate(RoadmapLibrary.Find("backend")!);

        skills.Where(s => s.Section == "Communication").Should().OnlyContain(s => s.Category == SkillCategory.Soft);
        skills.Where(s => s.Section == "Databases").Should().OnlyContain(s => s.Category == SkillCategory.Hard);
    }

    [Fact]
    public void Find_IgnoresCase_AndReturnsNullForUnknownKeys()
    {
        RoadmapLibrary.Find("BACKEND").Should().NotBeNull();
        RoadmapLibrary.Find("astronaut").Should().BeNull();
    }

    [Fact]
    public void EngineerRoadmaps_AddSeniorAndStaffSections()
    {
        var (_, skills) = RoadmapLibrary.Instantiate(RoadmapLibrary.Find("backend")!);

        skills.Where(s => s.Section == "Senior: technical leadership").Should().OnlyContain(s => s.FromStage == CareerStage.Senior);
        skills.Where(s => s.Section == "Staff: scope and influence").Should().OnlyContain(s => s.FromStage == CareerStage.Staff);
        skills.Where(s => s.Section == "Architecture and scale").Should().OnlyContain(s => s.FromStage == CareerStage.Mid);
        skills.Count(s => s.AppliesTo(CareerStage.Junior)).Should().BeLessThan(skills.Count(s => s.AppliesTo(CareerStage.Staff)));
    }
}
