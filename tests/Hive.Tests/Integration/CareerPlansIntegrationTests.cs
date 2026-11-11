using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// Roadmaps and career plans through the real DI container and the in-memory database.
/// </summary>
public class CareerPlansIntegrationTests : IntegrationTestBase
{
    private async Task<DirectReport> AddDirectReportAsync(string first, string last) =>
        await GetService<IDirectReportRepository>().AddAsync(
            new DirectReport(first, last, $"{first.ToLowerInvariant()}@company.com", "Engineer", "Engineering", new DateTime(2022, 3, 15)));

    [Fact]
    public async Task APlanCopiesTheWholeLibraryRoadmap_AndRatingASkillEarnsXpAndBadges()
    {
        // Arrange
        var roadmaps = GetService<ISkillRoadmapService>();
        var plans = GetService<ICareerPlanService>();
        var alice = await AddDirectReportAsync("Alice", "Johnson");
        var roadmap = await roadmaps.AddFromLibraryAsync("backend");
        var plan = await plans.CreateAsync(new CreateCareerPlanDto { DirectReportId = alice.Id, RoadmapId = roadmap.Id, CurrentStage = "Mid" });

        // Act
        var http = plan.Sections.First().Skills.First();
        var change = await plans.SetSkillLevelAsync(plan.Id, http.Id, new SetSkillLevelDto { Level = 3, Evidence = "Debugged the caching headers" });
        var focused = await plans.SetSkillFocusAsync(plan.Id, plan.Sections.First().Skills[1].Id, new SetSkillFocusDto { IsFocus = true });

        // Assert
        plan.SkillCount.Should().Be(roadmap.SkillCount);
        plan.Sections.Select(s => s.Name).Should().Contain(["Senior: technical leadership", "Staff: scope and influence"]);
        plan.Ladder.Reading.Should().Be("NotAssessed");
        change.SkillLevelledUp.Should().BeTrue();
        change.Plan.Game.Xp.Should().Be(3 * CareerProgress.XpPerSkillLevel + CareerProgress.XpPerSkillHad);
        change.NewBadges.Select(b => b.Key).Should().Contain(["first-steps", "got-it"]);
        focused.HasCount.Should().Be(1);
        focused.WorkingOnCount.Should().Be(1);
        (await plans.GetByDirectReportIdAsync(alice.Id))!.Id.Should().Be(plan.Id);

        var feed = await GetService<IActivityService>().GetByEntityTypeAsync(EntityType.CareerPlan);
        feed.Should().Contain(a => a.EntityId == plan.Id && a.Description.StartsWith("Levelled up"));
    }

    [Fact]
    public async Task EditingTheRoadmapLater_DoesNotChangeAPlan()
    {
        // Arrange
        var roadmaps = GetService<ISkillRoadmapService>();
        var plans = GetService<ICareerPlanService>();
        var bob = await AddDirectReportAsync("Bob", "Smith");
        var roadmap = await roadmaps.CreateAsync(new SaveSkillRoadmapDto { Name = "Platform" });
        roadmap = await roadmaps.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "Cloud", Title = "Terraform", Category = "Hard" });
        var plan = await plans.CreateAsync(new CreateCareerPlanDto { DirectReportId = bob.Id, RoadmapId = roadmap.Id, CurrentStage = "Junior" });

        // Act
        await roadmaps.AddSkillAsync(roadmap.Id, new SaveRoadmapSkillDto { Section = "Cloud", Title = "Kubernetes", Category = "Hard" });
        await roadmaps.DeleteAsync(roadmap.Id);

        // Assert
        var reloaded = await plans.GetByIdAsync(plan.Id);
        reloaded!.Sections.Single().Skills.Select(s => s.Title).Should().Equal("Terraform");
        reloaded.RoadmapName.Should().Be("Platform");
    }

    [Fact]
    public async Task ADirectReportHasOnePlan_AndAPromotionIsAChangeOfStage()
    {
        // Arrange
        var roadmaps = GetService<ISkillRoadmapService>();
        var plans = GetService<ICareerPlanService>();
        var carol = await AddDirectReportAsync("Carol", "Williams");
        var roadmap = await roadmaps.AddFromLibraryAsync("qa");
        var plan = await plans.CreateAsync(new CreateCareerPlanDto { DirectReportId = carol.Id, RoadmapId = roadmap.Id, CurrentStage = "Mid" });
        foreach (var skill in plan.Sections.SelectMany(s => s.Skills).Where(s => s.FromStage == "Junior"))
        {
            await plans.SetSkillLevelAsync(plan.Id, skill.Id, new SetSkillLevelDto { Level = 3 });
        }

        // Act
        var second = () => plans.CreateAsync(new CreateCareerPlanDto { DirectReportId = carol.Id, RoadmapId = roadmap.Id, CurrentStage = "Mid" });
        var atMid = await plans.GetByIdAsync(plan.Id);
        var promoted = await plans.UpdateAsync(plan.Id, new UpdateCareerPlanDto { CurrentStage = "Senior" });

        // Assert
        await second.Should().ThrowAsync<ConflictException>();
        atMid!.Ladder.Reading.Should().Be("AtRole");
        promoted.CurrentStage.Should().Be("Senior");
        promoted.Ladder.Reading.Should().Be("BelowRole");
        promoted.Ladder.Weaknesses.Should().NotBeEmpty();
    }
}
