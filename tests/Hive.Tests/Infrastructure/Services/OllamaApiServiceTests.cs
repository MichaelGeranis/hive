using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Infrastructure.Services;

/// <summary>
/// Tests for OllamaApiService, against a stubbed HTTP handler. No request leaves the process.
/// </summary>
public class OllamaApiServiceTests
{
    private const string SentimentJson = """
        {"positive":70,"neutral":20,"negative":10,"overallSentiment":"Positive",
         "keyThemes":["career growth"],
         "monthlyBreakdown":[{"year":2025,"month":10,"positive":70,"neutral":20,"negative":10,"notesCount":2}],
         "reasoning":"They shipped on time and want to lead the next project."}
        """;

    private static readonly OllamaConnection Connection = new("http://localhost:11434", "llama3.1:8b");

    private static readonly IReadOnlyList<MeetingNoteForAnalysis> Notes = new[]
    {
        new MeetingNoteForAnalysis("Weekly sync\n\n- Shipped the migration <on time>", new DateOnly(2025, 10, 6)),
        new MeetingNoteForAnalysis("Career chat\n\n- Wants to lead the next project", new DateOnly(2025, 10, 20))
    };

    [Fact]
    public async Task AnalyzeSentimentAsync_SendsTheModelThePromptAndTheSchema()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(SentimentJson)));
        var service = CreateService(handler);

        await service.AnalyzeSentimentAsync(Connection, Notes);

        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.ToString().Should().Be("http://localhost:11434/api/chat");

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be("llama3.1:8b");
        root.GetProperty("stream").GetBoolean().Should().BeFalse();
        root.GetProperty("format").GetProperty("required").EnumerateArray().Select(e => e.GetString())
            .Should().Contain(new[] { "positive", "keyThemes", "monthlyBreakdown", "reasoning" });
        root.GetProperty("options").GetProperty("temperature").GetDouble().Should().Be(0);
        root.GetProperty("options").GetProperty("num_ctx").GetInt32().Should().BeGreaterThanOrEqualTo(8192);

        var prompt = root.GetProperty("messages")[0].GetProperty("content").GetString();
        prompt.Should().Contain("<note date=\"2025-10-06\">");
        prompt.Should().Contain("Shipped the migration &lt;on time&gt;");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_ReadsTheAnswer()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(SentimentJson))));

        var result = await service.AnalyzeSentimentAsync(Connection, Notes);

        result.Positive.Should().Be(70);
        result.OverallSentiment.Should().Be("Positive");
        result.KeyThemes.Should().Equal("career growth");
        result.MonthlyBreakdown.Should().ContainSingle().Which.NotesCount.Should().Be(2);
        result.Reasoning.Should().Be("They shipped on time and want to lead the next project.");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WithoutReasoning_SaysSoRatherThanLeavingItEmpty()
    {
        const string withoutReasoning = """
            {"positive":70,"neutral":20,"negative":10,"overallSentiment":"Positive","keyThemes":[],"monthlyBreakdown":[]}
            """;
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(withoutReasoning))));

        var result = await service.AnalyzeSentimentAsync(Connection, Notes);

        result.Reasoning.Should().Be("The model gave no reasoning for this result.");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WithNoNotes_DoesNotCallOllama()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Ollama should not be called"));

        var result = await CreateService(handler).AnalyzeSentimentAsync(Connection, Array.Empty<MeetingNoteForAnalysis>());

        result.OverallSentiment.Should().Be("Neutral");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_SizesTheContextToFitManyNotes()
    {
        var longNotes = Enumerable.Range(0, 40)
            .Select(i => new MeetingNoteForAnalysis(new string('x', 3000), new DateOnly(2025, 10, 1).AddDays(i)))
            .ToList();
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(SentimentJson)));

        await CreateService(handler).AnalyzeSentimentAsync(Connection, longNotes);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var numCtx = body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32();
        numCtx.Should().BeGreaterThan(40 * 3000 / 3);
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenThePromptFilledTheContext_Throws()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(SentimentJson, promptEvalCount: 8192))));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*did not fit*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenTheAnswerIsCutOff_Throws()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat("{\"positive\":", doneReason: "length"))));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*cut off*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenTheAnswerIsNotJson_Throws()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat("I think they are happy"))));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*could not read*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenTheModelIsMissing_SaysHowToInstallIt()
    {
        var service = CreateService(new StubHandler(_ =>
            Json(HttpStatusCode.NotFound, """{"error":"model \"llama3.1:8b\" not found, try pulling it first"}""")));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*ollama pull llama3.1:8b*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenOllamaIsNotRunning_SaysSo()
    {
        var service = CreateService(new StubHandler(_ => throw new HttpRequestException("Connection refused")));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("Could not reach Ollama at http://localhost:11434*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_OnAnotherError_PassesOllamasMessageOn()
    {
        var service = CreateService(new StubHandler(_ =>
            Json(HttpStatusCode.InternalServerError, """{"error":"out of memory"}""")));

        var act = () => service.AnalyzeSentimentAsync(Connection, Notes);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*out of memory*");
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenTheModelIsInstalled_IsValid()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Tags("llama3.1:8b", "mistral:latest")));

        var (isValid, error) = await CreateService(handler).CheckConnectionAsync(Connection);

        isValid.Should().BeTrue();
        error.Should().BeNull();
        handler.Requests.Single().Uri.ToString().Should().Be("http://localhost:11434/api/tags");
    }

    [Fact]
    public async Task CheckConnectionAsync_WithoutATag_MatchesLatest()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Tags("mistral:latest"))));

        var (isValid, _) = await service.CheckConnectionAsync(new OllamaConnection("http://localhost:11434", "mistral"));

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenTheModelIsMissing_SaysHowToInstallIt()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Tags("mistral:latest"))));

        var (isValid, error) = await service.CheckConnectionAsync(Connection);

        isValid.Should().BeFalse();
        error.Should().Contain("ollama pull llama3.1:8b");
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenOllamaIsNotRunning_IsInvalid()
    {
        var service = CreateService(new StubHandler(_ => throw new HttpRequestException("Connection refused")));

        var (isValid, error) = await service.CheckConnectionAsync(Connection);

        isValid.Should().BeFalse();
        error.Should().StartWith("Could not reach Ollama");
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenSomethingElseAnswers_IsInvalid()
    {
        var service = CreateService(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html></html>", Encoding.UTF8, "application/json") }));

        var (isValid, error) = await service.CheckConnectionAsync(Connection);

        isValid.Should().BeFalse();
        error.Should().Contain("does not look like Ollama");
    }

    private const string RetroJson = """
        {"summary":"The team kept raising flaky tests.",
         "warnings":["Flaky tests raised in two retros"],
         "actionItems":[{"text":"Quarantine flaky tests","retroDate":"2026-09-28","owner":"Alice"},
                        {"text":"Pair on reviews","retroDate":"not a date","owner":""}],
         "suggestions":["Give the flaky test work an owner"]}
        """;

    private static readonly IReadOnlyList<RetroForAnalysis> Retros = new[]
    {
        new RetroForAnalysis("# 2026-09-28\n\n- Tests <flaky> again", new DateOnly(2026, 9, 28)),
        new RetroForAnalysis("# 2026-09-14\n\n- Tests flaky", new DateOnly(2026, 9, 14))
    };

    [Fact]
    public async Task SummarizeRetrosAsync_SendsTheRetrosOldestFirstWithTheSchema()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(RetroJson)));
        var service = CreateService(handler);

        await service.SummarizeRetrosAsync(Connection, Retros);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var root = body.RootElement;
        root.GetProperty("format").GetProperty("required").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("summary", "warnings", "actionItems", "suggestions");

        var prompt = root.GetProperty("messages")[0].GetProperty("content").GetString()!;
        prompt.IndexOf("<retro date=\"2026-09-14\">", StringComparison.Ordinal)
            .Should().BeLessThan(prompt.IndexOf("<retro date=\"2026-09-28\">", StringComparison.Ordinal));
        prompt.Should().Contain("Tests &lt;flaky&gt; again");
    }

    [Fact]
    public async Task SummarizeRetrosAsync_ReadsTheAnswer()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(RetroJson))));

        var result = await service.SummarizeRetrosAsync(Connection, Retros);

        result.Summary.Should().Be("The team kept raising flaky tests.");
        result.Warnings.Should().Equal("Flaky tests raised in two retros");
        result.ActionItems.Should().HaveCount(2);
        result.ActionItems[0].Should().Be(new RetroActionItem("Quarantine flaky tests", new DateOnly(2026, 9, 28), "Alice"));
        result.ActionItems[1].RetroDate.Should().BeNull();
        result.Suggestions.Should().Equal("Give the flaky test work an owner");
    }

    [Fact]
    public async Task SummarizeRetrosAsync_PromptFilledTheWindow_AsksForFewerRetros()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(RetroJson, promptEvalCount: 1_000_000))));

        var act = () => service.SummarizeRetrosAsync(Connection, Retros);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*Read fewer retros*");
    }

    [Fact]
    public async Task SummarizeRetrosAsync_NoSummary_Throws()
    {
        const string noSummary = """{"summary":"","warnings":[],"actionItems":[],"suggestions":[]}""";
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(noSummary))));

        var act = () => service.SummarizeRetrosAsync(Connection, Retros);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*empty answer*");
    }

    private static readonly TaskForReview ReviewedTask = new("Save drafts", "Story", "As a user I want <drafts> kept");

    [Fact]
    public async Task ReviewTaskAsync_SendsTheTaskWithTheSchema_AndReadsTheAnswer()
    {
        const string review = """{"verdict":"NeedsWork","summary":"Keeps drafts.","missing":["Acceptance criteria",""]}""";
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(review)));
        var service = CreateService(handler);

        var result = await service.ReviewTaskAsync(Connection, ReviewedTask);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var root = body.RootElement;
        root.GetProperty("format").GetProperty("required").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("verdict", "summary", "missing");
        var prompt = root.GetProperty("messages")[0].GetProperty("content").GetString()!;
        prompt.Should().Contain("<type>Story</type>");
        prompt.Should().Contain("<title>Save drafts</title>");
        prompt.Should().Contain("As a user I want &lt;drafts&gt; kept");

        result.Verdict.Should().Be(Hive.Core.Entities.TaskReadinessVerdict.NeedsWork);
        result.Summary.Should().Be("Keeps drafts.");
        result.Missing.Should().Equal("Acceptance criteria");
    }

    [Fact]
    public async Task ReviewTaskAsync_TellsTheModelWhetherHiveFoundAcceptanceCriteria()
    {
        const string review = """{"verdict":"Ready","summary":"Keeps drafts.","missing":[]}""";
        var withCriteria = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(review)));
        var without = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(review)));

        await CreateService(withCriteria).ReviewTaskAsync(Connection, ReviewedTask with { HasAcceptanceCriteria = true });
        await CreateService(without).ReviewTaskAsync(Connection, ReviewedTask);

        withCriteria.Requests.Single().Body.Should().Contain("Do not report acceptance criteria as missing");
        without.Requests.Single().Body.Should().Contain("has no acceptance criteria section");
    }

    [Fact]
    public async Task ReviewTaskAsync_UnknownVerdict_Throws()
    {
        const string review = """{"verdict":"Maybe","summary":"Keeps drafts.","missing":[]}""";
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Chat(review))));

        var act = () => service.ReviewTaskAsync(Connection, ReviewedTask);

        (await act.Should().ThrowAsync<OllamaApiException>()).WithMessage("*empty answer*");
    }

    [Fact]
    public async Task ReviewTaskAsync_NoDescription_SaysSoInThePrompt()
    {
        const string review = """{"verdict":"Unclear","summary":"A title only.","missing":["A description"]}""";
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Chat(review)));

        await CreateService(handler).ReviewTaskAsync(Connection, new TaskForReview("Fix it", "Bug", ""));

        handler.Requests.Single().Body.Should().Contain("(no description)");
    }

    private static OllamaApiService CreateService(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<OllamaApiService>.Instance);

    private static string Chat(string content, string doneReason = "stop", int promptEvalCount = 500) =>
        JsonSerializer.Serialize(new
        {
            model = "llama3.1:8b",
            message = new { role = "assistant", content },
            done = true,
            done_reason = doneReason,
            prompt_eval_count = promptEvalCount
        });

    private static string Tags(params string[] names) =>
        JsonSerializer.Serialize(new { models = names.Select(name => new { name, model = name }) });

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, body));
            return _respond(request);
        }
    }
}
