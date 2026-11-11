using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class CareerPlanTests
{
    private readonly Guid _directReportId = Guid.NewGuid();

    private CareerPlan NewPlan() => new(_directReportId, "Backend Engineer", CareerStage.Mid);

    private static void SetCreatedAt(CareerPlan plan, DateTime createdAt) =>
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(plan, createdAt);

    [Fact]
    public void Constructor_WithValidData_RecordsTheCurrentStage()
    {
        var plan = new CareerPlan(_directReportId, " Backend Engineer ", CareerStage.Senior, " Wants to lead ");

        plan.DirectReportId.Should().Be(_directReportId);
        plan.RoadmapName.Should().Be("Backend Engineer");
        plan.CurrentStage.Should().Be(CareerStage.Senior);
        plan.Notes.Should().Be("Wants to lead");
    }

    [Fact]
    public void Constructor_EmptyDirectReportId_Throws()
    {
        var act = () => new CareerPlan(Guid.Empty, "Backend", CareerStage.Mid);

        act.Should().Throw<ArgumentException>().WithParameterName("directReportId");
    }

    [Fact]
    public void Constructor_EmptyRoadmapName_Throws()
    {
        var act = () => new CareerPlan(_directReportId, " ", CareerStage.Mid);

        act.Should().Throw<ArgumentException>().WithParameterName("roadmapName");
    }

    [Fact]
    public void Constructor_UnknownStage_Throws()
    {
        var act = () => new CareerPlan(_directReportId, "Backend", (CareerStage)9);

        act.Should().Throw<ArgumentException>().WithParameterName("currentStage");
    }

    [Fact]
    public void Constructor_NotesTooLong_Throws()
    {
        var act = () => new CareerPlan(_directReportId, "Backend", CareerStage.Mid, new string('a', CareerPlan.MaxNotesLength + 1));

        act.Should().Throw<ArgumentException>().WithParameterName("notes");
    }

    [Fact]
    public void Update_RecordsAPromotionAndNotes()
    {
        var plan = NewPlan();

        plan.Update(CareerStage.Senior, "Promoted in March");

        plan.CurrentStage.Should().Be(CareerStage.Senior);
        plan.Notes.Should().Be("Promoted in March");
        plan.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsQuiet_NewPlan_IsNotQuiet()
    {
        NewPlan().IsQuiet(null, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsQuiet_OldPlanWithNoChanges_IsQuiet()
    {
        var plan = NewPlan();
        var now = DateTime.UtcNow;
        SetCreatedAt(plan, now.AddDays(-CareerPlan.QuietDays - 1));

        plan.IsQuiet(null, now).Should().BeTrue();
    }

    [Fact]
    public void IsQuiet_OldPlanWithARecentSkillChange_IsNotQuiet()
    {
        var plan = NewPlan();
        var now = DateTime.UtcNow;
        SetCreatedAt(plan, now.AddDays(-200));

        plan.IsQuiet(now.AddDays(-5), now).Should().BeFalse();
    }

    [Fact]
    public void IsQuiet_OldPlanUpdatedRecently_IsNotQuiet()
    {
        var plan = NewPlan();
        SetCreatedAt(plan, DateTime.UtcNow.AddDays(-200));
        plan.Update(CareerStage.Senior, null);

        plan.IsQuiet(null, DateTime.UtcNow).Should().BeFalse();
    }
}
