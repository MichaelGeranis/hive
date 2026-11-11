using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hive.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Services;

/// <summary>
/// <see cref="IJiraApiClient"/> over the Jira Cloud REST API, authenticated with an
/// account email and API token (HTTP Basic).
/// </summary>
/// <remarks>
/// Atlassian issues two kinds of API token. A classic token works against the site URL
/// (<c>https://yourteam.atlassian.net</c>). A scoped token ("API token with scopes") is
/// rejected there with a 401 and only works through the API gateway,
/// <c>https://api.atlassian.com/ex/jira/{cloudId}</c>. Every call tries the site first and,
/// on a 401, looks up the site's cloud ID and retries through the gateway, so the manager
/// does not need to know which kind of token they have.
/// </remarks>
public class JiraApiClient : IJiraApiClient
{
    // Jira's own maximum page size for the Agile API
    private const int SprintPageSize = 50;

    // The enhanced JQL search returns up to 100 issues per page with a short field list
    private const int SearchPageSize = 100;

    // A result set larger than this is not something Hive should be reading in one go;
    // stop rather than loop forever
    private const int MaxPages = 100;

    // How many issue keys go into one "key in (...)" search, well under JQL's length limit
    private const int KeyBatchSize = 50;

    private const string GatewayUrl = "https://api.atlassian.com/ex/jira";

    // Only the fields JiraIssue carries, so Jira does not send the rest of each issue
    private static readonly string[] IssueFields = { "summary", "issuetype", "status", "parent", "components" };

    // The standard fields a work item needs; story points and sprint are added by id per site
    private static readonly string[] WorkItemFields =
    {
        "summary", "description", "issuetype", "status", "priority", "assignee", "parent",
        "project", "duedate", "labels", "components"
    };

    private const string SprintFieldType = "com.pyxis.greenhopper.jira:gh-sprint";

    // In the order they are preferred: company-managed spaces use the first, team-managed the second
    private static readonly string[] StoryPointFieldNames = { "Story Points", "Story point estimate" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraApiClient> _logger;

    public JiraApiClient(HttpClient httpClient, ILogger<JiraApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<JiraSprint>> GetBoardSprintsAsync(
        JiraConnection connection,
        int boardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var session = new Session(connection);
        var resource = $"board {boardId}";
        var sprints = new List<JiraSprint>();
        var startAt = 0;

        for (var page = 0; page < MaxPages; page++)
        {
            var response = await SendAsync<SprintPage>(
                session,
                HttpMethod.Get,
                $"/rest/agile/1.0/board/{boardId}/sprint?startAt={startAt}&maxResults={SprintPageSize}",
                body: null,
                resource,
                cancellationToken);

            sprints.AddRange(response.Values.Select(v => new JiraSprint(
                v.Id,
                v.Name ?? string.Empty,
                v.State ?? string.Empty,
                v.StartDate,
                v.EndDate)));

            if (response.IsLast || response.Values.Count == 0)
            {
                return sprints;
            }

            startAt += response.Values.Count;
        }

        _logger.LogWarning("Stopped reading sprints for Jira {Resource} after {Pages} pages", resource, MaxPages);
        return sprints;
    }

    public async Task<IReadOnlyList<JiraIssue>> GetParentIssuesAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // JQL cannot ask for "issues that have sub-tasks" directly, so the other parents are
        // found through their sub-tasks and then read in batches by key
        var session = new Session(connection);
        var resource = $"space {projectKey}";
        var space = $"project = \"{projectKey}\"";

        var parents = new List<JiraIssue>(
            await SearchAsync(session, $"{space} AND issuetype = Epic ORDER BY key ASC", resource, cancellationToken));
        var subTasks = await SearchAsync(session, $"{space} AND issuetype in subTaskIssueTypes() ORDER BY key ASC", resource, cancellationToken);

        var known = parents.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var otherParentKeys = subTasks
            .Select(s => s.ParentKey)
            .Where(key => !string.IsNullOrWhiteSpace(key) && !known.Contains(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        foreach (var batch in otherParentKeys.Chunk(KeyBatchSize))
        {
            parents.AddRange(await SearchAsync(
                session, $"key in ({string.Join(", ", batch)}) ORDER BY key ASC", resource, cancellationToken));
        }

        return parents;
    }

    public async Task<IReadOnlyList<JiraComponent>> GetComponentsAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // This endpoint returns every component in one response; it is not paged
        var session = new Session(connection);
        var components = await SendAsync<List<ComponentValue>>(
            session,
            HttpMethod.Get,
            $"/rest/api/3/project/{Uri.EscapeDataString(projectKey)}/components",
            body: null,
            $"space {projectKey}",
            cancellationToken);

        return components
            .Where(c => !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new JiraComponent(c.Id!, c.Name!, string.IsNullOrWhiteSpace(c.Description) ? null : c.Description))
            .ToList();
    }

    public async Task<IReadOnlyList<JiraWorkItem>> GetSprintWorkItemsAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var session = new Session(connection);
        var resource = $"space {projectKey}";
        var (storyPointsField, sprintField) = await FindCustomFieldsAsync(session, cancellationToken);

        var fields = WorkItemFields.Append(sprintField);
        if (storyPointsField is not null)
        {
            fields = fields.Append(storyPointsField);
        }

        return await SearchAsync(
            session,
            $"project = \"{projectKey}\" AND issuetype != Epic AND sprint is not EMPTY ORDER BY key ASC",
            fields.ToArray(),
            issue => ToWorkItem(issue, storyPointsField, sprintField),
            resource,
            cancellationToken);
    }

    /// <summary>
    /// Story points and sprints live in custom fields whose ids differ from site to site, so
    /// they are looked up by name and type.
    /// </summary>
    private async Task<(string? StoryPoints, string Sprint)> FindCustomFieldsAsync(Session session, CancellationToken cancellationToken)
    {
        var definitions = await SendAsync<List<FieldDefinition>>(
            session, HttpMethod.Get, "/rest/api/3/field", body: null, "the field list", cancellationToken);

        var sprint = definitions.FirstOrDefault(f => f.Schema?.Custom == SprintFieldType)?.Id
            ?? throw new JiraApiException("This Jira site has no Sprint field, so tasks cannot be read by sprint.");

        var storyPoints = StoryPointFieldNames
            .Select(name => definitions.FirstOrDefault(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) && f.Schema?.Type == "number"))
            .FirstOrDefault(f => f is not null)?.Id;

        return (storyPoints, sprint);
    }

