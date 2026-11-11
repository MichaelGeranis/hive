using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

public class QuarterlyPlanningServiceTests
{
    private readonly Mock<IQuarterRepository> _quarters = new();
    private readonly Mock<IInitiativeRepository> _initiatives = new();
    private readonly Mock<IAllocationRepository> _allocations = new();
    private readonly Mock<ISprintGoalRepository> _sprintGoals = new();
    private readonly Mock<IInitiativeDependencyRepository> _dependencies = new();
    private readonly Mock<IInitiativeMemberRepository> _members = new();
    private readonly Mock<IExternalDependencyRepository> _externalDependencies = new();
    private readonly Mock<IQuarterMemberRepository> _quarterMembers = new();
    private readonly Mock<IInitiativeProductRepository> _initiativeProducts = new();
    private readonly Mock<ISprintRepository> _sprints = new();
    private readonly Mock<IDirectReportRepository> _people = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IActivityService> _activity = new();
    private readonly QuarterlyPlanningService _service;

    private readonly Quarter _quarter = new(2026, 4);
    private readonly Sprint _sprint = new("MAR_4Q26_S1");
    private readonly DirectReport _alice = new("Alice", "Smith", "alice@example.com", "Engineer", "Eng", new DateTime(2020, 1, 1));

    public QuarterlyPlanningServiceTests()
    {
        _service = new QuarterlyPlanningService(
            _quarters.Object, _initiatives.Object, _allocations.Object, _sprintGoals.Object, _dependencies.Object,
            _members.Object, _externalDependencies.Object, _quarterMembers.Object,
            _initiativeProducts.Object, _sprints.Object, _people.Object, _products.Object, _activity.Object);

        _quarters.Setup(r => r.GetByIdAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_quarter);
        _sprints.Setup(r => r.GetByIdAsync(_sprint.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_sprint);
        _sprints.Setup(r => r.GetByYearQuarterAsync(2026, 4, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { _sprint });
        _people.Setup(r => r.GetByIdAsync(_alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_alice);
        _people.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { _alice });
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Initiative>());
        _allocations.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Allocation>());
        _quarterMembers.Setup(r => r.GetByQuarterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuarterMember>());
    }

