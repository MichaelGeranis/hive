using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class CareerProgressTests
{
    private readonly Guid _planId = Guid.NewGuid();
    private int _sortOrder;

    private CareerPlanSkill Skill(string section, int level = 0, SkillCategory category = SkillCategory.Hard,
        bool optional = false, CareerStage fromStage = CareerStage.Junior)
    {
        var skill = new CareerPlanSkill(_planId, section, $"Skill {_sortOrder}", category, _sortOrder++,
            isOptional: optional, fromStage: fromStage);
        if (level > 0) skill.SetLevel(level);
        return skill;
    }

    private static CareerProgress Calculate(IReadOnlyList<CareerPlanSkill> skills, CareerStage stage = CareerStage.Mid, DateTime? now = null) =>
        CareerProgress.Calculate(skills, CareerLadder.Assess(skills, stage), now ?? DateTime.UtcNow);

    [Fact]
    public void Calculate_NoSkills_IsARookieWithNothingEarned()
    {
        var progress = Calculate([]);

        progress.Xp.Should().Be(0);
        progress.PlayerLevel.Should().Be(1);
        progress.PlayerTitle.Should().Be("Rookie");
        progress.Badges.Should().OnlyContain(b => !b.IsEarned);
        progress.LastSkillChangeAt.Should().BeNull();
    }

    [Fact]
    public void Calculate_Xp_IsTenPerLevelPlusABonusPerSkillHad()
    {
        var progress = Calculate([Skill("A", level: 3), Skill("A", level: 1)]);

        progress.Xp.Should().Be(4 * CareerProgress.XpPerSkillLevel + CareerProgress.XpPerSkillHad);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 150)]
    [InlineData(3, 450)]
    [InlineData(4, 900)]
    [InlineData(5, 1500)]
    [InlineData(8, 4200)]
    public void XpForPlayerLevel_EachLevelCostsAHundredAndFiftyMoreThanTheLast(int level, int xp)
    {
        CareerProgress.XpForPlayerLevel(level).Should().Be(xp);
    }

    [Fact]
    public void Calculate_PlayerLevel_FollowsTheXpThresholds()
    {
        // 9 skills at 3: 9 × 30 + 9 × 25 = 495 XP, past the 450 needed for level 3
        var progress = Calculate(Enumerable.Range(0, 9).Select(_ => Skill("A", level: 3)).ToList());

        progress.Xp.Should().Be(495);
        progress.PlayerLevel.Should().Be(3);
        progress.PlayerTitle.Should().Be("Adventurer");
        progress.CurrentLevelXp.Should().Be(450);
        progress.NextLevelXp.Should().Be(900);
    }

    [Fact]
    public void GetTitle_BeyondTheLastTitle_StaysLegend()
    {
        CareerProgress.GetTitle(8).Should().Be("Legend");
        CareerProgress.GetTitle(20).Should().Be("Legend");
    }

    [Fact]
    public void Calculate_CountsHasWorkingOnAndNotYet()
    {
        var focus = Skill("A", level: 1);
        focus.SetFocus(true);

        var had = Skill("A", level: 3);

        var progress = Calculate([had, focus, Skill("A")]);

        progress.HasCount.Should().Be(1);
        progress.WorkingOnCount.Should().Be(1);
        progress.NotYetCount.Should().Be(1);
        progress.LastSkillChangeAt.Should().Be(had.UpdatedAt);
    }

    [Fact]
    public void Calculate_Sections_KeepTheirOrderAndKnowWhenCleared()
    {
        var progress = Calculate(
        [
            Skill("Internet", level: 3),
            Skill("Internet", level: 4),
            Skill("Databases", level: 3),
            Skill("Databases"),
            Skill("Extras", level: 3, optional: true)
        ]);

        progress.Sections.Select(s => s.Name).Should().Equal("Internet", "Databases", "Extras");
        progress.Sections.Select(s => s.IsCleared).Should().Equal(true, false, true);
        progress.Sections[1].HasCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_Badges_AreEarnedByTheirRules()
    {
        var progress = Calculate(
        [
            Skill("A", level: 5),
            Skill("B", level: 3),
            Skill("C", level: 3),
            Skill("D", level: 1),
            Skill("Leadership", level: 3, category: SkillCategory.Soft, fromStage: CareerStage.Senior)
        ]);

        progress.Badges.Where(b => b.IsEarned).Select(b => b.Key).Should().BeEquivalentTo(
            "first-steps", "got-it", "deep-diver", "on-a-roll", "trailblazer", "ahead-of-the-curve");
    }

    [Fact]
    public void Calculate_MeetingEverythingTheirStageExpects_IsRoleReady()
    {
        var progress = Calculate([Skill("A", level: 3), Skill("B", level: 4)], CareerStage.Mid);

        progress.Badges.Single(b => b.Key == "role-ready").IsEarned.Should().BeTrue();
    }

    [Fact]
    public void Calculate_LevelUpsOutsideTheMomentumWindow_DoNotCount()
    {
        var skills = new[] { Skill("A", level: 1), Skill("A", level: 1), Skill("A", level: 1) };

        var progress = Calculate(skills, now: DateTime.UtcNow.AddDays(CareerProgress.MomentumDays + 1));

        progress.SkillsLevelledUpRecently.Should().Be(0);
        progress.Badges.Single(b => b.Key == "on-a-roll").IsEarned.Should().BeFalse();
    }
}
