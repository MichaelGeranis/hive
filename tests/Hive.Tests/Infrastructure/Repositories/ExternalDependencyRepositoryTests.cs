using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class ExternalDependencyRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly ExternalDependencyRepository _repository;
    private readonly Initiative _initiative;

    public ExternalDependencyRepositoryTests()
    {
        _repository = new ExternalDependencyRepository(_context);
        _initiative = new Initiative(Guid.NewGuid(), "Euterpe", "#8B5CF6");
        _context.Initiatives.TryAdd(_initiative.Id, _initiative);
    }

    [Fact]
    public async Task GetByInitiativeAsync_ReturnsByTeam()
    {
        await _repository.AddAsync(new ExternalDependency(_initiative.Id, "Research"));
        await _repository.AddAsync(new ExternalDependency(_initiative.Id, "CORE"));

        var result = await _repository.GetByInitiativeAsync(_initiative.Id);

        result.Select(d => d.Team).Should().Equal("CORE", "Research");
    }

    [Fact]
    public async Task UpdateAsync_Existing_StoresChange()
    {
        var dependency = await _repository.AddAsync(new ExternalDependency(_initiative.Id, "CS"));
        dependency.Update("CS", "Release window", ExternalDependencyStatus.Agreed);

        await _repository.UpdateAsync(dependency);

        (await _repository.GetByIdAsync(dependency.Id))!.Status.Should().Be(ExternalDependencyStatus.Agreed);
    }

    [Fact]
    public async Task GetByQuarterAsync_OnlyTheQuarterInitiatives()
    {
        await _repository.AddAsync(new ExternalDependency(_initiative.Id, "CORE"));
        await _repository.AddAsync(new ExternalDependency(Guid.NewGuid(), "BI"));

        (await _repository.GetByQuarterAsync(_initiative.QuarterId)).Should().ContainSingle(d => d.Team == "CORE");
    }
}