    private Initiative AddInitiative(string name = "Euterpe", InitiativeKind kind = InitiativeKind.Option)
    {
        var initiative = kind == InitiativeKind.Bucket
            ? Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance)
            : new Initiative(_quarter.Id, name, "#8B5CF6", kind: kind);
        _initiatives.Setup(r => r.GetByIdAsync(initiative.Id, It.IsAny<CancellationToken>())).ReturnsAsync(initiative);
        return initiative;
    }

    [Fact]
    public async Task CreateQuarter_New_AddsBothStandingLanes()
    {
        var added = new List<Initiative>();
        _initiatives.Setup(r => r.AddAsync(Capture.In(added), It.IsAny<CancellationToken>())).ReturnsAsync((Initiative i, CancellationToken _) => i);

        var quarter = await _service.CreateQuarterAsync(new CreateQuarterDto { Year = 2027, QuarterNumber = 1 });

        quarter.Name.Should().Be("Q1 2027");
        quarter.ProductRoadmapPercent.Should().Be(50);
        added.Should().HaveCount(2).And.OnlyContain(i => i.IsBucket);
        added.Select(i => i.WorkType).Should().BeEquivalentTo(new[] { WorkType.Maintenance, WorkType.TechRoadmap });
    }

    [Fact]
    public async Task CreateQuarter_Existing_ThrowsConflict()
    {
        _quarters.Setup(r => r.ExistsAsync(2026, 4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.CreateQuarterAsync(new CreateQuarterDto { Year = 2026, QuarterNumber = 4 });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateQuarter_NewMix_SetsTarget()
    {
        var quarter = await _service.UpdateQuarterAsync(_quarter.Id, new UpdateQuarterDto { ProductRoadmapPercent = 40, TechRoadmapPercent = 20, MaintenancePercent = 40 });

        quarter.MaintenancePercent.Should().Be(40);
        _quarters.Verify(r => r.UpdateAsync(_quarter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInitiative_ExistingOptions_RanksLastWithUnusedColor()
    {
        var first = new Initiative(_quarter.Id, "First", Initiative.AvailableColors[0], rank: 1);
        var second = new Initiative(_quarter.Id, "Second", Initiative.AvailableColors[1], rank: 2);
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { first, second });
        Initiative? created = null;
        _initiatives.Setup(r => r.AddAsync(It.IsAny<Initiative>(), It.IsAny<CancellationToken>()))
            .Callback<Initiative, CancellationToken>((i, _) => created = i)
            .ReturnsAsync((Initiative i, CancellationToken _) => i);

        await _service.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = _quarter.Id, Name = "Third", Status = (int)InitiativeStatus.ReadyForCommitment, TshirtSize = "L+" });

        created!.Rank.Should().Be(3);
        created.Color.Should().Be(Initiative.AvailableColors[2]);
        created.TshirtSize.Should().Be("L+");
    }

    [Fact]
    public async Task CreateInitiative_SizedInDiscovery_Throws()
    {
        var act = () => _service.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = _quarter.Id, Name = "Euterpe", TshirtSize = "M" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ready for commitment*");
    }

    [Fact]
    public async Task CreateInitiative_WithStatus_StartsThere()
    {
        Initiative? created = null;
        _initiatives.Setup(r => r.AddAsync(It.IsAny<Initiative>(), It.IsAny<CancellationToken>()))
            .Callback<Initiative, CancellationToken>((i, _) => created = i)
            .ReturnsAsync((Initiative i, CancellationToken _) => i);

        await _service.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = _quarter.Id, Name = "Euterpe", Status = (int)InitiativeStatus.InSolutionDesign });

        created!.Status.Should().Be(InitiativeStatus.InSolutionDesign);
        created.TshirtSize.Should().BeNull();
    }

    [Fact]
    public async Task CreateInitiative_AsLane_Throws()
    {
        var act = () => _service.CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = _quarter.Id, Name = "Lane", Kind = (int)InitiativeKind.Bucket });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddBucket_LaneAlreadyThere_ThrowsConflict()
    {
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance) });

        var act = () => _service.AddBucketAsync(_quarter.Id, new AddBucketDto { WorkType = (int)WorkType.Maintenance });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task UpdateInitiative_ProductIds_ReplacesLinks()
    {
        var option = AddInitiative();
        var product = new Product("Rights Manager");
        _products.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _service.UpdateInitiativeAsync(option.Id, new UpdateInitiativeDto
        {
            Name = "Euterpe",
            Readiness = (int)ReadinessIssue.RequirementsPending,
            ProductIds = new[] { product.Id, product.Id }
        });

        option.Readiness.Should().Be(ReadinessIssue.RequirementsPending);
        _initiativeProducts.Verify(r => r.DeleteByInitiativeAsync(option.Id, It.IsAny<CancellationToken>()), Times.Once);
        _initiativeProducts.Verify(r => r.AddAsync(It.Is<InitiativeProduct>(p => p.ProductId == product.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateInitiative_EarliestSprintOfAnotherQuarter_Throws()
    {
        var option = AddInitiative();
        var other = new Sprint("MAR_1Q27_S1");
        _sprints.Setup(r => r.GetByIdAsync(other.Id, It.IsAny<CancellationToken>())).ReturnsAsync(other);

        var act = () => _service.UpdateInitiativeAsync(option.Id, new UpdateInitiativeDto { Name = "Euterpe", EarliestSprintId = other.Id });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not a sprint of Q4 2026*");
    }

    [Fact]
    public async Task ChangeStatus_Dropped_RecordsReason()
    {
        var option = AddInitiative();

        await _service.ChangeInitiativeStatusAsync(option.Id, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.Dropped, Note = "Not ready" });

        option.Status.Should().Be(InitiativeStatus.Dropped);
        option.DecisionNote.Should().Be("Not ready");
    }

    [Fact]
    public async Task ChangeStatus_ReadyWithOpenIssues_Throws()
    {
        var option = AddInitiative();
        option.SetReadiness(ReadinessIssue.EstimationPending);

        var act = () => _service.ChangeInitiativeStatusAsync(option.Id, new ChangeInitiativeStatusDto { Status = (int)InitiativeStatus.ReadyForCommitment });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Reorder_Options_RanksInGivenOrderSkippingLanes()
    {
        var a = new Initiative(_quarter.Id, "A", "#8B5CF6", rank: 1);
        var b = new Initiative(_quarter.Id, "B", "#8B5CF6", rank: 2);
        var lane = Initiative.CreateBucket(_quarter.Id, WorkType.Maintenance);
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b, lane });

        await _service.ReorderInitiativesAsync(_quarter.Id, new ReorderInitiativesDto { OrderedIds = new[] { b.Id, lane.Id, a.Id } });

        b.Rank.Should().Be(1);
        a.Rank.Should().Be(2);
        lane.Rank.Should().Be(0);
    }

    [Fact]
    public async Task AddMember_NewLead_DemotesPreviousLead()
    {
        var option = AddInitiative();
        var previous = new InitiativeMember(option.Id, Guid.NewGuid(), InitiativeRole.Lead);
        _members.Setup(r => r.GetByInitiativeAsync(option.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { previous });

        await _service.AddInitiativeMemberAsync(option.Id, new CreateInitiativeMemberDto { DirectReportId = _alice.Id, Role = (int)InitiativeRole.Lead });

        previous.Role.Should().Be(InitiativeRole.Contributor);
        _members.Verify(r => r.AddAsync(It.Is<InitiativeMember>(m => m.IsLead && m.DirectReportId == _alice.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMember_AlreadyOnInitiative_ThrowsConflict()
    {
        var option = AddInitiative();
        _members.Setup(r => r.ExistsAsync(option.Id, _alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.AddInitiativeMemberAsync(option.Id, new CreateInitiativeMemberDto { DirectReportId = _alice.Id });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SetCells_NewShares_ReplacesTheCell()
    {
        var a = AddInitiative("A");
        var b = AddInitiative("B");
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b });
        var old = new Allocation(a.Id, _alice.Id, _sprint.Id);
        _allocations.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { old });

        await _service.SetCellsAsync(_quarter.Id, new SetCellsDto
        {
            Cells = new[]
            {
                new CellDto
                {
                    DirectReportId = _alice.Id, SprintId = _sprint.Id,
                    Shares = new[] { new CellShareDto { InitiativeId = a.Id, Percent = 50 }, new CellShareDto { InitiativeId = b.Id, Percent = 50 } }
                }
            }
        });

        old.Percent.Should().Be(50);
        _allocations.Verify(r => r.UpdateAsync(old, It.IsAny<CancellationToken>()), Times.Once);
        _allocations.Verify(r => r.AddAsync(It.Is<Allocation>(x => x.InitiativeId == b.Id && x.Percent == 50), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCells_EmptyShares_ClearsTheCell()
    {
        var a = AddInitiative("A");
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a });
        var old = new Allocation(a.Id, _alice.Id, _sprint.Id);
        _allocations.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { old });

        await _service.SetCellsAsync(_quarter.Id, new SetCellsDto { Cells = new[] { new CellDto { DirectReportId = _alice.Id, SprintId = _sprint.Id } } });

        _allocations.Verify(r => r.DeleteAsync(old.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCells_BeyondWholeSprint_ThrowsAndWritesNothing()
    {
        var a = AddInitiative("A");
        var b = AddInitiative("B");
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b });

        var act = () => _service.SetCellsAsync(_quarter.Id, new SetCellsDto
        {
            Cells = new[]
            {
                new CellDto
                {
                    DirectReportId = _alice.Id, SprintId = _sprint.Id,
                    Shares = new[] { new CellShareDto { InitiativeId = a.Id, Percent = 80 }, new CellShareDto { InitiativeId = b.Id, Percent = 40 } }
                }
            }
        });

        await act.Should().ThrowAsync<ArgumentException>();
        _allocations.Verify(r => r.AddAsync(It.IsAny<Allocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetCells_SprintOfAnotherQuarter_ThrowsNotFound()
    {
        var act = () => _service.SetCellsAsync(_quarter.Id, new SetCellsDto
        {
            Cells = new[] { new CellDto { DirectReportId = _alice.Id, SprintId = Guid.NewGuid() } }
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetRosterMember_DefaultRoster_MaterialisesItFirst()
    {
        var added = new List<QuarterMember>();
        _quarterMembers.Setup(r => r.AddAsync(Capture.In(added), It.IsAny<CancellationToken>())).ReturnsAsync((QuarterMember m, CancellationToken _) => m);
        var materialised = new QuarterMember(_quarter.Id, _alice.Id);
        _quarterMembers.Setup(r => r.GetByQuarterAndDirectReportAsync(_quarter.Id, _alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(materialised);

        await _service.SetRosterMemberAsync(_quarter.Id, new SetRosterMemberDto { DirectReportId = _alice.Id, CapacityPercent = 50 });

        added.Should().ContainSingle(m => m.DirectReportId == _alice.Id && m.CapacityPercent == 100);
        materialised.CapacityPercent.Should().Be(50);
    }

    [Fact]
    public async Task RemoveRosterMember_OnRoster_RemovesTheirCells()
    {
        var member = new QuarterMember(_quarter.Id, _alice.Id);
        _quarterMembers.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { member });
        _quarterMembers.Setup(r => r.GetByQuarterAndDirectReportAsync(_quarter.Id, _alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        var cell = new Allocation(Guid.NewGuid(), _alice.Id, _sprint.Id);
        _allocations.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { cell });

        await _service.RemoveRosterMemberAsync(_quarter.Id, _alice.Id);

        _allocations.Verify(r => r.DeleteAsync(cell.Id, It.IsAny<CancellationToken>()), Times.Once);
        _quarterMembers.Verify(r => r.DeleteAsync(member.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CarryOver_NextQuarterMissing_CreatesItWithLeftoverAndOpenWork()
    {
        var option = AddInitiative("YT Consolidation");
        _externalDependencies.Setup(r => r.GetByInitiativeAsync(option.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ExternalDependency(option.Id, "CORE"), new ExternalDependency(option.Id, "CS", null, ExternalDependencyStatus.Done) });
        _initiativeProducts.Setup(r => r.GetByInitiativeAsync(option.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<InitiativeProduct>());
        _members.Setup(r => r.GetByInitiativeAsync(option.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<InitiativeMember>());
        var added = new List<Initiative>();
        _initiatives.Setup(r => r.AddAsync(Capture.In(added), It.IsAny<CancellationToken>())).ReturnsAsync((Initiative i, CancellationToken _) => i);

        await _service.CarryOverInitiativeAsync(option.Id);

        _quarters.Verify(r => r.AddAsync(It.Is<Quarter>(q => q.Year == 2027 && q.QuarterNumber == 1), It.IsAny<CancellationToken>()), Times.Once);
        added.Should().ContainSingle(i => i.Kind == InitiativeKind.Leftover && i.Name == "YT Consolidation");
        _externalDependencies.Verify(r => r.AddAsync(It.Is<ExternalDependency>(d => d.Team == "CORE"), It.IsAny<CancellationToken>()), Times.Once);
        _externalDependencies.Verify(r => r.AddAsync(It.Is<ExternalDependency>(d => d.Team == "CS"), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateDependency_AcrossQuarters_Throws()
    {
        var a = AddInitiative("A");
        var other = new Initiative(Guid.NewGuid(), "B", "#8B5CF6");
        _initiatives.Setup(r => r.GetByIdAsync(other.Id, It.IsAny<CancellationToken>())).ReturnsAsync(other);

        var act = () => _service.CreateDependencyAsync(new CreateInitiativeDependencyDto { DependentInitiativeId = a.Id, DependencyInitiativeId = other.Id });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteQuarter_Existing_DeletesEverythingPlannedInIt()
    {
        var option = AddInitiative();
        _initiatives.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { option });
        _sprintGoals.Setup(r => r.GetByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SprintGoal>());

        await _service.DeleteQuarterAsync(_quarter.Id);

        _externalDependencies.Verify(r => r.DeleteByInitiativeAsync(option.Id, It.IsAny<CancellationToken>()), Times.Once);
        _quarterMembers.Verify(r => r.DeleteByQuarterAsync(_quarter.Id, It.IsAny<CancellationToken>()), Times.Once);
        _quarters.Verify(r => r.DeleteAsync(_quarter.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSprintNotes_NoNotesYet_AddsThem()
    {
        await _service.SetSprintNotesAsync(_quarter.Id, _sprint.Id, new SprintNotesDto { Notes = "Euterpe BE ready" });

        _sprintGoals.Verify(r => r.AddAsync(It.Is<SprintGoal>(g => g.Notes == "Euterpe BE ready"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
