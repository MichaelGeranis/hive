using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;

namespace Hive.Tests.Application.Services;

public class WorkMixTests
{
    private static WorkCategoryShareDto Share(WorkCategory category, int storyPoints) =>
        new() { Category = category, StoryPoints = storyPoints };

    [Fact]
    public void AsPlanningMix_EveryCategory_FoldsIntoTheThreeWorkTypes()
    {
        var mix = WorkMix.AsPlanningMix(
        [
            Share(WorkCategory.Productisation, 30),
            Share(WorkCategory.SystemImprovements, 10),
            Share(WorkCategory.TechMaintenance, 40),
            Share(WorkCategory.TechRoadmap, 15),
            Share(WorkCategory.TechDebt, 5)
        ]);

        mix.Should().BeEquivalentTo(new MixSharesDto { ProductRoadmap = 40, TechRoadmap = 20, Maintenance = 40 });
    }

    [Fact]
    public void AsPlanningMix_NoStoryPoints_ReturnsNull()
    {
        WorkMix.AsPlanningMix([Share(WorkCategory.Productisation, 0), Share(WorkCategory.TechMaintenance, 0)])
            .Should().BeNull();
    }

    [Fact]
    public void ByCategory_TaskUnderAnEpic_CountsItsNewPointsInTheEpicsCategory()
    {
        var epic = Parent.FromJira("MAR-1", "Epic", "Tech Debt");
        var carried = new TeamTask("Refactor", storyPoints: 8, parentId: epic.Id);
        carried.OverridePreviousSprintsStoryPoints(3);
        var tasks = new[] { carried, new TeamTask("Feature", storyPoints: 5) };

        var shares = WorkMix.ByCategory(tasks, new WorkCategories([epic], tasks));

        shares.Single(s => s.Category == WorkCategory.TechDebt).StoryPoints.Should().Be(5);
        shares.Single(s => s.Category == WorkCategory.Productisation).PercentageOfStoryPoints.Should().Be(50);
    }
}
