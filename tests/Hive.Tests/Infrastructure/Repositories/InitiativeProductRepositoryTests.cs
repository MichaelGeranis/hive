using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class InitiativeProductRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly InitiativeProductRepository _repository;

    public InitiativeProductRepositoryTests()
    {
        _repository = new InitiativeProductRepository(_context);
    }

    [Fact]
    public async Task GetByQuarterAsync_LinksOfTheQuarterInitiatives()
    {
        var initiative = new Initiative(Guid.NewGuid(), "Euterpe", "#8B5CF6");
        _context.Initiatives.TryAdd(initiative.Id, initiative);
        await _repository.AddAsync(new InitiativeProduct(initiative.Id, Guid.NewGuid()));
        await _repository.AddAsync(new InitiativeProduct(Guid.NewGuid(), Guid.NewGuid()));

        (await _repository.GetByQuarterAsync(initiative.QuarterId)).Should().ContainSingle(l => l.InitiativeId == initiative.Id);
    }

    [Fact]
    public async Task DeleteByInitiativeAsync_RemovesItsLinks()
    {
        var initiativeId = Guid.NewGuid();
        await _repository.AddAsync(new InitiativeProduct(initiativeId, Guid.NewGuid()));
        await _repository.AddAsync(new InitiativeProduct(initiativeId, Guid.NewGuid()));

        await _repository.DeleteByInitiativeAsync(initiativeId);

        (await _repository.GetByInitiativeAsync(initiativeId)).Should().BeEmpty();
    }
}
