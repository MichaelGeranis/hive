using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class CareerPlanSkillTests
{
    private readonly Guid _planId = Guid.NewGuid();

    private CareerPlanSkill NewSkill() => new(_planId, "Databases", "SQL", SkillCategory.Hard, 0);

    [Fact]
    public void Constructor_StartsNotStartedAndNotInFocus()
    {
        var skill = NewSkill();

        skill.CareerPlanId.Should().Be(_planId);
        skill.Level.Should().Be(0);
        skill.IsFocus.Should().BeFalse();
        skill.FromStage.Should().Be(CareerStage.Junior);
        skill.Status.Should().Be(CareerSkillStatus.NotYet);
        skill.LastLevelUpAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyPlanId_Throws()
    {
        var act = () => new CareerPlanSkill(Guid.Empty, "Databases", "SQL", SkillCategory.Hard, 0);

        act.Should().Throw<ArgumentException>().WithParameterName("careerPlanId");
    }

    [Fact]
    public void Constructor_UnknownStage_Throws()
    {
        var act = () => new CareerPlanSkill(_planId, "Databases", "SQL", SkillCategory.Hard, 0, fromStage: (CareerStage)9);

        act.Should().Throw<ArgumentException>().WithParameterName("fromStage");
    }

    [Fact]
    public void FromRoadmap_CopiesTheSkillAndItsStage()
    {
        var roadmapSkill = new RoadmapSkill(Guid.NewGuid(), "Leadership", "Mentoring", SkillCategory.Soft, 7, "Grows others",
            isOptional: true, fromStage: CareerStage.Senior);

        var skill = CareerPlanSkill.FromRoadmap(_planId, roadmapSkill);

        skill.CareerPlanId.Should().Be(_planId);
        skill.Section.Should().Be("Leadership");
        skill.Title.Should().Be("Mentoring");
        skill.Description.Should().Be("Grows others");
        skill.Category.Should().Be(SkillCategory.Soft);
        skill.SortOrder.Should().Be(7);
        skill.IsOptional.Should().BeTrue();
        skill.FromStage.Should().Be(CareerStage.Senior);
        skill.Level.Should().Be(0);
    }

    [Theory]
    [InlineData(2, false, CareerSkillStatus.NotYet)]
    [InlineData(3, false, CareerSkillStatus.Has)]
    [InlineData(5, false, CareerSkillStatus.Has)]
    [InlineData(1, true, CareerSkillStatus.WorkingOn)]
    [InlineData(4, true, CareerSkillStatus.WorkingOn)]
    public void Status_WorkingOnComesFirst_ThenHasFromPractising(int level, bool focus, CareerSkillStatus expected)
    {
        var skill = NewSkill();
        skill.SetLevel(level);
        skill.SetFocus(focus);

        skill.Status.Should().Be(expected);
        skill.Has.Should().Be(level >= SkillLevel.Practising);
    }

    [Fact]
    public void SetLevel_Raised_RecordsALevelUp()
    {
        var skill = NewSkill();

        var levelledUp = skill.SetLevel(2, "Wrote the reporting queries");

        levelledUp.Should().BeTrue();
        skill.Level.Should().Be(2);
        skill.Evidence.Should().Be("Wrote the reporting queries");
        skill.LastLevelUpAt.Should().NotBeNull();
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetLevel_Lowered_IsNotALevelUp()
    {
        var skill = NewSkill();
        skill.SetLevel(3);
        var levelUpAt = skill.LastLevelUpAt;

        var levelledUp = skill.SetLevel(1);

        levelledUp.Should().BeFalse();
        skill.LastLevelUpAt.Should().Be(levelUpAt);
    }

    [Fact]
    public void SetLevel_WithoutEvidence_KeepsTheEvidence()
    {
        var skill = NewSkill();
        skill.SetLevel(1, "Read the docs");

        skill.SetLevel(2);

        skill.Evidence.Should().Be("Read the docs");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void SetLevel_OutOfRange_Throws(int level)
    {
        var act = () => NewSkill().SetLevel(level);

        act.Should().Throw<ArgumentException>().WithParameterName("level");
    }

    [Fact]
    public void SetLevel_EvidenceTooLong_Throws()
    {
        var act = () => NewSkill().SetLevel(1, new string('a', CareerPlanSkill.MaxEvidenceLength + 1));

        act.Should().Throw<ArgumentException>().WithParameterName("evidence");
    }

    [Fact]
    public void SetFocus_RecordsTheChange()
    {
        var skill = NewSkill();

        skill.SetFocus(true);

        skill.IsFocus.Should().BeTrue();
        skill.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, "Not started")]
    [InlineData(3, "Practising")]
    [InlineData(5, "Expert")]
    public void SkillLevel_GetLabel_NamesEachLevel(int level, string label)
    {
        SkillLevel.GetLabel(level).Should().Be(label);
    }
}
