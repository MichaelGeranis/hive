using FluentAssertions;
using Hive.Core.Entities;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Tests.Core.Entities;

public class TaskReadinessTests
{
    private static readonly DateTime Today = new(2026, 9, 30);

    private static Sprint SprintFrom(string name, int startOffset, int endOffset)
    {
        var sprint = new Sprint(name);
        sprint.UpdateDates(Today.AddDays(startOffset), Today.AddDays(endOffset));
        return sprint;
    }

    private readonly Sprint _past = SprintFrom("MAR_3Q26_S5", -17, -4);
    private readonly Sprint _current = SprintFrom("MAR_3Q26_S6", -3, 10);
    private readonly Sprint _next = SprintFrom("MAR_4Q26_S1", 11, 24);
    private readonly Sprint _later = SprintFrom("MAR_4Q26_S2", 25, 38);

    private Sprint[] Sprints => [_past, _current, _next, _later];

    [Fact]
    public void FindSprints_ReturnsTheCurrentAndTheNext()
    {
        var (current, next) = TaskReadiness.FindSprints(Sprints, Today);

        current.Should().Be(_current);
        next.Should().Be(_next);
    }

    [Fact]
    public void FindSprints_NoSprints_ReturnsNeither()
    {
        TaskReadiness.FindSprints([], Today).Should().Be(((Sprint?)null, (Sprint?)null));
    }

    [Fact]
    public void FindUpcoming_KeepsUnstartedWorkInTheCurrentAndNextSprint()
    {
        var inCurrent = new TeamTask("Current story", type: TaskType.Story, sprint: _current.Name);
        var inNext = new TeamTask("Next bug", type: TaskType.Bug, sprint: _next.Name);
        var later = new TeamTask("Later", sprint: _later.Name);
        var past = new TeamTask("Past", sprint: _past.Name);
        var noSprint = new TeamTask("No sprint");
        var started = new TeamTask("Started", sprint: _current.Name);
        started.Start();
        var epic = new TeamTask("Epic", type: TaskType.Epic, sprint: _current.Name);

        var upcoming = TaskReadiness.FindUpcoming([inNext, later, past, noSprint, started, epic, inCurrent], Sprints, Today);

        upcoming.Should().Equal(inCurrent, inNext);
    }

    [Fact]
    public void FindUpcoming_CarriedOverWork_CountsByItsLatestSprint()
    {
        var carried = new TeamTask("Carried", sprint: $"{_past.Name},{_current.Name}");
        var movedOn = new TeamTask("Moved on", sprint: $"{_current.Name},{_later.Name}");

        TaskReadiness.FindUpcoming([carried, movedOn], Sprints, Today).Should().Equal(carried);
    }

    [Fact]
    public void FindUpcoming_ToDoCounts_AndHigherPriorityComesFirst()
    {
        var low = new TeamTask("Low", priority: TaskPriority.Low, sprint: _current.Name);
        var critical = new TeamTask("Critical", priority: TaskPriority.Critical, sprint: _current.Name);
        critical.MoveToTodo();

        var upcoming = TaskReadiness.FindUpcoming([low, critical], Sprints, Today);

        upcoming.Should().Equal(critical, low);
        critical.Status.Should().Be(TaskStatus.Todo);
    }

    [Fact]
    public void Fingerprint_ChangesWithTheDescription_NotWithTheStatus()
    {
        var task = new TeamTask("Story", "As a user", TaskType.Story);
        var before = TaskReadiness.Fingerprint(task);

        task.MoveToTodo();
        TaskReadiness.Fingerprint(task).Should().Be(before);

        task.Update("Story", "As a user I want more", TaskType.Story, TaskPriority.Medium, null, null, null);
        TaskReadiness.Fingerprint(task).Should().NotBe(before);
    }

    [Theory]
    [InlineData("Ready", TaskReadinessVerdict.Ready)]
    [InlineData("needs work", TaskReadinessVerdict.NeedsWork)]
    [InlineData("NeedsWork", TaskReadinessVerdict.NeedsWork)]
    [InlineData("UNCLEAR", TaskReadinessVerdict.Unclear)]
    public void TryParseVerdict_ReadsTheName(string value, TaskReadinessVerdict expected)
    {
        TaskReadiness.TryParseVerdict(value, out var verdict).Should().BeTrue();
        verdict.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("Maybe")]
    public void TryParseVerdict_NotAVerdict_IsFalse(string? value)
    {
        TaskReadiness.TryParseVerdict(value, out _).Should().BeFalse();
    }
}
