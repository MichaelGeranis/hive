using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class CareerStageTests
{
    [Theory]
    [InlineData(CareerStage.Junior, SkillCategory.Hard, 2)]
    [InlineData(CareerStage.Mid, SkillCategory.Hard, 3)]
    [InlineData(CareerStage.Senior, SkillCategory.Soft, 4)]
    [InlineData(CareerStage.Staff, SkillCategory.Hard, 4)]
    [InlineData(CareerStage.Staff, SkillCategory.Soft, 5)]
    public void For_ASkillFromJunior_ExpectsTheStageLevel(CareerStage stage, SkillCategory category, int target)
    {
        CareerStageExpectations.For(stage, category).Should().Be(target);
    }

    [Theory]
    [InlineData(CareerStage.Senior, CareerStage.Senior, 3)]
    [InlineData(CareerStage.Staff, CareerStage.Senior, 4)]
    [InlineData(CareerStage.Staff, CareerStage.Staff, 3)]
    [InlineData(CareerStage.Mid, CareerStage.Mid, 3)]
    [InlineData(CareerStage.Staff, CareerStage.Mid, 5)]
    public void For_ASkillFromALaterStage_ExpectsPractisingThenOneMorePerStage(CareerStage stage, CareerStage introducedAt, int target)
    {
        CareerStageExpectations.For(stage, SkillCategory.Soft, introducedAt).Should().Be(target);
    }

    [Theory]
    [InlineData("senior", CareerStage.Senior)]
    [InlineData(" Staff ", CareerStage.Staff)]
    public void Parse_ReadsTheStageName(string value, CareerStage expected)
    {
        CareerStageExpectations.Parse(value, "stage").Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Principal")]
    [InlineData("2")]
    public void Parse_UnknownStage_Throws(string? value)
    {
        var act = () => CareerStageExpectations.Parse(value, "stage");

        act.Should().Throw<ArgumentException>().WithParameterName("stage");
    }
}
