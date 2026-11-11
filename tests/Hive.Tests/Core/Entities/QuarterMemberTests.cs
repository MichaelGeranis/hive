using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class QuarterMemberTests
{
    [Fact]
    public void Constructor_NoCapacity_IsFullTime()
    {
        var member = new QuarterMember(Guid.NewGuid(), Guid.NewGuid());

        member.CapacityPercent.Should().Be(100);
        member.CapacityFactor.Should().Be(1m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Constructor_CapacityOutOfRange_Throws(int capacity)
    {
        var act = () => new QuarterMember(Guid.NewGuid(), Guid.NewGuid(), capacity);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCapacity_PartTime_SetsFactor()
    {
        var member = new QuarterMember(Guid.NewGuid(), Guid.NewGuid());

        member.SetCapacity(60);

        member.CapacityFactor.Should().Be(0.6m);
        member.UpdatedAt.Should().NotBeNull();
    }
}
