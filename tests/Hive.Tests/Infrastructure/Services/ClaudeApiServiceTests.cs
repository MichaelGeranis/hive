using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Infrastructure.Services;

/// <summary>
/// Tests for ClaudeApiService, against a stubbed HTTP handler under the Anthropic SDK.
/// No request leaves the process.
/// </summary>
public class ClaudeApiServiceTests
{
    private const string SentimentJson = """
        {"positive":70,"neutral":20,"negative":10,"overallSentiment":"Positive",
         "keyThemes":["career growth"],
         "monthlyBreakdown":[{"year":2025,"month":10,"positive":70,"neutral":20,"negative":10,"notesCount":2}]}
        """;

    private static readonly IReadOnlyList<MeetingNoteForAnalysis> Notes = new[]
    {
        new MeetingNoteForAnalysis("Weekly sync\n\n- Shipped the migration <on time>", new DateOnly(2025, 10, 6)),
        new MeetingNoteForAnalysis("Career chat\n\n- Wants to lead the next project", new DateOnly(2025, 10, 20))
    };

    [Fact]
    public async Task AnalyzeSentimentAsync_SendsStructuredOutputRequestWithDefaultFallbacks()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Message(TextBlock(SentimentJson))));
        var service = CreateService(handler);

        await service.AnalyzeSentimentAsync("sk-test", Notes);

        var request = handler.Requests.Single();
        request.Uri.AbsolutePath.Should().Be("/v1/messages");
        request.Headers["x-api-key"].Should().Be("sk-test");
        request.Headers["anthropic-beta"].Should().Contain("server-side-fallback-2026-07-01");

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be("claude-opus-5");
        root.GetProperty("fallbacks").GetString().Should().Be("default");
        root.GetProperty("max_tokens").GetInt32().Should().Be(16000);
        root.GetProperty("output_config").GetProperty("format").GetProperty("type").GetString().Should().Be("json_schema");
        root.TryGetProperty("temperature", out _).Should().BeFalse();
        root.TryGetProperty("thinking", out _).Should().BeFalse("Claude Opus 5 thinks adaptively by default");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_PutsEachNoteInItsOwnEscapedBlock()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Message(TextBlock(SentimentJson))));
        var service = CreateService(handler);

        await service.AnalyzeSentimentAsync("sk-test", Notes);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var content = body.RootElement.GetProperty("messages")[0].GetProperty("content");
        var prompt = content.ValueKind == JsonValueKind.String
            ? content.GetString()!
            : string.Concat(content.EnumerateArray().Select(b => b.GetProperty("text").GetString()));
        prompt.Should().Contain("<note date=\"2025-10-06\">");
        prompt.Should().Contain("Shipped the migration &lt;on time&gt;");
        prompt.Should().Contain("</notes>");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_ParsesTheStructuredAnswer()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(TextBlock(SentimentJson)))));

        var result = await service.AnalyzeSentimentAsync("sk-test", Notes);

        result.Positive.Should().Be(70);
        result.Negative.Should().Be(10);
        result.OverallSentiment.Should().Be("Positive");
        result.KeyThemes.Should().Equal("career growth");
        result.MonthlyBreakdown.Should().ContainSingle()
            .Which.Should().Be(new MonthlySentiment(2025, 10, 70, 20, 10, 2));
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_AfterFallback_ReadsOnlyTheSubstituteModelsAnswer()
    {
        const string fallbackBlock = """
            {"type":"fallback","from":{"model":"claude-opus-5"},"to":{"model":"claude-opus-4-8"},
             "trigger":{"type":"refusal","category":"cyber"}}
            """;
        var content = string.Join(",", TextBlock("{\"positive\":"), fallbackBlock, TextBlock(SentimentJson));
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(content))));

        var result = await service.AnalyzeSentimentAsync("sk-test", Notes);

        result.Positive.Should().Be(70);
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenClaudeDeclines_ThrowsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(
            "", stopReason: "refusal",
            stopDetails: """{"type":"refusal","category":"cyber","explanation":"Declined"}"""))));

        var act = () => service.AnalyzeSentimentAsync("sk-test", Notes);

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("Claude declined*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenAnswerIsCutOff_ThrowsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(
            TextBlock("{\"positive\":"), stopReason: "max_tokens"))));

        var act = () => service.AnalyzeSentimentAsync("sk-test", Notes);

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("*cut off*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenKeyIsRejected_ThrowsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.Unauthorized, ErrorBody("authentication_error"))));

        var act = () => service.AnalyzeSentimentAsync("sk-bad", Notes);

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("*rejected the API key*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WhenRequestIsInvalid_IncludesClaudesReason()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.BadRequest, ErrorBody("invalid_request_error"))));

        var act = () => service.AnalyzeSentimentAsync("sk-test", Notes);

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("Claude rejected the request*");
    }

    [Fact]
    public async Task AnalyzeSentimentAsync_WithNoNotes_DoesNotCallClaude()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Claude should not be called"));
        var service = CreateService(handler);

        var result = await service.AnalyzeSentimentAsync("sk-test", Array.Empty<MeetingNoteForAnalysis>());

        result.Neutral.Should().Be(100);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenModelLookupSucceeds_IsValidWithoutGeneratingTokens()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {"type":"model","id":"claude-opus-5","display_name":"Claude Opus 5","created_at":"2026-01-01T00:00:00Z"}
            """));
        var service = CreateService(handler);

        var (isValid, error) = await service.ValidateApiKeyAsync("sk-test");

        isValid.Should().BeTrue();
        error.Should().BeNull();
        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.Uri.AbsolutePath.Should().Be("/v1/models/claude-opus-5");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenKeyIsRejected_ReturnsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.Unauthorized, ErrorBody("authentication_error"))));

        var (isValid, error) = await service.ValidateApiKeyAsync("sk-bad");

        isValid.Should().BeFalse();
        error.Should().Contain("rejected the API key");
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithBlankKey_DoesNotCallClaude()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Claude should not be called"));

        var (isValid, _) = await CreateService(handler).ValidateApiKeyAsync(" ");

        isValid.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    private const string RetroJson = """
        {"summary":"The team kept raising flaky tests.","warnings":["Flaky tests raised twice"],
         "actionItems":[{"text":"Quarantine flaky tests","retroDate":"2026-09-28","owner":"Alice"}],
         "suggestions":["Give the flaky test work an owner"]}
        """;

    private static readonly IReadOnlyList<RetroForAnalysis> Retros = new[]
    {
        new RetroForAnalysis("# 2026-09-28\n\n- Tests <flaky> again", new DateOnly(2026, 9, 28))
    };

    [Fact]
    public async Task SummarizeRetrosAsync_SendsTheRetroSchemaAndReadsTheAnswer()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Message(TextBlock(RetroJson))));
        var service = CreateService(handler);

        var result = await service.SummarizeRetrosAsync("sk-test", Retros);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var root = body.RootElement;
        root.GetProperty("fallbacks").GetString().Should().Be("default");
        root.GetProperty("output_config").GetProperty("format").GetProperty("schema").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()).Should().Equal("summary", "warnings", "actionItems", "suggestions");
        var content = root.GetProperty("messages")[0].GetProperty("content");
        var prompt = content.ValueKind == JsonValueKind.String
            ? content.GetString()!
            : string.Concat(content.EnumerateArray().Select(b => b.GetProperty("text").GetString()));
        prompt.Should().Contain("<retro date=\"2026-09-28\">");
        prompt.Should().Contain("Tests &lt;flaky&gt; again");

        result.Summary.Should().Be("The team kept raising flaky tests.");
        result.ActionItems.Should().ContainSingle().Which.Owner.Should().Be("Alice");
    }

    [Fact]
    public async Task SummarizeRetrosAsync_WhenClaudeDeclines_ThrowsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(
            "", stopReason: "refusal",
            stopDetails: """{"type":"refusal","category":"cyber","explanation":"Declined"}"""))));

        var act = () => service.SummarizeRetrosAsync("sk-test", Retros);

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("Claude declined*");
    }

    [Fact]
    public async Task SummarizeRetrosAsync_WithoutKey_DoesNotCallClaude()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Claude should not be called"));
        var service = CreateService(handler);

        var act = () => service.SummarizeRetrosAsync("", Retros);

        await act.Should().ThrowAsync<ClaudeApiException>();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ReviewTaskAsync_SendsTheTaskSchemaAndReadsTheAnswer()
    {
        const string review = """{"verdict":"Ready","summary":"Keeps drafts.","missing":[]}""";
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, Message(TextBlock(review))));

        var result = await CreateService(handler).ReviewTaskAsync("sk-test", new TaskForReview("Save drafts", "Story", "## Acceptance criteria"));

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        body.RootElement.GetProperty("output_config").GetProperty("format").GetProperty("schema").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()).Should().Equal("verdict", "summary", "missing");
        result.Verdict.Should().Be(Hive.Core.Entities.TaskReadinessVerdict.Ready);
        result.Missing.Should().BeEmpty();
    }

    [Fact]
    public async Task ReviewTaskAsync_WhenClaudeDeclines_ThrowsReadableError()
    {
        var service = CreateService(new StubHandler(_ => Json(HttpStatusCode.OK, Message(
            "", stopReason: "refusal",
            stopDetails: """{"type":"refusal","category":"cyber","explanation":"Declined"}"""))));

        var act = () => service.ReviewTaskAsync("sk-test", new TaskForReview("Save drafts", "Story", ""));

        (await act.Should().ThrowAsync<ClaudeApiException>()).WithMessage("Claude declined*");
    }

    private static ClaudeApiService CreateService(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<ClaudeApiService>.Instance);

    private static string TextBlock(string text) =>
        JsonSerializer.Serialize(new { type = "text", text });

    private static string Message(string content, string stopReason = "end_turn", string stopDetails = "null") =>
        "{\"id\":\"msg_test\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-opus-5\","
        + "\"content\":[" + content + "],\"stop_reason\":\"" + stopReason + "\",\"stop_sequence\":null,"
        + "\"stop_details\":" + stopDetails + ",\"usage\":{\"input_tokens\":100,\"output_tokens\":50}}";

    private static string ErrorBody(string type) =>
        "{\"type\":\"error\",\"error\":{\"type\":\"" + type + "\",\"message\":\"" + type + " from the stub\"}}";

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record CapturedRequest(HttpMethod Method, Uri Uri, Dictionary<string, string> Headers, string? Body);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => string.Join(",", h.Value));
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, headers, body));
            return _respond(request);
        }
    }
}
