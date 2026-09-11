using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hive.Application.Interfaces;
using Hive.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Services;

/// <summary>
/// Service for interacting with the Claude API for sentiment analysis.
/// </summary>
public class ClaudeApiService : IClaudeApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ILogger<ClaudeApiService> _logger;
    private const string ClaudeApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ClaudeModel = "claude-sonnet-4-20250514";

    public ClaudeApiService(
        HttpClient httpClient,
        IAppSettingsRepository settingsRepository,
        ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default)
    {
        if (notes.Count == 0)
        {
            return new SentimentAnalysisResult(
                Positive: 0,
                Neutral: 100,
                Negative: 0,
                OverallSentiment: "Neutral",
                KeyThemes: Array.Empty<string>(),
                MonthlyBreakdown: Array.Empty<MonthlySentiment>()
            );
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings is null || string.IsNullOrWhiteSpace(settings.ClaudeApiKey))
        {
            throw new InvalidOperationException("Claude API key is not configured.");
        }

        var prompt = BuildPrompt(notes);
        var response = await CallClaudeApiAsync(settings.ClaudeApiKey, prompt, cancellationToken);

        return ParseResponse(response, notes);
    }

    public async Task<(bool IsValid, string? Error)> ValidateApiKeyAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key cannot be empty.");
        }

        try
        {
            // Make a minimal request to validate the API key
            var request = new ClaudeRequest
            {
                Model = ClaudeModel,
                MaxTokens = 10,
                Messages = new[]
                {
                    new ClaudeMessage { Role = "user", Content = "Say 'valid'" }
                }
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ClaudeApiUrl);
            httpRequest.Content = content;
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("API key validation failed: {StatusCode} - {Error}", response.StatusCode, errorContent);

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => (false, "Invalid API key."),
                System.Net.HttpStatusCode.Forbidden => (false, "API key does not have permission."),
                _ => (false, $"API error: {response.StatusCode}")
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error validating API key");
            return (false, "Network error. Please check your internet connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating API key");
            return (false, "An unexpected error occurred.");
        }
    }

    private static string BuildPrompt(IReadOnlyList<MeetingNoteForAnalysis> notes)
    {
        var notesByMonth = notes
            .GroupBy(n => new { n.MeetingDate.Year, n.MeetingDate.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month);

        var sb = new StringBuilder();
        sb.AppendLine("Analyze the sentiment of these engineering 1:1 meeting notes and provide insights about team member morale and engagement.");
        sb.AppendLine();
        sb.AppendLine("Return a JSON response with this exact structure:");
        sb.AppendLine(@"{
  ""positive"": <number 0-100>,
  ""neutral"": <number 0-100>,
  ""negative"": <number 0-100>,
  ""overallSentiment"": ""Positive"" | ""Neutral"" | ""Negative"" | ""Mixed"",
  ""keyThemes"": [""theme1"", ""theme2"", ...],
  ""monthlyBreakdown"": [
    { ""year"": 2024, ""month"": 1, ""positive"": 70, ""neutral"": 20, ""negative"": 10, ""notesCount"": 5 },
    ...
  ]
}");
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Sentiment scores must total 100");
        sb.AppendLine("- Identify up to 5 key themes (e.g., 'career growth', 'workload concerns', 'team collaboration')");
        sb.AppendLine("- Consider note categories: Discussion, ActionItem, Feedback, CareerDevelopment, Blocker, Achievement, Personal, FollowUp, Agenda");
        sb.AppendLine("- 'Blocker' and concerns indicate potential negative sentiment");
        sb.AppendLine("- 'Achievement' and positive feedback indicate positive sentiment");
        sb.AppendLine();
        sb.AppendLine("Meeting notes by month:");
        sb.AppendLine();

        foreach (var monthGroup in notesByMonth)
        {
            var monthName = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1).ToString("MMMM yyyy");
            sb.AppendLine($"### {monthName} ({monthGroup.Count()} notes)");

            foreach (var note in monthGroup)
            {
                sb.AppendLine($"- {note.Content}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Respond only with the JSON, no additional text.");

        return sb.ToString();
    }

    private async Task<string> CallClaudeApiAsync(string apiKey, string prompt, CancellationToken cancellationToken)
    {
        var request = new ClaudeRequest
        {
            Model = ClaudeModel,
            MaxTokens = 2048,
            Messages = new[]
            {
                new ClaudeMessage { Role = "user", Content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ClaudeApiUrl);
        httpRequest.Content = content;
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Claude API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Claude API error: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent, JsonOptions);

        if (claudeResponse?.Content?.FirstOrDefault()?.Text is null)
        {
            throw new InvalidOperationException("Invalid response from Claude API.");
        }

        return claudeResponse.Content.First().Text;
    }

    private SentimentAnalysisResult ParseResponse(string response, IReadOnlyList<MeetingNoteForAnalysis> notes)
    {
        try
        {
            // Extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var result = JsonSerializer.Deserialize<ClaudeSentimentResponse>(response, JsonOptions);
            if (result is null)
            {
                throw new InvalidOperationException("Failed to parse sentiment response.");
            }

            var monthlyBreakdown = result.MonthlyBreakdown?
                .Select(m => new MonthlySentiment(m.Year, m.Month, m.Positive, m.Neutral, m.Negative, m.NotesCount))
                .ToList() ?? new List<MonthlySentiment>();

            // If no monthly breakdown provided, create one from the notes
            if (monthlyBreakdown.Count == 0)
            {
                monthlyBreakdown = notes
                    .GroupBy(n => new { n.MeetingDate.Year, n.MeetingDate.Month })
                    .Select(g => new MonthlySentiment(
                        g.Key.Year,
                        g.Key.Month,
                        result.Positive,
                        result.Neutral,
                        result.Negative,
                        g.Count()))
                    .ToList();
            }

            return new SentimentAnalysisResult(
                Positive: result.Positive,
                Neutral: result.Neutral,
                Negative: result.Negative,
                OverallSentiment: result.OverallSentiment ?? "Unknown",
                KeyThemes: result.KeyThemes ?? Array.Empty<string>(),
                MonthlyBreakdown: monthlyBreakdown
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response: {Response}", response);
            throw new InvalidOperationException("Failed to parse sentiment analysis response.", ex);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Claude API request/response models
    private class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public ClaudeMessage[] Messages { get; set; } = Array.Empty<ClaudeMessage>();
    }

    private class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public ClaudeContent[]? Content { get; set; }
    }

    private class ClaudeContent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class ClaudeSentimentResponse
    {
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
        public string? OverallSentiment { get; set; }
        public string[]? KeyThemes { get; set; }
        public ClaudeMonthlyBreakdown[]? MonthlyBreakdown { get; set; }
    }

    private class ClaudeMonthlyBreakdown
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
        public int NotesCount { get; set; }
    }
}
