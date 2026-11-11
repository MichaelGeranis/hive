using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Infrastructure.Services;

/// <summary>
/// Tests for JiraApiClient, against a stubbed HTTP handler. No request leaves the process.
/// </summary>
public class JiraApiClientTests
{
    private static readonly JiraConnection Connection = new("https://acme.atlassian.net", "em@acme.com", "token-123");

    [Fact]
    public async Task GetBoardSprintsAsync_SendsBasicAuthToAgileSprintEndpoint()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{ "isLast": true, "values": [] }"""));
        var client = CreateClient(handler);

        await client.GetBoardSprintsAsync(Connection, 42);

        var request = handler.Requests.Single();
        request.RequestUri!.ToString().Should().Be(
            "https://acme.atlassian.net/rest/agile/1.0/board/42/sprint?startAt=0&maxResults=50");
        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!))
            .Should().Be("em@acme.com:token-123");
    }

    [Fact]
    public async Task GetBoardSprintsAsync_ParsesSprintsAndDates()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {
              "isLast": true,
              "values": [
                { "id": 7, "name": "LP_4Q25_S1", "state": "closed",
                  "startDate": "2025-10-06T08:00:00.000Z", "endDate": "2025-10-17T15:00:00.000Z" },
                { "id": 8, "name": "LP_4Q25_S2", "state": "future" }
              ]
            }
            """));
        var client = CreateClient(handler);

        var sprints = await client.GetBoardSprintsAsync(Connection, 42);

        sprints.Should().HaveCount(2);
        sprints[0].Should().Be(new JiraSprint(
            7, "LP_4Q25_S1", "closed",
            new DateTimeOffset(2025, 10, 6, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 10, 17, 15, 0, 0, TimeSpan.Zero)));
        sprints[1].StartDate.Should().BeNull();
        sprints[1].EndDate.Should().BeNull();
    }

    [Fact]
    public async Task GetBoardSprintsAsync_FollowsPagesUntilLast()
    {
        var handler = new StubHandler(request => request.RequestUri!.Query.Contains("startAt=0")
            ? Json(HttpStatusCode.OK, """{ "isLast": false, "values": [ { "id": 1, "name": "A" }, { "id": 2, "name": "B" } ] }""")
            : Json(HttpStatusCode.OK, """{ "isLast": true, "values": [ { "id": 3, "name": "C" } ] }"""));
        var client = CreateClient(handler);

        var sprints = await client.GetBoardSprintsAsync(Connection, 42);

        sprints.Select(s => s.Name).Should().Equal("A", "B", "C");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[1].RequestUri!.Query.Should().Contain("startAt=2");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "*credentials*")]
    [InlineData(HttpStatusCode.Forbidden, "*permission*board 42*")]
    [InlineData(HttpStatusCode.NotFound, "*board 42 was not found*")]
    [InlineData(HttpStatusCode.TooManyRequests, "*rate limiting*")]
    [InlineData(HttpStatusCode.InternalServerError, "*(500)*")]
    public async Task GetBoardSprintsAsync_OnErrorStatus_ThrowsReadableJiraApiException(HttpStatusCode status, string message)
    {
        var client = CreateClient(new StubHandler(_ => Json(status, "{}")));

        var act = () => client.GetBoardSprintsAsync(Connection, 42);

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage(message);
    }

    [Fact]
    public async Task GetBoardSprintsAsync_OnBadRequest_IncludesJiraErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => Json(
            HttpStatusCode.BadRequest,
            """{ "errorMessages": ["The board does not support sprints"], "errors": {} }""")));

        var act = () => client.GetBoardSprintsAsync(Connection, 42);

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("*does not support sprints*");
    }

    [Fact]
    public async Task GetBoardSprintsAsync_WithScopedToken_RetriesThroughTheApiGateway()
    {
        var handler = new StubHandler(request => request.RequestUri!.ToString() switch
        {
            var u when u.StartsWith("https://acme.atlassian.net/rest/") => Json(HttpStatusCode.Unauthorized, "{}"),
            "https://acme.atlassian.net/_edge/tenant_info" => Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }"""),
            var u when u.StartsWith("https://api.atlassian.com/ex/jira/cloud-123/rest/agile/1.0/board/42/sprint") =>
                Json(HttpStatusCode.OK, """{ "isLast": true, "values": [ { "id": 1, "name": "LP_4Q25_S1" } ] }"""),
            var u => throw new InvalidOperationException($"Unexpected request {u}")
        });
        var client = CreateClient(handler);

        var sprints = await client.GetBoardSprintsAsync(Connection, 42);

        sprints.Select(s => s.Name).Should().Equal("LP_4Q25_S1");
        handler.Requests.Select(r => r.RequestUri!.GetLeftPart(UriPartial.Path)).Should().Equal(
            "https://acme.atlassian.net/rest/agile/1.0/board/42/sprint",
            "https://acme.atlassian.net/_edge/tenant_info",
            "https://api.atlassian.com/ex/jira/cloud-123/rest/agile/1.0/board/42/sprint");
        handler.Requests[1].Headers.Authorization.Should().BeNull("the tenant lookup is public");
        handler.Requests[2].Headers.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact]
    public async Task GetBoardSprintsAsync_AfterSwitchingToGateway_ReadsLaterPagesThere()
    {
        var handler = new StubHandler(request => request.RequestUri!.ToString() switch
        {
            var u when u.StartsWith("https://acme.atlassian.net/rest/") => Json(HttpStatusCode.Unauthorized, "{}"),
            "https://acme.atlassian.net/_edge/tenant_info" => Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }"""),
            var u when u.Contains("startAt=0") => Json(HttpStatusCode.OK, """{ "isLast": false, "values": [ { "id": 1, "name": "A" } ] }"""),
            _ => Json(HttpStatusCode.OK, """{ "isLast": true, "values": [ { "id": 2, "name": "B" } ] }""")
        });
        var client = CreateClient(handler);

        var sprints = await client.GetBoardSprintsAsync(Connection, 42);

        sprints.Select(s => s.Name).Should().Equal("A", "B");
        handler.Requests.Should().HaveCount(4);
        handler.Requests[3].RequestUri!.ToString().Should().StartWith("https://api.atlassian.com/ex/jira/cloud-123/");
    }

    [Fact]
    public async Task GetBoardSprintsAsync_WhenGatewayAlsoRejectsCredentials_ThrowsCredentialsError()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath == "/_edge/tenant_info"
            ? Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }""")
            : Json(HttpStatusCode.Unauthorized, "{}"));
        var client = CreateClient(handler);

        var act = () => client.GetBoardSprintsAsync(Connection, 42);

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("*credentials*");
        handler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetParentIssuesAsync_SearchesEpicsThenSubTasksWithOnlyTheFieldsItReads()
    {
        var handler = new StubHandler((_, _) => Json(HttpStatusCode.OK, """{ "issues": [], "isLast": true }"""));

        await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        handler.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Post
            && r.RequestUri!.ToString() == "https://acme.atlassian.net/rest/api/3/search/jql");
        Jql(handler.Bodies[0]).Should().Be("project = \"MAR\" AND issuetype = Epic ORDER BY key ASC");
        Jql(handler.Bodies[1]).Should().Be("project = \"MAR\" AND issuetype in subTaskIssueTypes() ORDER BY key ASC");
        handler.Requests.Should().HaveCount(2, "no sub-task has a parent to look up");
        using var body = JsonDocument.Parse(handler.Bodies[0]!);
        body.RootElement.GetProperty("fields").EnumerateArray().Select(f => f.GetString())
            .Should().BeEquivalentTo("summary", "issuetype", "status", "parent", "components");
        body.RootElement.TryGetProperty("nextPageToken", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetComponentsAsync_ReadsTheSpacesComponents()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            [
              { "id": "10042", "name": "Matching", "description": "Matches works" },
              { "id": "10043", "name": "IPI" },
              { "id": "10044", "name": "  " }
            ]
            """));

        var components = await CreateClient(handler).GetComponentsAsync(Connection, "MAR");

        handler.Requests.Single().Method.Should().Be(HttpMethod.Get);
        handler.Requests.Single().RequestUri!.ToString().Should().Be("https://acme.atlassian.net/rest/api/3/project/MAR/components");
        components.Should().Equal(
            new JiraComponent("10042", "Matching", "Matches works"),
            new JiraComponent("10043", "IPI", null));
    }

    [Fact]
    public async Task GetComponentsAsync_UnknownSpace_ThrowsReadableJiraApiException()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.NotFound, """{ "errorMessages": ["No project could be found with key 'NOPE'."] }"""));

        var act = () => CreateClient(handler).GetComponentsAsync(Connection, "NOPE");

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("Jira space NOPE was not found*");
    }

    [Fact]
    public async Task GetParentIssuesAsync_ReturnsEpicsAndTheParentsOfSubTasks()
    {
        var handler = new StubHandler((_, body) => Jql(body) switch
        {
            var q when q.Contains("issuetype = Epic") => Issues(Issue("MAR-1", "Payments", "Epic", status: "Done", components: new[] { "Matching", "IPI" })),
            var q when q.Contains("subTaskIssueTypes()") => Issues(
                Issue("MAR-5", "Backfill", "Sub-task", parent: "MAR-2"),
                Issue("MAR-6", "Deploy", "Sub-task", parent: "MAR-2"),
                Issue("MAR-7", "Tidy up", "Sub-task", parent: "MAR-1")),
            var q when q.StartsWith("key in") => Issues(Issue("MAR-2", "Created_at field", "Story", status: "In Progress")),
            var q => throw new InvalidOperationException($"Unexpected JQL {q}")
        });

        var parents = await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        parents.Should().BeEquivalentTo(
            new[]
            {
                new JiraIssue("MAR-1", "Payments", "Epic", "Done", Components: new[] { "Matching", "IPI" }),
                new JiraIssue("MAR-2", "Created_at field", "Story", "In Progress", Components: Array.Empty<string>())
            },
            options => options.WithStrictOrdering());
        Jql(handler.Bodies[2]).Should().Be("key in (MAR-2) ORDER BY key ASC", "the epic MAR-1 is already known");
    }

    [Fact]
    public async Task GetParentIssuesAsync_LooksUpManyParentsInBatches()
    {
        var subTasks = Enumerable.Range(1, 120).Select(i => Issue($"MAR-{1000 + i}", "Sub-task", "Sub-task", parent: $"MAR-{i}")).ToArray();
        var handler = new StubHandler((_, body) => Jql(body) switch
        {
            var q when q.Contains("issuetype = Epic") => Issues(),
            var q when q.Contains("subTaskIssueTypes()") => Issues(subTasks),
            _ => Issues()
        });

        await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        var lookups = handler.Bodies.Skip(2).Select(Jql).ToList();
        lookups.Should().HaveCount(3);
        lookups.Select(q => q.Split(',').Length).Should().Equal(50, 50, 20);
    }

    [Fact]
    public async Task GetParentIssuesAsync_FollowsPageTokens()
    {
        var epicPages = 0;
        var handler = new StubHandler((_, body) => Jql(body).Contains("issuetype = Epic")
            ? ++epicPages == 1
                ? Json(HttpStatusCode.OK, """{ "isLast": false, "nextPageToken": "page-2", "issues": [ { "key": "MAR-1", "fields": { "summary": "A", "issuetype": { "name": "Epic" } } } ] }""")
                : Issues(Issue("MAR-2", "B", "Epic"))
            : Issues());

        var parents = await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        parents.Select(e => e.Key).Should().Equal("MAR-1", "MAR-2");
        using var secondBody = JsonDocument.Parse(handler.Bodies[1]!);
        secondBody.RootElement.GetProperty("nextPageToken").GetString().Should().Be("page-2");
    }

    [Fact]
    public async Task GetParentIssuesAsync_WithScopedToken_MovesToTheGatewayOnceForEverySearch()
    {
        var handler = new StubHandler((request, _) => request.RequestUri!.ToString() switch
        {
            "https://acme.atlassian.net/rest/api/3/search/jql" => Json(HttpStatusCode.Unauthorized, "{}"),
            "https://acme.atlassian.net/_edge/tenant_info" => Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }"""),
            "https://api.atlassian.com/ex/jira/cloud-123/rest/api/3/search/jql" => Issues(Issue("MAR-1", "A", "Epic")),
            var u => throw new InvalidOperationException($"Unexpected request {u}")
        });

        var parents = await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        parents.Should().ContainSingle();
        handler.Requests.Select(r => r.RequestUri!.Host).Should().Equal(
            "acme.atlassian.net", "acme.atlassian.net", "api.atlassian.com", "api.atlassian.com");
        Jql(handler.Bodies[2]).Should().Contain("issuetype = Epic", "the retried request carries the same search");
    }

    [Fact]
    public async Task GetParentIssuesAsync_WhenSiteRunsTheSearchAnonymously_RetriesThroughTheGateway()
    {
        // Jira answers a failed login on search with 200 and no results, flagged only in a header
        var handler = new StubHandler((request, body) =>
        {
            if (request.RequestUri!.Host == "acme.atlassian.net" && request.RequestUri.AbsolutePath == "/rest/api/3/search/jql")
            {
                var anonymous = Issues();
                anonymous.Headers.Add("X-Seraph-LoginReason", "AUTHENTICATED_FAILED");
                return anonymous;
            }

            if (request.RequestUri.AbsolutePath == "/_edge/tenant_info")
            {
                return Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }""");
            }

            return Jql(body).Contains("issuetype = Epic") ? Issues(Issue("MAR-1", "A", "Epic")) : Issues();
        });

        var parents = await CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        parents.Select(e => e.Key).Should().Equal("MAR-1");
        handler.Requests.Last().RequestUri!.Host.Should().Be("api.atlassian.com");
    }

    [Fact]
    public async Task GetParentIssuesAsync_WhenScopedTokenLacksTheScope_SaysSo()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath == "/_edge/tenant_info"
            ? Json(HttpStatusCode.OK, """{ "cloudId": "cloud-123" }""")
            : request.RequestUri.Host == "api.atlassian.com"
                ? Json(HttpStatusCode.Unauthorized, """{ "code": 401, "message": "Unauthorized; scope does not match" }""")
                : Json(HttpStatusCode.Unauthorized, "{}"));

        var act = () => CreateClient(handler).GetParentIssuesAsync(Connection, "MAR");

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("*not allowed to read space MAR*scopes*");
    }

    private const string FieldList = """
        [
          { "id": "customfield_10016", "name": "Story point estimate", "schema": { "type": "number", "custom": "com.pyxis.greenhopper.jira:jsw-story-points" } },
          { "id": "customfield_10028", "name": "Story Points", "schema": { "type": "number", "custom": "com.atlassian.jira.plugin.system.customfieldtypes:float" } },
          { "id": "customfield_16073", "name": "Sprint", "schema": { "type": "string", "custom": "com.atlassian.jira.plugin.system.customfieldtypes:textfield" } },
          { "id": "customfield_10020", "name": "Sprint", "schema": { "type": "array", "custom": "com.pyxis.greenhopper.jira:gh-sprint" } }
        ]
        """;

    [Fact]
    public async Task GetSprintWorkItemsAsync_FindsTheSitesFieldsAndSearchesSprintWork()
    {
        var handler = new StubHandler((request, _) => request.RequestUri!.AbsolutePath == "/rest/api/3/field"
            ? Json(HttpStatusCode.OK, FieldList)
            : Issues());

        await CreateClient(handler).GetSprintWorkItemsAsync(Connection, "MAR");

        handler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/rest/api/3/field");
        Jql(handler.Bodies[1]).Should().Be("project = \"MAR\" AND issuetype != Epic AND sprint is not EMPTY ORDER BY key ASC");
        using var body = JsonDocument.Parse(handler.Bodies[1]!);
        body.RootElement.GetProperty("fields").EnumerateArray().Select(f => f.GetString())
            .Should().Contain(new[] { "customfield_10028", "customfield_10020", "description", "assignee" })
            .And.NotContain(new[] { "customfield_10016", "customfield_16073" }, "the classic Story Points and the real sprint field are preferred");
    }

    [Fact]
    public async Task GetSprintWorkItemsAsync_ReadsEveryFieldATaskNeeds()
    {
        var handler = new StubHandler((request, _) => request.RequestUri!.AbsolutePath == "/rest/api/3/field"
            ? Json(HttpStatusCode.OK, FieldList)
            : Json(HttpStatusCode.OK, """
                {
                  "isLast": true,
                  "issues": [ {
                    "key": "MAR-10",
                    "fields": {
                      "summary": "Backfill created_at",
                      "description": { "type": "doc", "version": 1, "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Plain words" } ] } ] },
                      "issuetype": { "name": "Sub-task" },
                      "status": { "name": "In Review" },
                      "priority": { "name": "High" },
                      "assignee": { "emailAddress": "alice@acme.com", "displayName": "Alice Smith" },
                      "parent": { "key": "MAR-8" },
                      "project": { "name": "Marketplace" },
                      "duedate": "2026-10-01",
                      "labels": [ "payments" ],
                      "components": [ { "name": "API" } ],
                      "customfield_10028": 2.5,
                      "customfield_10020": [
                        { "id": 12, "name": "MAR_1Q26_S2", "state": "closed", "startDate": "2026-01-19T09:00:00.000Z", "endDate": "2026-01-30T17:00:00.000Z" },
                        { "id": 11, "name": "MAR_1Q26_S1", "state": "closed" }
                      ]
                    }
                  } ]
                }
                """));

        var item = (await CreateClient(handler).GetSprintWorkItemsAsync(Connection, "MAR")).Single();

        item.Key.Should().Be("MAR-10");
        item.Description.Should().Be("Plain words");
        item.IssueType.Should().Be("Sub-task");
        item.Status.Should().Be("In Review");
        item.Priority.Should().Be("High");
        item.AssigneeEmail.Should().Be("alice@acme.com");
        item.AssigneeName.Should().Be("Alice Smith");
        item.ParentKey.Should().Be("MAR-8");
        item.ProjectName.Should().Be("Marketplace");
        item.DueDate.Should().Be(new DateTime(2026, 10, 1));
        item.Labels.Should().Equal("payments");
        item.Components.Should().Equal("API");
        item.StoryPoints.Should().Be(2.5);
        item.Sprints.Select(sp => sp.Name).Should().Equal("MAR_1Q26_S1", "MAR_1Q26_S2");
        item.Sprints[1].StartDate.Should().Be(new DateTimeOffset(2026, 1, 19, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetSprintWorkItemsAsync_WithoutASprintField_SaysSo()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, "[]"));

        var act = () => CreateClient(handler).GetSprintWorkItemsAsync(Connection, "MAR");

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("*no Sprint field*");
    }

    [Fact]
    public async Task GetBoardSprintsAsync_WhenJiraUnreachable_ThrowsJiraApiException()
    {
        var client = CreateClient(new StubHandler(_ => throw new HttpRequestException("No such host")));

        var act = () => client.GetBoardSprintsAsync(Connection, 42);

        (await act.Should().ThrowAsync<JiraApiException>()).WithMessage("*Could not reach Jira*");
    }

    private static JiraApiClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<JiraApiClient>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string Jql(string? body) =>
        body is null ? string.Empty : JsonDocument.Parse(body).RootElement.GetProperty("jql").GetString()!;

    private static object Issue(string key, string summary, string type, string? status = null, string? parent = null, string[]? components = null) => new
    {
        key,
        fields = new
        {
            summary,
            issuetype = new { name = type },
            status = status is null ? null : new { name = status },
            parent = parent is null ? null : new { key = parent },
            components = components?.Select(name => new { name })
        }
    };

    private static HttpResponseMessage Issues(params object[] issues) =>
        Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { issues, isLast = true }));

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = (request, _) => respond(request);

        // For answering by what was asked, e.g. the JQL in a search body
        public StubHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> respond) => _respond = respond;

        public List<HttpRequestMessage> Requests { get; } = new();

        // Request bodies are disposed once sent, so keep their text
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Bodies.Add(body);
            return _respond(request, body);
        }
    }
}
