using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class RoadmapSkillTests
{
    private readonly Guid _roadmapId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_SetsFields()
    {
        var skill = new RoadmapSkill(_roadmapId, " Databases ", " SQL ", SkillCategory.Hard, 3, " Queries ", isOptional: true);

        skill.RoadmapId.Should().Be(_roadmapId);
        skill.Section.Should().Be("Databases");
        skill.Title.Should().Be("SQL");
        skill.Description.Should().Be("Queries");
        skill.Category.Should().Be(SkillCategory.Hard);
        skill.SortOrder.Should().Be(3);
        skill.IsOptional.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyRoadmapId_Throws()
    {
        var act = () => new RoadmapSkill(Guid.Empty, "Databases", "SQL", SkillCategory.Hard, 0);

        act.Should().Throw<ArgumentException>().WithParameterName("roadmapId");
    }

    [Theory]
    [InlineData("", "SQL", "section")]
    [InlineData("Databases", " ", "title")]
    public void Constructor_EmptyText_Throws(string section, string title, string parameter)
    {
        var act = () => new RoadmapSkill(_roadmapId, section, title, SkillCategory.Hard, 0);

        act.Should().Throw<ArgumentException>().WithParameterName(parameter);
    }

    [Fact]
    public void Constructor_TitleTooLong_Throws()
    {
        var act = () => new RoadmapSkill(_roadmapId, "Databases", new string('a', RoadmapSkill.MaxTitleLength + 1), SkillCategory.Hard, 0);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Constructor_SectionTooLong_Throws()
    {
        var act = () => new RoadmapSkill(_roadmapId, new string('a', RoadmapSkill.MaxSectionLength + 1), "SQL", SkillCategory.Hard, 0);

        act.Should().Throw<ArgumentException>().WithParameterName("section");
    }

    [Fact]
    public void Constructor_UnknownCategory_Throws()
    {
        var act = () => new RoadmapSkill(_roadmapId, "Databases", "SQL", (SkillCategory)7, 0);

        act.Should().Throw<ArgumentException>().WithParameterName("category");
    }

    [Fact]
    public void Constructor_NegativeSortOrder_Throws()
    {
        var act = () => new RoadmapSkill(_roadmapId, "Databases", "SQL", SkillCategory.Hard, -1);

        act.Should().Throw<ArgumentException>().WithParameterName("sortOrder");
    }

    [Fact]
    public void Update_ChangesEverythingButTheOrder()
    {
        var skill = new RoadmapSkill(_roadmapId, "Databases", "SQL", SkillCategory.Hard, 4);

        skill.Update("Communication", "Writing", SkillCategory.Soft, "Clear PRs", true);

        skill.Section.Should().Be("Communication");
        skill.Title.Should().Be("Writing");
        skill.Category.Should().Be(SkillCategory.Soft);
        skill.Description.Should().Be("Clear PRs");
        skill.IsOptional.Should().BeTrue();
        skill.SortOrder.Should().Be(4);
    }

    [Fact]
    public void FromStage_DefaultsToJunior_AndDecidesWhichStagesInclude()
    {
        var everyone = new RoadmapSkill(_roadmapId, "Databases", "SQL", SkillCategory.Hard, 0);
        var senior = new RoadmapSkill(_roadmapId, "Leadership", "Mentoring", SkillCategory.Soft, 1, fromStage: CareerStage.Senior);

        everyone.FromStage.Should().Be(CareerStage.Junior);
        everyone.AppliesTo(CareerStage.Junior).Should().BeTrue();
        senior.AppliesTo(CareerStage.Mid).Should().BeFalse();
        senior.AppliesTo(CareerStage.Senior).Should().BeTrue();
        senior.AppliesTo(CareerStage.Staff).Should().BeTrue();
    }

    [Fact]
    public void Constructor_UnknownStage_Throws()
    {
        var act = () => new RoadmapSkill(_roadmapId, "Databases", "SQL", SkillCategory.Hard, 0, fromStage: (CareerStage)9);

        act.Should().Throw<ArgumentException>().WithParameterName("fromStage");
    }

    [Fact]
    public void Update_ChangesTheStage()
    {
        var skill = new RoadmapSkill(_roadmapId, "Databases", "SQL", SkillCategory.Hard, 0);

        skill.Update("Databases", "SQL", SkillCategory.Hard, null, false, CareerStage.Mid);

        skill.FromStage.Should().Be(CareerStage.Mid);
    }
}
