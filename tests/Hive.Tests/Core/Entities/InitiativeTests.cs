using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class InitiativeTests
{
    private static readonly Guid QuarterId = Guid.NewGuid();

    // A size comes with readiness, so a sized option is created ready for commitment
    private static Initiative CreateOption(string name = "Euterpe Integration", string? size = null) =>
        new(QuarterId, name, "#8B5CF6", tshirtSize: size,
            status: size is null ? InitiativeStatus.InDiscovery : InitiativeStatus.ReadyForCommitment);

    [Fact]
    public void Constructor_ReadyWithSize_CreatesSizedOption()
    {
        var dueDate = new DateTime(2026, 9, 24);
        var initiative = new Initiative(
            QuarterId, "  Euterpe Integration  ", "#8B5CF6", "  notes  ", "l+", "https://docs.example.com/design",
            WorkType.ProductRoadmap, InitiativeKind.Option, 3, new DateTime(2026, 8, 14), dueDate,
            InitiativeStatus.ReadyForCommitment);

        initiative.Name.Should().Be("Euterpe Integration");
        initiative.Description.Should().Be("notes");
        initiative.TshirtSize.Should().Be("L+");
        initiative.Kind.Should().Be(InitiativeKind.Option);
        initiative.Status.Should().Be(InitiativeStatus.ReadyForCommitment);
        initiative.Rank.Should().Be(3);
        initiative.ArrivedOn.Should().Be(new DateTime(2026, 8, 14));
        initiative.DueDate.Should().Be(dueDate);
        initiative.Readiness.Should().Be(ReadinessIssue.None);
    }

    [Fact]
    public void Constructor_NoStatus_IsInDiscoveryAndUnsized()
    {
        var option = new Initiative(QuarterId, "Euterpe", "#8B5CF6");

        option.Status.Should().Be(InitiativeStatus.InDiscovery);
        option.TshirtSize.Should().BeNull();
    }

    [Theory]
    [InlineData(InitiativeStatus.InDiscovery)]
    [InlineData(InitiativeStatus.InSolutionDesign)]
    public void Constructor_SizeBeforeReady_Throws(InitiativeStatus status)
    {
        var act = () => new Initiative(QuarterId, "Euterpe", "#8B5CF6", tshirtSize: "M", status: status);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ready for commitment*");
    }

    [Fact]
    public void Constructor_SizedLane_Throws()
    {
        var act = () => new Initiative(QuarterId, "Maintenance", "#EF4444", tshirtSize: "M", kind: InitiativeKind.Bucket);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        var act = () => new Initiative(QuarterId, name!, "#8B5CF6");

        act.Should().Throw<ArgumentException>().WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Constructor_NameTooLong_Throws()
    {
        var act = () => new Initiative(QuarterId, new string('a', 201), "#8B5CF6");

        act.Should().Throw<ArgumentException>().WithMessage("*200 characters*");
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void Constructor_InvalidColor_Throws(string color)
    {
        var act = () => new Initiative(QuarterId, "Option", color);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("S")]
    [InlineData("M")]
    [InlineData("L")]
    [InlineData("L+")]
    [InlineData("XL")]
    [InlineData("XL+")]
    [InlineData("XXL")]
    [InlineData("xs")]
    public void Constructor_SizeOnTheScale_IsAccepted(string size)
    {
        var act = () => CreateOption(size: size);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("XM")]
    [InlineData("L++")]
    [InlineData("HUGE")]
    [InlineData("3")]
    public void Constructor_SizeOffTheScale_Throws(string size)
    {
        var act = () => CreateOption(size: size);

        act.Should().Throw<ArgumentException>().WithMessage("*T-shirt size*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Constructor_RankOutOfRange_Throws(int rank)
    {
        var act = () => new Initiative(QuarterId, "Option", "#8B5CF6", rank: rank);

        act.Should().Throw<ArgumentException>().WithMessage("*Priority*");
    }

    [Fact]
    public void Constructor_UnknownWorkType_Throws()
    {
        var act = () => new Initiative(QuarterId, "Option", "#8B5CF6", workType: (WorkType)9);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(WorkType.Maintenance, "Maintenance")]
    [InlineData(WorkType.TechRoadmap, "Tech roadmap")]
    public void CreateBucket_Lane_IsCommittedAndNamed(WorkType workType, string name)
    {
        var lane = Initiative.CreateBucket(QuarterId, workType);

        lane.IsBucket.Should().BeTrue();
        lane.Name.Should().Be(name);
        lane.WorkType.Should().Be(workType);
        lane.Status.Should().Be(InitiativeStatus.Committed);
    }

    [Fact]
    public void CreateBucket_ProductRoadmap_Throws()
    {
        var act = () => Initiative.CreateBucket(QuarterId, WorkType.ProductRoadmap);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Commit_Lane_Throws()
    {
        var lane = Initiative.CreateBucket(QuarterId, WorkType.Maintenance);

        var act = () => lane.Drop();

        act.Should().Throw<InvalidOperationException>().WithMessage("*always committed*");
    }

    [Fact]
    public void Update_LaneWorkType_Throws()
    {
        var lane = Initiative.CreateBucket(QuarterId, WorkType.Maintenance);

        var act = () => lane.Update("Maintenance", null, null, null, WorkType.TechRoadmap, null, null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_OptionToLane_Throws()
    {
        var option = CreateOption();

        var act = () => option.Update("Option", null, null, null, null, InitiativeKind.Bucket, null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_ValidData_ReplacesDetails()
    {
        var option = CreateOption();

        option.Update("WH off-cycle", "Release needs CS", null, "https://example.com", WorkType.TechRoadmap, InitiativeKind.Leftover, null, new DateTime(2026, 9, 30));

        option.Name.Should().Be("WH off-cycle");
        option.Description.Should().Be("Release needs CS");
        option.Color.Should().Be("#8B5CF6");
        option.WorkType.Should().Be(WorkType.TechRoadmap);
        option.Kind.Should().Be(InitiativeKind.Leftover);
        option.DueDate.Should().Be(new DateTime(2026, 9, 30));
        option.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetSize_ReadyForCommitment_SizesIt()
    {
        var option = CreateOption();
        option.MarkReadyForCommitment();

        option.SetSize("xl");

        option.TshirtSize.Should().Be("XL");
    }

    [Theory]
    [InlineData(InitiativeStatus.InDiscovery)]
    [InlineData(InitiativeStatus.InSolutionDesign)]
    public void SetSize_BeforeReady_Throws(InitiativeStatus status)
    {
        var option = CreateOption();
        if (status == InitiativeStatus.InSolutionDesign) option.MarkInSolutionDesign();

        var act = () => option.SetSize("M");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ready for commitment*");
    }

    [Fact]
    public void SetSize_Null_ClearsTheSize()
    {
        var option = CreateOption(size: "L");

        option.SetSize(null);

        option.TshirtSize.Should().BeNull();
    }

    [Theory]
    [InlineData(InitiativeStatus.InDiscovery)]
    [InlineData(InitiativeStatus.InSolutionDesign)]
    public void MovingBackBeforeReady_ClearsTheSize(InitiativeStatus status)
    {
        var option = CreateOption(size: "L");

        if (status == InitiativeStatus.InDiscovery) option.MarkInDiscovery();
        else option.MarkInSolutionDesign();

        option.TshirtSize.Should().BeNull();
    }

    [Fact]
    public void Decisions_KeepTheSize()
    {
        var option = CreateOption(size: "L");

        option.Commit();
        option.MakeStretch();

        option.TshirtSize.Should().Be("L");
    }

    [Fact]
    public void MarkReadyForCommitment_OpenReadinessIssues_Throws()
    {
        var option = CreateOption();
        option.SetReadiness(ReadinessIssue.RequirementsPending);

        var act = () => option.MarkReadyForCommitment();

        act.Should().Throw<InvalidOperationException>().WithMessage("*readiness issues*");
    }

    [Fact]
    public void MarkReadyForCommitment_NoIssues_IsReady()
    {
        var option = CreateOption();

        option.MarkReadyForCommitment();

        option.Status.Should().Be(InitiativeStatus.ReadyForCommitment);
    }

    [Fact]
    public void SetReadiness_IssueOnReadyOption_PutsItBackInSolutionDesignUnsized()
    {
        var option = CreateOption(size: "L");

        option.SetReadiness(ReadinessIssue.ReleaseCoordination | ReadinessIssue.EstimationPending);

        option.Status.Should().Be(InitiativeStatus.InSolutionDesign);
        option.HasReadinessIssues.Should().BeTrue();
        option.TshirtSize.Should().BeNull();
    }

    [Fact]
    public void SetReadiness_UnknownFlag_Throws()
    {
        var act = () => CreateOption().SetReadiness((ReadinessIssue)64);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Drop_WithReason_RecordsDecision()
    {
        var option = CreateOption();

        option.Drop("  Requirements will not be ready  ");

        option.Status.Should().Be(InitiativeStatus.Dropped);
        option.DecisionNote.Should().Be("Requirements will not be ready");
        option.IsInPlay.Should().BeFalse();
    }

    [Fact]
    public void Commit_NoteTooLong_Throws()
    {
        var act = () => CreateOption().Commit(new string('x', 1001));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(InitiativeStatus.InDiscovery)]
    [InlineData(InitiativeStatus.ReadyForCommitment)]
    [InlineData(InitiativeStatus.Dropped)]
    public void MarkDelivered_NotTakenOn_Throws(InitiativeStatus status)
    {
        var option = CreateOption();
        if (status == InitiativeStatus.ReadyForCommitment) option.MarkReadyForCommitment();
        if (status == InitiativeStatus.Dropped) option.Drop();

        var act = () => option.MarkDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDelivered_Stretch_IsDelivered()
    {
        var option = CreateOption();
        option.MakeStretch();

        option.MarkDelivered();

        option.Status.Should().Be(InitiativeStatus.Delivered);
    }

    [Fact]
    public void CarryOverTo_NothingPending_IsLeftoverReadyWithItsSize()
    {
        var option = new Initiative(QuarterId, "YT Consolidation", "#8B5CF6", "notes", "XL", "https://example.com",
            WorkType.ProductRoadmap, InitiativeKind.Option, 4, status: InitiativeStatus.ReadyForCommitment);
        option.Commit();
        var next = Guid.NewGuid();

        var copy = option.CarryOverTo(next);

        copy.Id.Should().NotBe(option.Id);
        copy.QuarterId.Should().Be(next);
        copy.Kind.Should().Be(InitiativeKind.Leftover);
        copy.Status.Should().Be(InitiativeStatus.ReadyForCommitment);
        copy.TshirtSize.Should().Be("XL");
        copy.Rank.Should().Be(0);
    }

    [Fact]
    public void CarryOverTo_StillWaiting_IsLeftoverInSolutionDesignUnsized()
    {
        var option = new Initiative(QuarterId, "YT Consolidation", "#8B5CF6", tshirtSize: "XL", status: InitiativeStatus.ReadyForCommitment);
        option.Commit();
        option.SetReadiness(ReadinessIssue.RequirementsPending);

        var copy = option.CarryOverTo(Guid.NewGuid());

        copy.Status.Should().Be(InitiativeStatus.InSolutionDesign);
        copy.TshirtSize.Should().BeNull();
        copy.Readiness.Should().Be(ReadinessIssue.RequirementsPending);
    }

    [Fact]
    public void CarryOverTo_SameQuarter_Throws()
    {
        var act = () => CreateOption().CarryOverTo(QuarterId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CarryOverTo_Lane_Throws()
    {
        var act = () => Initiative.CreateBucket(QuarterId, WorkType.Maintenance).CarryOverTo(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetEarliestSprint_SetsSprint()
    {
        var option = CreateOption();
        var sprintId = Guid.NewGuid();

        option.SetEarliestSprint(sprintId);

        option.EarliestSprintId.Should().Be(sprintId);
    }
}
