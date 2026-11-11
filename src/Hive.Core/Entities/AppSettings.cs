using System.Text.Json;

namespace Hive.Core.Entities;

/// <summary>
/// Represents application settings stored in the database.
/// </summary>
public class AppSettings
{
    public Guid Id { get; private set; }
    public string TshirtSizeMappings { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public const string DefaultOllamaBaseUrl = "http://localhost:11434";
    public const string DefaultOllamaModel = "llama3.1:8b";

    // Sentiment Analysis settings
    public SentimentProvider SentimentProvider { get; private set; } = SentimentProvider.Ollama;
    public string? ClaudeApiKey { get; private set; }
    public string OllamaBaseUrl { get; private set; } = DefaultOllamaBaseUrl;
    public string OllamaModel { get; private set; } = DefaultOllamaModel;
    public int SentimentAnalysisDays { get; private set; } = 90;
    public bool SentimentAnalysisEnabled { get; private set; }

    // Retro summary: read by the same model as sentiment, switched on separately
    public bool RetroSummaryEnabled { get; private set; }
    public int RetroSummaryCount { get; private set; } = DefaultRetroSummaryCount;

    // Task readiness: an AI review of upcoming tasks, with the same model, switched on separately
    public bool TaskReviewEnabled { get; private set; }

    // Sprint Import settings
    // If set, only sprints matching this team name will be imported
    // If null, all sprints matching the pattern will be imported
    public string? SprintTeamFilter { get; private set; }

    // Dashboard insight thresholds
    public int MaxInProgressTasks { get; private set; } = 2;
    public int MaxBlockedTasks { get; private set; } = 1;
    public int MaxInReviewTasks { get; private set; } = 1;
    public int MinProductMembers { get; private set; } = 2;

    // Fewest products a direct report should have worked on before they are narrowly engaged
    public int MinMemberProducts { get; private set; } = DefaultMinMemberProducts;

    // How often the manager aims to hold a 1:1 with each direct report, in days
    public int OneOnOneCadenceDays { get; private set; } = OneOnOneCadence.DefaultDays;

    // How many days an in-flight task may keep one status before it is reported as stale
    public int StaleTaskDays { get; private set; } = TeamTask.DefaultStaleAfterDays;

    // Maintenance label configuration: support is recognised by its task type, not a label
    // JSON array of strings, e.g., ["maintenance", "upgrade"]
    public string MaintenanceLabels { get; private set; } = "[\"maintenance\"]";

    // The epic names that name each work category, as JSON: {"TechDebt":["Tech Debt"],…}.
    // Empty means the defaults (WorkCategories.DefaultEpicNames).
    public string WorkCategoryEpicNames { get; private set; } = string.Empty;
    public const int MaxEpicNameLength = 100;
    public const int MaxEpicNamesPerCategory = 20;

    // Jira integration
    public string? JiraBaseUrl { get; private set; }

    // Jira Cloud API access, used to sync sprint data on demand. The token is write-only
    // from the API's point of view: it is stored here and never returned.
    public string? JiraEmail { get; private set; }
    public string? JiraApiToken { get; private set; }
    public int? JiraBoardId { get; private set; }

    // The Jira space (project) whose epics become parents, e.g. MAR
    public string? JiraProjectKey { get; private set; }

    // Google Calendar's "secret address in iCal format". The address is itself the credential,
    // so like the Jira token it is write-only from the API's point of view.
    public string? GoogleCalendarUrl { get; private set; }

    public AppSettings()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateTshirtSizeMappings(string tshirtSizeMappings)
    {
        TshirtSizeMappings = tshirtSizeMappings ?? "[]";
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateClaudeApiKey(string? apiKey)
    {
        ClaudeApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSentimentSettings(int days, bool enabled)
    {
        SentimentAnalysisDays = days > 0 ? days : 90;
        SentimentAnalysisEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasClaudeApiKey => !string.IsNullOrWhiteSpace(ClaudeApiKey);

    public void UpdateSentimentProvider(string provider)
    {
        if (!Enum.TryParse<SentimentProvider>(provider?.Trim(), ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException("Sentiment provider must be Ollama or Claude.", nameof(provider));
        }

        SentimentProvider = parsed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Where Ollama answers and which model it runs. A blank value goes back to the default.
    /// </summary>
    public void UpdateOllamaConnection(string? baseUrl, string? model)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? DefaultOllamaBaseUrl : baseUrl.Trim().TrimEnd('/');
        if (url.Length > 500 ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Ollama address must be an http:// or https:// URL, e.g. http://localhost:11434.", nameof(baseUrl));
        }

        var name = string.IsNullOrWhiteSpace(model) ? DefaultOllamaModel : model.Trim();
        if (name.Length > 200 || name.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Ollama model must be a model name such as llama3.1:8b, without spaces.", nameof(model));
        }

        OllamaBaseUrl = url;
        OllamaModel = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Whether the chosen provider has what it needs: Claude needs a key, while Ollama always
    /// has an address and a model, and whether it answers is only known by asking it.
    /// </summary>
    public bool IsSentimentProviderConfigured =>
        SentimentProvider == SentimentProvider.Ollama || HasClaudeApiKey;

    public void UpdateSprintTeamFilter(string? teamFilter)
    {
        SprintTeamFilter = string.IsNullOrWhiteSpace(teamFilter) ? null : teamFilter.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDashboardThresholds(int maxInProgress, int maxBlocked, int maxInReview, int minProductMembers)
    {
        MaxInProgressTasks = maxInProgress > 0 ? maxInProgress : 2;
        MaxBlockedTasks = maxBlocked > 0 ? maxBlocked : 1;
        MaxInReviewTasks = maxInReview > 0 ? maxInReview : 1;
        MinProductMembers = minProductMembers > 0 ? minProductMembers : 2;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Switches the AI review of upcoming tasks on or off.
    /// </summary>
    public void UpdateTaskReviewEnabled(bool enabled)
    {
        TaskReviewEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int DefaultRetroSummaryCount = 6;
    public const int MinRetroSummaryCount = 1;
    public const int MaxRetroSummaryCount = 20;

    /// <summary>
    /// Switches the retro summary on or off, and sets how many of the latest retros it reads.
    /// </summary>
    public void UpdateRetroSummarySettings(bool enabled, int count)
    {
        if (count < MinRetroSummaryCount || count > MaxRetroSummaryCount)
        {
            throw new ArgumentException(
                $"The retro summary reads between {MinRetroSummaryCount} and {MaxRetroSummaryCount} retros.", nameof(count));
        }

        RetroSummaryEnabled = enabled;
        RetroSummaryCount = count;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int DefaultMinMemberProducts = 2;
    public const int MinMinMemberProducts = 1;
    public const int MaxMinMemberProducts = 50;

    /// <summary>
    /// Sets how many products a direct report should have worked on. At 1, nobody who has worked
    /// on a product is flagged; someone with none is an unengaged member either way.
    /// </summary>
    public void UpdateMinMemberProducts(int minMemberProducts)
    {
        if (minMemberProducts < MinMinMemberProducts || minMemberProducts > MaxMinMemberProducts)
        {
            throw new ArgumentException(
                $"The minimum products per person must be between {MinMinMemberProducts} and {MaxMinMemberProducts}.",
                nameof(minMemberProducts));
        }

        MinMemberProducts = minMemberProducts;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateOneOnOneCadence(int cadenceDays)
    {
        OneOnOneCadence.Validate(cadenceDays);
        OneOnOneCadenceDays = cadenceDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStaleTaskDays(int staleTaskDays)
    {
        TeamTask.ValidateStaleAfterDays(staleTaskDays);
        StaleTaskDays = staleTaskDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMaintenanceLabels(string maintenanceLabels)
    {
        MaintenanceLabels = maintenanceLabels ?? "[\"maintenance\"]";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The epic names that name each work category: the manager's, or the defaults.
    /// </summary>
    public IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>> GetWorkCategoryEpicNames()
    {
        if (string.IsNullOrWhiteSpace(WorkCategoryEpicNames))
            return WorkCategories.DefaultEpicNames;

        try
        {
            var stored = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(WorkCategoryEpicNames) ?? [];
            return WorkCategories.DefaultEpicNames.Keys.ToDictionary(
                category => category,
                category => (IReadOnlyList<string>)(stored.TryGetValue(category.ToString(), out var names) ? names : []));
        }
        catch (JsonException)
        {
            return WorkCategories.DefaultEpicNames;
        }
    }

    /// <summary>
    /// Sets the epic names that name each work category. Every category but Productisation,
    /// which is every other epic, takes a list; a category left out has no names. A name is
    /// trimmed, cannot be empty or longer than <see cref="MaxEpicNameLength"/>, needs a letter
    /// or digit, and cannot name two categories (compared as <see cref="WorkCategories.Normalize"/>).
    /// </summary>
    public void UpdateWorkCategoryEpicNames(IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>> epicNames)
    {
        ArgumentNullException.ThrowIfNull(epicNames);
        if (epicNames.ContainsKey(WorkCategory.Productisation))
        {
            throw new ArgumentException("Productisation is every epic the other categories do not name; it takes no names.", nameof(epicNames));
        }

        var cleaned = new Dictionary<string, List<string>>();
        var seen = new Dictionary<string, WorkCategory>();
        foreach (var category in WorkCategories.DefaultEpicNames.Keys)
        {
            var names = (epicNames.TryGetValue(category, out var given) ? given : [])
                .Select(name => name?.Trim() ?? string.Empty)
                .ToList();
            if (names.Count > MaxEpicNamesPerCategory)
            {
                throw new ArgumentException($"A work category can have at most {MaxEpicNamesPerCategory} epic names.", nameof(epicNames));
            }

            foreach (var name in names)
            {
                if (name.Length == 0 || name.Length > MaxEpicNameLength)
                {
                    throw new ArgumentException($"An epic name must be 1 to {MaxEpicNameLength} characters.", nameof(epicNames));
                }

                var key = WorkCategories.Normalize(name);
                if (key.Length == 0)
                {
                    throw new ArgumentException($"The epic name \"{name}\" needs a letter or a digit.", nameof(epicNames));
                }

                if (seen.TryGetValue(key, out var other) && other != category)
                {
                    throw new ArgumentException($"\"{name}\" names both {other} and {category}; an epic name can name one category.", nameof(epicNames));
                }

                seen[key] = category;
            }

            cleaned[category.ToString()] = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        WorkCategoryEpicNames = JsonSerializer.Serialize(cleaned);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateJiraBaseUrl(string? jiraBaseUrl)
    {
        JiraBaseUrl = string.IsNullOrWhiteSpace(jiraBaseUrl) ? null : jiraBaseUrl.TrimEnd('/');
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateJiraEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            JiraEmail = null;
        }
        else
        {
            var trimmed = email.Trim();
            if (trimmed.Length > 255)
            {
                throw new ArgumentException("Jira email cannot exceed 255 characters.", nameof(email));
            }

            var at = trimmed.IndexOf('@');
            if (at <= 0 || at == trimmed.Length - 1 || trimmed.Contains(' '))
            {
                throw new ArgumentException("Jira email must be a valid email address.", nameof(email));
            }

            JiraEmail = trimmed;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateJiraApiToken(string? apiToken)
    {
        if (!string.IsNullOrWhiteSpace(apiToken) && apiToken.Trim().Length > 500)
        {
            throw new ArgumentException("Jira API token cannot exceed 500 characters.", nameof(apiToken));
        }

        JiraApiToken = string.IsNullOrWhiteSpace(apiToken) ? null : apiToken.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateJiraBoardId(int? boardId)
    {
        if (boardId.HasValue && boardId.Value <= 0)
        {
            throw new ArgumentException("Jira board ID must be a positive number.", nameof(boardId));
        }

        JiraBoardId = boardId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateJiraProjectKey(string? projectKey)
    {
        if (string.IsNullOrWhiteSpace(projectKey))
        {
            JiraProjectKey = null;
        }
        else
        {
            var key = projectKey.Trim().ToUpperInvariant();
            if (key.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Z][A-Z0-9_]*$"))
            {
                throw new ArgumentException("Jira space key must be letters and digits starting with a letter, e.g. MAR.", nameof(projectKey));
            }

            JiraProjectKey = key;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasJiraApiToken => !string.IsNullOrWhiteSpace(JiraApiToken);

    public void UpdateGoogleCalendarUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            GoogleCalendarUrl = null;
        }
        else
        {
            var trimmed = url.Trim();
            if (trimmed.Length > 2000)
            {
                throw new ArgumentException("Google Calendar address cannot exceed 2000 characters.", nameof(url));
            }

            // The address carries the calendar's secret, so it is only ever fetched over HTTPS
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    "Google Calendar address must be an https:// link: the secret address in iCal format.", nameof(url));
            }

            GoogleCalendarUrl = trimmed;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasGoogleCalendarUrl => !string.IsNullOrWhiteSpace(GoogleCalendarUrl);

    /// <summary>
    /// The Jira site root (scheme and host) derived from <see cref="JiraBaseUrl"/>, which is
    /// stored with its browse path for building issue links. Null when no usable URL is set.
    /// </summary>
    public string? GetJiraSiteUrl()
    {
        if (string.IsNullOrWhiteSpace(JiraBaseUrl) ||
            !Uri.TryCreate(JiraBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    /// <summary>
    /// Where a Jira issue opens in the browser: the issue key is appended to this, e.g.
    /// <c>https://yourteam.atlassian.net/browse</c> + <c>/MAR-128</c>. Derived from the site root,
    /// so it is right whether or not <see cref="JiraBaseUrl"/> was entered with its browse path.
    /// Null when no usable URL is set.
    /// </summary>
    public string? GetJiraBrowseUrl()
    {
        var site = GetJiraSiteUrl();
        return site is null ? null : $"{site}/browse";
    }

    /// <summary>
    /// Names the settings still missing before sprint dates can be synced from Jira.
    /// An empty list means that sync is fully configured.
    /// </summary>
    public IReadOnlyList<string> GetMissingJiraSprintSyncSettings()
    {
        var missing = GetMissingJiraConnectionSettings();
        if (!JiraBoardId.HasValue) missing.Add("Jira board ID");
        return missing;
    }

    /// <summary>
    /// Names the settings still missing before parents can be synced from Jira epics.
    /// An empty list means that sync is fully configured.
    /// </summary>
    public IReadOnlyList<string> GetMissingJiraParentSyncSettings()
    {
        var missing = GetMissingJiraConnectionSettings();
        if (string.IsNullOrWhiteSpace(JiraProjectKey)) missing.Add("Jira space key");
        return missing;
    }

    private List<string> GetMissingJiraConnectionSettings()
    {
        var missing = new List<string>();
        if (GetJiraSiteUrl() is null) missing.Add("Jira base URL");
        if (string.IsNullOrWhiteSpace(JiraEmail)) missing.Add("Jira email");
        if (!HasJiraApiToken) missing.Add("Jira API token");
        return missing;
    }
}
