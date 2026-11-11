using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class OneOnOneCadenceTests
{
    private static readonly DateOnly Today = new(2026, 9, 24);

    [Fact]
    public void Evaluate_WithoutA1on1_IsNeverMet()
    {
        OneOnOneCadence.Evaluate(null, Today, 14).Should().Be(OneOnOneCadenceStatus.NeverMet);
    }

    [Theory]
    [InlineData(0, OneOnOneCadenceStatus.OnTrack)]
    [InlineData(11, OneOnOneCadenceStatus.OnTrack)]
    [InlineData(12, OneOnOneCadenceStatus.DueSoon)]
    [InlineData(14, OneOnOneCadenceStatus.DueSoon)]
    [InlineData(15, OneOnOneCadenceStatus.Overdue)]
    [InlineData(60, OneOnOneCadenceStatus.Overdue)]
    public void Evaluate_ComparesDaysSinceTheLast1on1WithTheCadence(int daysAgo, OneOnOneCadenceStatus expected)
    {
        OneOnOneCadence.Evaluate(Today.AddDays(-daysAgo), Today, 14).Should().Be(expected);
    }

    [Fact]
    public void Evaluate_WithAOneDayCadence_IsDueTheNextDay()
    {
        OneOnOneCadence.Evaluate(Today, Today, 1).Should().Be(OneOnOneCadenceStatus.DueSoon);
        OneOnOneCadence.Evaluate(Today.AddDays(-2), Today, 1).Should().Be(OneOnOneCadenceStatus.Overdue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(91)]
    public void Evaluate_WithACadenceOutOfRange_Throws(int cadenceDays)
    {
        var act = () => OneOnOneCadence.Evaluate(Today, Today, cadenceDays);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(OneOnOneCadenceStatus.OnTrack, false)]
    [InlineData(OneOnOneCadenceStatus.DueSoon, false)]
    [InlineData(OneOnOneCadenceStatus.Overdue, true)]
    [InlineData(OneOnOneCadenceStatus.NeverMet, true)]
    public void NeedsAttention_OnlyForOverdueOrNeverMet(OneOnOneCadenceStatus status, bool expected)
    {
        OneOnOneCadence.NeedsAttention(status).Should().Be(expected);
    }
}