    private static JiraWorkItem ToWorkItem(IssueValue issue, string? storyPointsField, string sprintField)
    {
        var fields = issue.Fields;
        var extra = fields?.Extra;

        double? storyPoints = null;
        if (storyPointsField is not null && extra is not null &&
            extra.TryGetValue(storyPointsField, out var points) && points.ValueKind == JsonValueKind.Number)
        {
            storyPoints = points.GetDouble();
        }

        var sprints = new List<JiraSprint>();
        if (extra is not null && extra.TryGetValue(sprintField, out var sprintValues) && sprintValues.ValueKind == JsonValueKind.Array)
        {
            sprints.AddRange(sprintValues.EnumerateArray()
                .Select(s => s.Deserialize<SprintValue>(JsonOptions))
                .Where(s => s is not null)
                .Select(s => new JiraSprint(s!.Id, s.Name ?? string.Empty, s.State ?? string.Empty, s.StartDate, s.EndDate))
                .OrderBy(s => s.Id));
        }

        return new JiraWorkItem(
            issue.Key,
            fields?.Summary ?? string.Empty,
            fields?.Description is { } description ? AtlassianDocumentText.ToMarkdown(description) : string.Empty,
            fields?.IssueType?.Name ?? string.Empty,
            fields?.Status?.Name,
            fields?.Priority?.Name,
            fields?.Assignee?.EmailAddress,
            fields?.Assignee?.DisplayName,
            fields?.Parent?.Key,
            fields?.Project?.Name,
            DateTime.TryParse(fields?.DueDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var due)
                ? DateTime.SpecifyKind(due.Date, DateTimeKind.Unspecified)
                : null,
            fields?.Labels ?? new List<string>(),
            ComponentNames(fields),
            storyPoints,
            sprints);
    }

    private static List<string> ComponentNames(IssueFieldValues? fields) =>
        fields?.Components?.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Cast<string>().ToList() ?? new List<string>();

    private static JiraIssue ToIssue(IssueValue issue) => new(
        issue.Key,
        issue.Fields?.Summary ?? string.Empty,
        issue.Fields?.IssueType?.Name ?? string.Empty,
        issue.Fields?.Status?.Name,
        issue.Fields?.Parent?.Key,
        ComponentNames(issue.Fields));

