using FluentAssertions;
using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;

namespace Hive.Tests.Infrastructure.Repositories;

public class QuarterMemberRepositoryTests
{
    private readonly InMemoryDbContext _context = new();
    private readonly QuarterMemberRepository _repository;

    public QuarterMemberRepositoryTests()
    {
        _repository = new QuarterMemberRepository(_context);
    }

    [Fact]
    public async Task GetByQuarterAndDirectReportAsync_FindsTheirRow()
    {
        var quarterId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await _repository.AddAsync(new QuarterMember(quarterId, personId, 60));
        await _repository.AddAsync(new QuarterMember(Guid.NewGuid(), personId));

        var result = await _repository.GetByQuarterAndDirectReportAsync(quarterId, personId);

        result!.CapacityPercent.Should().Be(60);
    }

    [Fact]
    public async Task DeleteByQuarterAsync_RemovesOnlyThatQuarter()
    {
        var quarterId = Guid.NewGuid();
        await _repository.AddAsync(new QuarterMember(quarterId, Guid.NewGuid()));
        await _repository.AddAsync(new QuarterMember(quarterId, Guid.NewGuid()));
        var other = await _repository.AddAsync(new QuarterMember(Guid.NewGuid(), Guid.NewGuid()));

        await _repository.DeleteByQuarterAsync(quarterId);

        (await _repository.GetByQuarterAsync(quarterId)).Should().BeEmpty();
        (await _repository.GetByIdAsync(other.Id)).Should().NotBeNull();
    }
}
