using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class CareerLadderTests
{
    private readonly Guid _planId = Guid.NewGuid();
    private int _sortOrder;

    private CareerPlanSkill Skill(int level, CareerStage fromStage = CareerStage.Junior, SkillCategory category = SkillCategory.Hard, bool optional = false)
    {
        var skill = new CareerPlanSkill(_planId, fromStage.ToString(), $"Skill {_sortOrder}", category, _sortOrder++,
            isOptional: optional, fromStage: fromStage);
        if (level > 0) skill.SetLevel(level);
        return skill;
    }

    [Fact]
    public void Assess_NothingRated_IsNotAssessed()
    {
        var ladder = CareerLadder.Assess([Skill(0), Skill(0)], CareerStage.Mid);

        ladder.Reading.Should().Be(LadderReading.NotAssessed);
        ladder.SkillsStage.Should().BeNull();
    }

    [Fact]
    public void Assess_MeetingTheirOwnStageButNotTheNext_ReadsAtRole()
    {
        // Mid expects 3 on these; Senior would expect 4
        var skills = Enumerable.Range(0, 5).Select(_ => Skill(3)).ToList();

        var ladder = CareerLadder.Assess(skills, CareerStage.Mid);

        ladder.SkillsStage.Should().Be(CareerStage.Mid);
        ladder.Reading.Should().Be(LadderReading.AtRole);
        ladder.Stages.Single(s => s.Stage == CareerStage.Mid).MetPercent.Should().Be(100);
        ladder.Stages.Single(s => s.Stage == CareerStage.Senior).MetPercent.Should().Be(0);
    }

    [Fact]
    public void Assess_MeetingALaterStage_ReadsAboveRole()
    {
        // Senior expects 4 on the first four and 3 on its own soft skill; Staff expects 5 and 4 on the soft ones
        var skills = new List<CareerPlanSkill>
        {
            Skill(4), Skill(4), Skill(4), Skill(4, category: SkillCategory.Soft), Skill(3, CareerStage.Senior, SkillCategory.Soft)
        };

        var ladder = CareerLadder.Assess(skills, CareerStage.Mid);

        ladder.SkillsStage.Should().Be(CareerStage.Senior);
        ladder.Reading.Should().Be(LadderReading.AboveRole);
    }

    [Fact]
    public void Assess_BelowTheirStage_ReadsBelowRole()
    {
        var skills = new List<CareerPlanSkill> { Skill(3), Skill(2), Skill(2), Skill(2) };

        var ladder = CareerLadder.Assess(skills, CareerStage.Senior);

        ladder.SkillsStage.Should().Be(CareerStage.Junior);
        ladder.Reading.Should().Be(LadderReading.BelowRole);
    }

    [Fact]
    public void Assess_AStageIsMetFromEightyPercent()
    {
        // 4 of 5 skills at Mid's 3: 80%
        var skills = new List<CareerPlanSkill> { Skill(3), Skill(3), Skill(3), Skill(3), Skill(2) };

        CareerLadder.Assess(skills, CareerStage.Mid).SkillsStage.Should().Be(CareerStage.Mid);
    }

    [Fact]
    public void Assess_OptionalSkills_AreNotExpected()
    {
        var skills = new List<CareerPlanSkill> { Skill(3), Skill(0, optional: true) };

        var ladder = CareerLadder.Assess(skills, CareerStage.Mid);

        ladder.Stages.Single(s => s.Stage == CareerStage.Mid).ExpectedCount.Should().Be(1);
        ladder.Weaknesses.Should().BeEmpty();
    }

    [Fact]
    public void Assess_StrengthsAndWeaknesses_AreReadAgainstTheirOwnStage()
    {
        var aboveMid = Skill(5);
        var atMid = Skill(3);
        var farBelow = Skill(0);
        var slightlyBelow = Skill(2);
        var seniorSkillHad = Skill(3, CareerStage.Senior, SkillCategory.Soft);
        var seniorSkillNotYet = Skill(1, CareerStage.Senior, SkillCategory.Soft);

        var ladder = CareerLadder.Assess([aboveMid, atMid, farBelow, slightlyBelow, seniorSkillHad, seniorSkillNotYet], CareerStage.Mid);

        ladder.Strengths.Should().Equal(seniorSkillHad, aboveMid);
        ladder.Weaknesses.Should().Equal(farBelow, slightlyBelow);
        ladder.ExpectedLevel(atMid).Should().Be(3);
        ladder.ExpectedLevel(seniorSkillNotYet).Should().BeNull();
    }
}
