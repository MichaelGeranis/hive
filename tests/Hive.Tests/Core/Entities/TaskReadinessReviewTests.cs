using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class TaskReadinessReviewTests
{
    private static TaskReadinessReview Create(Guid? taskId = null, string summary = "Adds draft saving.", string fingerprint = "ABC") =>
        new(taskId ?? Guid.NewGuid(), TaskReadinessVerdict.NeedsWork, summary, "[\"Acceptance criteria\"]", "llama3.1:8b", fingerprint);

    [Fact]
    public void Constructor_ValidInput_SetsEverything()
    {
        var taskId = Guid.NewGuid();

        var review = Create(taskId);

        review.Id.Should().NotBeEmpty();
        review.TeamTaskId.Should().Be(taskId);
        review.Verdict.Should().Be(TaskReadinessVerdict.NeedsWork);
        review.Summary.Should().Be("Adds draft saving.");
        review.MissingJson.Should().Be("[\"Acceptance criteria\"]");
        review.Model.Should().Be("llama3.1:8b");
        review.AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_NoTask_Throws()
    {
        var act = () => Create(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_EmptySummary_Throws(string summary)
    {
        var act = () => Create(summary: summary);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NoFingerprint_Throws()
    {
        var act = () => Create(fingerprint: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_UnknownVerdict_Throws()
    {
        var act = () => new TaskReadinessReview(Guid.NewGuid(), (TaskReadinessVerdict)42, "ok", "[]", "m", "ABC");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LongSummary_IsTruncated()
    {
        var review = Create(summary: new string('a', TaskReadinessReview.MaxSummaryLength + 10));

        review.Summary.Length.Should().Be(TaskReadinessReview.MaxSummaryLength);
        review.Summary.Should().EndWith("…");
    }

    [Fact]
    public void Constructor_MissingListTooLong_Throws()
    {
        var act = () => new TaskReadinessReview(
            Guid.NewGuid(), TaskReadinessVerdict.Ready, "ok", new string('x', TaskReadinessReview.MaxJsonLength + 1), "m", "ABC");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ReplacesTheReview_KeepsTheTask()
    {
        var review = Create();
        var taskId = review.TeamTaskId;

        review.Update(TaskReadinessVerdict.Ready, "Clear now.", "", "Claude", "DEF");

        review.TeamTaskId.Should().Be(taskId);
        review.Verdict.Should().Be(TaskReadinessVerdict.Ready);
        review.MissingJson.Should().Be("[]");
        review.Model.Should().Be("Claude");
        review.IsStaleFor("DEF").Should().BeFalse();
        review.IsStaleFor("ABC").Should().BeTrue();
    }
}