    private Task<IReadOnlyList<JiraIssue>> SearchAsync(Session session, string jql, string resource, CancellationToken cancellationToken) =>
        SearchAsync(session, jql, IssueFields, ToIssue, resource, cancellationToken);

    /// <summary>
    /// Runs a JQL search through the enhanced search endpoint, following its page tokens.
    /// </summary>
    private async Task<IReadOnlyList<T>> SearchAsync<T>(
        Session session,
        string jql,
        string[] fields,
        Func<IssueValue, T> map,
        string resource,
        CancellationToken cancellationToken)
    {
        var issues = new List<T>();
        string? pageToken = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var response = await SendAsync<SearchPage>(
                session,
                HttpMethod.Post,
                "/rest/api/3/search/jql",
                new SearchRequest(jql, fields, SearchPageSize, pageToken),
                resource,
                cancellationToken);

            issues.AddRange(response.Issues.Select(map));

            if (response.IsLast || string.IsNullOrEmpty(response.NextPageToken) || response.Issues.Count == 0)
            {
                return issues;
            }

            pageToken = response.NextPageToken;
        }

        _logger.LogWarning("Stopped reading issues for Jira {Resource} after {Pages} pages", resource, MaxPages);
        return issues;
    }

    /// <summary>
    /// Sends one request, retrying through the gateway if the site rejects the token.
    /// Once a session has moved to the gateway it stays there for its remaining pages.
    /// </summary>
    private async Task<T> SendAsync<T>(
        Session session,
        HttpMethod method,
        string path,
        object? body,
        string resource,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SendOnceAsync<T>(session, method, path, body, resource, cancellationToken);
        }
        catch (JiraUnauthorizedException) when (!session.OnGateway)
        {
            // Possibly a scoped token
            session.ApiBase = await GetGatewayBaseUrlAsync(session.Connection, cancellationToken);
            session.OnGateway = true;
            return await SendOnceAsync<T>(session, method, path, body, resource, cancellationToken);
        }
    }

    private async Task<T> SendOnceAsync<T>(
        Session session,
        HttpMethod method,
        string path,
        object? body,
        string resource,
        CancellationToken cancellationToken)
    {
        var connection = session.Connection;
        using var request = new HttpRequestMessage(method, session.ApiBase + path);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{connection.Email}:{connection.ApiToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Jira at {SiteUrl}", connection.SiteUrl);
            throw new JiraApiException($"Could not reach Jira at {connection.SiteUrl}. Check the Jira base URL and your connection.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timed out calling Jira at {SiteUrl}", connection.SiteUrl);
            throw new JiraApiException("Jira did not respond in time. Try again.", ex);
        }

        using (response)
        {
            // Some endpoints, JQL search among them, do not answer a rejected login with a 401:
            // they run the request as an anonymous user and return 200 with nothing visible,
            // flagging the failure only in this header. Treat that as the 401 it is.
            if (response.IsSuccessStatusCode && LoginFailed(response))
            {
                _logger.LogWarning("Jira ran the request anonymously because the login failed");
                throw new JiraUnauthorizedException();
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return result ?? throw new JiraApiException("Jira returned an empty response.");
            }

            var detail = await ReadErrorDetailAsync(response, cancellationToken);
            _logger.LogWarning("Jira request failed: {StatusCode} {Detail}", response.StatusCode, detail);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // The gateway accepts a scoped token but refuses calls outside its scopes
                if (session.OnGateway && detail?.Contains("scope", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new JiraApiException(
                        $"The Jira API token is not allowed to read {resource}. Add read access for Jira work items and boards to the token's scopes, or use a classic token.");
                }

                throw new JiraUnauthorizedException();
            }

            throw new JiraApiException(response.StatusCode switch
            {
                HttpStatusCode.Forbidden =>
                    $"The Jira account does not have permission to view {resource}.",
                HttpStatusCode.NotFound =>
                    $"Jira {resource} was not found, or the account cannot see it.",
                HttpStatusCode.TooManyRequests =>
                    "Jira is rate limiting requests. Wait a minute and try again.",
                HttpStatusCode.BadRequest when detail is not null =>
                    $"Jira rejected the request: {detail}",
                _ => $"Jira returned an error ({(int)response.StatusCode})."
            });
        }
    }

    private static bool LoginFailed(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Seraph-LoginReason", out var reasons) &&
        reasons.Any(r => r.Contains("AUTHENTICATED_FAILED", StringComparison.OrdinalIgnoreCase) ||
                         r.Contains("AUTHENTICATION_DENIED", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The gateway address for this site. The cloud ID comes from the site's public
    /// <c>/_edge/tenant_info</c> endpoint, which needs no credentials.
    /// </summary>
    private async Task<string> GetGatewayBaseUrlAsync(JiraConnection connection, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Jira rejected the credentials at {SiteUrl}; retrying through the Atlassian API gateway in case the token is scoped",
            connection.SiteUrl);

        try
        {
            var tenant = await _httpClient.GetFromJsonAsync<TenantInfo>(
                $"{connection.SiteUrl.TrimEnd('/')}/_edge/tenant_info", JsonOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(tenant?.CloudId))
            {
                return $"{GatewayUrl}/{tenant.CloudId}";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Could not look up the Jira cloud ID for {SiteUrl}", connection.SiteUrl);
        }

        // Without a cloud ID there is nowhere else to try: the credentials were simply rejected
        throw new JiraUnauthorizedException();
    }

    /// <summary>
    /// Jira errors carry human-readable text in <c>errorMessages</c>, e.g. a Kanban board
    /// "does not support sprints"; the gateway uses a single <c>message</c> instead.
    /// </summary>
    private static async Task<string?> ReadErrorDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions, cancellationToken);
            var messages = error?.ErrorMessages?.Where(m => !string.IsNullOrWhiteSpace(m)).ToList() ?? new List<string>();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                messages.Add(error.Message);
            }

            return messages.Count > 0 ? string.Join(" ", messages) : null;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Where one logical call (all of its pages) is currently being sent.
    /// </summary>
    private sealed class Session
    {
        public Session(JiraConnection connection)
        {
            Connection = connection;
            ApiBase = connection.SiteUrl.TrimEnd('/');
        }

        public JiraConnection Connection { get; }
        public string ApiBase { get; set; }
        public bool OnGateway { get; set; }
    }

    private sealed record SearchRequest(
        [property: JsonPropertyName("jql")] string Jql,
        [property: JsonPropertyName("fields")] string[] Fields,
        [property: JsonPropertyName("maxResults")] int MaxResults,
        [property: JsonPropertyName("nextPageToken"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextPageToken);

    private sealed class SearchPage
    {
        public List<IssueValue> Issues { get; set; } = new();
        public string? NextPageToken { get; set; }
        public bool IsLast { get; set; }
    }

    private sealed class IssueValue
    {
        public string Key { get; set; } = string.Empty;
        public IssueFieldValues? Fields { get; set; }
    }

    private sealed class IssueFieldValues
    {
        public string? Summary { get; set; }
        public JsonElement? Description { get; set; }
        public NamedValue? IssueType { get; set; }
        public NamedValue? Status { get; set; }
        public NamedValue? Priority { get; set; }
        public UserValue? Assignee { get; set; }
        public NamedValue? Project { get; set; }
        public string? DueDate { get; set; }
        public List<string>? Labels { get; set; }
        public List<NamedValue>? Components { get; set; }
        public ParentReference? Parent { get; set; }

        // Custom fields, such as story points and sprint, whose ids vary by site
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    private sealed class UserValue
    {
        public string? EmailAddress { get; set; }
        public string? DisplayName { get; set; }
    }

    private sealed class FieldDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public FieldSchema? Schema { get; set; }
    }

    private sealed class FieldSchema
    {
        public string? Type { get; set; }
        public string? Custom { get; set; }
    }

    private sealed class ParentReference
    {
        public string? Key { get; set; }
    }

    private sealed class ComponentValue
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private sealed class NamedValue
    {
        public string? Name { get; set; }
    }

    private sealed class SprintPage
    {
        [JsonPropertyName("isLast")]
        public bool IsLast { get; set; }

        [JsonPropertyName("values")]
        public List<SprintValue> Values { get; set; } = new();
    }

    private sealed class SprintValue
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? State { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    private sealed class ErrorBody
    {
        public List<string>? ErrorMessages { get; set; }
        public string? Message { get; set; }
    }

    private sealed class TenantInfo
    {
        public string? CloudId { get; set; }
    }

    /// <summary>
    /// A 401, kept distinct so the client can retry through the gateway. Callers only ever
    /// see it as a <see cref="JiraApiException"/>.
    /// </summary>
    private sealed class JiraUnauthorizedException : JiraApiException
    {
        public JiraUnauthorizedException()
            : base("Jira rejected the credentials. Check the Jira email and API token in Settings.")
        {
        }
    }
}
