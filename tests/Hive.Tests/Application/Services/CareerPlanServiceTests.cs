using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Tests.Application.Services;

public class CareerPlanServiceTests
{
    private readonly Mock<ICareerPlanRepository> _plans = new();
    private readonly Mock<ICareerPlanSkillRepository> _skills = new();
    private readonly Mock<ISkillRoadmapRepository> _roadmaps = new();
    private readonly Mock<IRoadmapSkillRepository> _roadmapSkills = new();
    private readonly Mock<IDirectReportRepository> _directReports = new();
    private readonly Mock<IActivityService> _activity = new();
    private readonly CareerPlanService _service;

    private readonly DirectReport _alice = new("Alice", "Johnson", "alice@company.com", "Engineer", "Engineering", new DateTime(2022, 3, 15));
    private readonly DirectReport _bob = new("Bob", "Smith", "bob@company.com", "Engineer", "Engineering", new DateTime(2023, 1, 10));
    private readonly DirectReport _skipLevel = new("Sam", "Lee", "sam@company.com", "Engineer", "Engineering", new DateTime(2023, 1, 10), isDirect: false);

    public CareerPlanServiceTests()
    {
        _directReports.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_alice, _bob, _skipLevel]);
        _directReports.Setup(r => r.GetByIdAsync(_alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_alice);
        _directReports.Setup(r => r.GetByIdAsync(_bob.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_bob);
        _plans.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _skills.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _skills.Setup(r => r.GetByCareerPlanIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _service = new CareerPlanService(
            _plans.Object, _skills.Object, _roadmaps.Object, _roadmapSkills.Object, _directReports.Object, _activity.Object);
    }

    private (CareerPlan Plan, IReadOnlyList<CareerPlanSkill> Skills) GivenPlan(DirectReport directReport, CareerStage stage, params (string Title, int Level)[] skills)
    {
        var plan = new CareerPlan(directReport.Id, "Backend Engineer", stage);
        var planSkills = skills
            .Select((s, i) =>
            {
                var skill = new CareerPlanSkill(plan.Id, "Databases", s.Title, SkillCategory.Hard, i);
                if (s.Level > 0) skill.SetLevel(s.Level);
                return skill;
            })
            .ToList();
        _plans.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _skills.Setup(r => r.GetByCareerPlanIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(planSkills);
        foreach (var skill in planSkills)
        {
            _skills.Setup(r => r.GetByIdAsync(skill.Id, It.IsAny<CancellationToken>())).ReturnsAsync(skill);
        }
        return (plan, planSkills);
    }

    [Fact]
    public async Task CreateAsync_CopiesEveryRoadmapSkill_AndRecordsTheCurrentStage()
    {
        var roadmap = new SkillRoadmap("Backend Engineer");
        _roadmaps.Setup(r => r.GetByIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(roadmap);
        _roadmapSkills.Setup(r => r.GetByRoadmapIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new RoadmapSkill(roadmap.Id, "Databases", "SQL", SkillCategory.Hard, 0),
            new RoadmapSkill(roadmap.Id, "Staff: scope and influence", "Technical strategy", SkillCategory.Soft, 1, fromStage: CareerStage.Staff)
        ]);
        var copied = new List<CareerPlanSkill>();
        _skills.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CareerPlanSkill>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CareerPlanSkill>, CancellationToken>((s, _) => copied.AddRange(s));

        var result = await _service.CreateAsync(new CreateCareerPlanDto
        {
            DirectReportId = _alice.Id,
            RoadmapId = roadmap.Id,
            CurrentStage = "mid"
        });

        result.DirectReportName.Should().Be("Alice Johnson");
        result.CurrentStage.Should().Be("Mid");
        copied.Select(s => (s.Title, s.FromStage, s.Level)).Should().Equal(
            ("SQL", CareerStage.Junior, 0),
            ("Technical strategy", CareerStage.Staff, 0));
        _activity.Verify(a => a.LogActivityAsync(ActivityType.Created, EntityType.CareerPlan, result.Id,
            "Alice Johnson's career plan", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTheDirectReportHasAPlan_ThrowsConflict()
    {
        var roadmap = new SkillRoadmap("Backend Engineer");
        _roadmaps.Setup(r => r.GetByIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(roadmap);
        _plans.Setup(r => r.ExistsForDirectReportAsync(_alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.CreateAsync(new CreateCareerPlanDto { DirectReportId = _alice.Id, RoadmapId = roadmap.Id, CurrentStage = "Mid" });

        await act.Should().ThrowAsync<ConflictException>().WithMessage("Alice Johnson already has a career plan.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Principal")]
    public async Task CreateAsync_UnknownStage_ThrowsArgumentException(string stage)
    {
        var roadmap = new SkillRoadmap("Backend Engineer");
        _roadmaps.Setup(r => r.GetByIdAsync(roadmap.Id, It.IsAny<CancellationToken>())).ReturnsAsync(roadmap);

        var act = () => _service.CreateAsync(new CreateCareerPlanDto { DirectReportId = _alice.Id, RoadmapId = roadmap.Id, CurrentStage = stage });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_UnknownRoadmap_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(new CreateCareerPlanDto { DirectReportId = _alice.Id, RoadmapId = Guid.NewGuid(), CurrentStage = "Mid" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_MapsStatusesLadderAndGame()
    {
        var (plan, skills) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 4), ("Indexes", 1), ("Migrations", 0));
        skills[1].SetFocus(true);

        var result = await _service.GetByIdAsync(plan.Id);

        result!.HasCount.Should().Be(1);
        result.WorkingOnCount.Should().Be(1);
        result.NotYetCount.Should().Be(1);
        var mapped = result.Sections.Single().Skills;
        mapped.Select(s => s.Status).Should().Equal("Has", "WorkingOn", "NotYet");
        mapped.Should().OnlyContain(s => s.ExpectedLevel == 3);
        result.Ladder.CurrentStage.Should().Be("Mid");
        result.Ladder.Reading.Should().Be("BelowRole");
        result.Ladder.Strengths.Select(s => s.Title).Should().Equal("SQL");
        result.Ladder.Weaknesses.Select(s => s.Title).Should().Equal("Migrations", "Indexes");
        result.Game.Xp.Should().Be(50 + CareerProgress.XpPerSkillHad);
        result.Game.Badges.Should().Contain(b => b.Key == "got-it" && b.IsEarned);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_WhenNone_ReturnsNull()
    {
        (await _service.GetByDirectReportIdAsync(_bob.Id)).Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_RecordsAChangeOfStage()
    {
        var (plan, _) = GivenPlan(_alice, CareerStage.Mid);

        var result = await _service.UpdateAsync(plan.Id, new UpdateCareerPlanDto { CurrentStage = "Senior", Notes = "Promoted" });

        result.CurrentStage.Should().Be("Senior");
        result.Notes.Should().Be("Promoted");
        _activity.Verify(a => a.LogActivityAsync(ActivityType.Updated, EntityType.CareerPlan, plan.Id, It.IsAny<string>(),
            "Role stage changed from Mid to Senior", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSkillLevelAsync_ReportsTheLevelUpAndNewBadges()
    {
        // 80 XP before; a 5 on SQL brings 50 + 25 more, past the 150 of player level 2
        var (plan, skills) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 0), ("Indexes", 2), ("Migrations", 2), ("Caching", 2), ("Queues", 1), ("DNS", 1));

        var result = await _service.SetSkillLevelAsync(plan.Id, skills[0].Id, new SetSkillLevelDto { Level = 5, Evidence = "Owns the reporting schema" });

        result.SkillLevelledUp.Should().BeTrue();
        result.PlayerLevelledUp.Should().BeTrue();
        result.NewBadges.Select(b => b.Key).Should().BeEquivalentTo(["got-it", "deep-diver"]);
        result.Plan.Sections.Single().Skills.First().Evidence.Should().Be("Owns the reporting schema");
        _skills.Verify(r => r.UpdateAsync(It.Is<CareerPlanSkill>(s => s.Level == 5), It.IsAny<CancellationToken>()), Times.Once);
        _activity.Verify(a => a.LogActivityAsync(ActivityType.Updated, EntityType.CareerPlan, plan.Id, It.IsAny<string>(),
            It.Is<string>(d => d.StartsWith("Levelled up")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSkillLevelAsync_Lowered_IsNotALevelUp()
    {
        var (plan, skills) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 3));

        var result = await _service.SetSkillLevelAsync(plan.Id, skills[0].Id, new SetSkillLevelDto { Level = 2 });

        result.SkillLevelledUp.Should().BeFalse();
        result.PlayerLevelledUp.Should().BeFalse();
        result.NewBadges.Should().BeEmpty();
    }

    [Fact]
    public async Task SetSkillLevelAsync_SkillOfAnotherPlan_ThrowsNotFound()
    {
        var (plan, _) = GivenPlan(_alice, CareerStage.Mid);

        var act = () => _service.SetSkillLevelAsync(plan.Id, Guid.NewGuid(), new SetSkillLevelDto { Level = 1 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetSkillFocusAsync_PutsTheSkillInFocus()
    {
        var (plan, skills) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 1));

        var result = await _service.SetSkillFocusAsync(plan.Id, skills[0].Id, new SetSkillFocusDto { IsFocus = true });

        result.WorkingOnCount.Should().Be(1);
        result.Sections.Single().Skills.Single().Status.Should().Be("WorkingOn");
        _activity.Verify(a => a.LogActivityAsync(ActivityType.Updated, EntityType.CareerPlan, plan.Id, It.IsAny<string>(),
            "Started working on SQL", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSkillFocusAsync_Unchanged_DoesNotSaveOrLog()
    {
        var (plan, skills) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 1));

        await _service.SetSkillFocusAsync(plan.Id, skills[0].Id, new SetSkillFocusDto { IsFocus = false });

        _skills.Verify(r => r.UpdateAsync(It.IsAny<CareerPlanSkill>(), It.IsAny<CancellationToken>()), Times.Never);
        _activity.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddSkillAsync_GoesAfterTheLastSkill_WithItsStage()
    {
        var (plan, _) = GivenPlan(_alice, CareerStage.Mid, ("SQL", 0), ("Indexes", 0));

        await _service.AddSkillAsync(plan.Id, new AddCareerPlanSkillDto { Section = "Extra", Title = "Kafka", Category = "Hard", FromStage = "Senior" });

        _skills.Verify(r => r.AddAsync(It.Is<CareerPlanSkill>(s => s.SortOrder == 2 && s.Title == "Kafka" && s.FromStage == CareerStage.Senior),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RemovesThePlanAndItsSkills()
    {
        var (plan, _) = GivenPlan(_alice, CareerStage.Mid);

        await _service.DeleteAsync(plan.Id);

        _skills.Verify(r => r.DeleteByCareerPlanIdAsync(plan.Id, It.IsAny<CancellationToken>()), Times.Once);
        _plans.Verify(r => r.DeleteAsync(plan.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_CarriesNoScores_AndFlagsAQuietPlan()
    {
        var plan = new CareerPlan(_alice.Id, "Backend Engineer", CareerStage.Mid);
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(plan, DateTime.UtcNow.AddDays(-90));
        _plans.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([plan]);

        var result = await _service.GetAllAsync();

        var summary = result.Single();
        summary.IsQuiet.Should().BeTrue();
        summary.Should().BeOfType<CareerPlanSummaryDto>();
    }

    [Fact]
    public async Task GetOverviewAsync_ListsWhatNeedsTheManager()
    {
        var bobsPlan = new CareerPlan(_bob.Id, "Backend Engineer", CareerStage.Mid);
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(bobsPlan, DateTime.UtcNow.AddDays(-90));
        _plans.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([bobsPlan]);

        var overview = await _service.GetOverviewAsync();

        // Sam is not a direct report, so no plan is expected of them
        overview.DirectReportsWithoutPlan.Select(d => d.FullName).Should().Equal("Alice Johnson");
        overview.NothingInFocus.Should().ContainSingle(a => a.PlanId == bobsPlan.Id && a.FullName == "Bob Smith");
        overview.Quiet.Should().ContainSingle(a => a.PlanId == bobsPlan.Id);
    }
}
