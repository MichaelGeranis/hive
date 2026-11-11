using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class CareerPlanRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly CareerPlanRepository _repository;
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    public CareerPlanRepositoryTests()
    {
        _repository = new CareerPlanRepository(_context);
    }

    private static CareerPlan Plan(Guid directReportId, DateTime createdAt)
    {
        var plan = new CareerPlan(directReportId, "Backend Engineer", CareerStage.Mid);
        typeof(CareerPlan).GetProperty(nameof(CareerPlan.CreatedAt))!.SetValue(plan, createdAt);
        return plan;
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new CareerPlanRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        var older = await _repository.AddAsync(Plan(_alice, new DateTime(2026, 1, 1)));
        var newer = await _repository.AddAsync(Plan(_bob, new DateTime(2026, 6, 1)));

        (await _repository.GetAllAsync()).Should().Equal(newer, older);
    }

    [Fact]
    public async Task GetByDirectReportIdAsync_AndExistsForDirectReportAsync_FindThePersonsPlan()
    {
        var plan = await _repository.AddAsync(Plan(_alice, DateTime.UtcNow));

        (await _repository.GetByDirectReportIdAsync(_alice)).Should().Be(plan);
        (await _repository.GetByDirectReportIdAsync(_bob)).Should().BeNull();
        (await _repository.ExistsForDirectReportAsync(_alice)).Should().BeTrue();
        (await _repository.ExistsForDirectReportAsync(_bob)).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ASecondPlanForTheSamePerson_Throws()
    {
        await _repository.AddAsync(Plan(_alice, DateTime.UtcNow));

        var act = () => _repository.AddAsync(Plan(_alice, DateTime.UtcNow));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesThePlan()
    {
        var plan = await _repository.AddAsync(Plan(_alice, DateTime.UtcNow));

        await _repository.DeleteAsync(plan.Id);

        (await _repository.GetByIdAsync(plan.Id)).Should().BeNull();
    }
}
