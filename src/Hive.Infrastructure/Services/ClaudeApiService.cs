using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using Hive.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Services;

/// <summary>
/// <see cref="IClaudeApiService"/> over the official Anthropic SDK. The SDK sends its requests
/// through the <see cref="HttpClient"/> that <c>AddHttpClient</c> manages for this class.
/// </summary>
public class ClaudeApiService : IClaudeApiService
{
    private const string ClaudeModel = "claude-opus-5";

    // Room for adaptive thinking as well as the answer; the JSON itself is small
    private const int MaxTokens = 16000;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeApiService> _logger;

    public ClaudeApiService(HttpClient httpClient, ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        string apiKey,
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ClaudeApiException("Claude API key is not configured.");
        }

        if (notes.Count == 0)
        {
            return SentimentPrompt.NoNotes();
        }

        var json = await CreateJsonAsync(apiKey, SentimentPrompt.Build(notes), SentimentPrompt.Schema, "sentiment analysis", cancellationToken);

        SentimentAnalysisResult? result;
        try
        {
            result = SentimentPrompt.Parse(json, notes);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Claude returned unreadable sentiment JSON");
            throw new ClaudeApiException("Claude returned an answer Hive could not read. Try again.", ex);
        }

        return result ?? throw new ClaudeApiException("Claude returned an empty answer. Try again.");
    }

    public async Task<RetroSummaryResult> SummarizeRetrosAsync(
        string apiKey,
        IReadOnlyList<RetroForAnalysis> retros,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ClaudeApiException("Claude API key is not configured.");
        }

        if (retros.Count == 0)
        {
            throw new ClaudeApiException("There are no retros to summarise.");
        }

        var json = await CreateJsonAsync(apiKey, RetroPrompt.Build(retros), RetroPrompt.Schema, "retro summary", cancellationToken);

        RetroSummaryResult? result;
        try
        {
            result = RetroPrompt.Parse(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Claude returned unreadable retro summary JSON");
            throw new ClaudeApiException("Claude returned an answer Hive could not read. Try again.", ex);
        }

        return result ?? throw new ClaudeApiException("Claude returned an empty answer. Try again.");
    }

    public async Task<TaskReviewResult> ReviewTaskAsync(
        string apiKey,
        TaskForReview task,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ClaudeApiException("Claude API key is not configured.");
        }

        var json = await CreateJsonAsync(apiKey, TaskPrompt.Build(task), TaskPrompt.Schema, "task review", cancellationToken);

        TaskReviewResult? result;
        try
        {
            result = TaskPrompt.Parse(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Claude returned unreadable task review JSON");
            throw new ClaudeApiException("Claude returned an answer Hive could not read. Try again.", ex);
        }

        return result ?? throw new ClaudeApiException("Claude returned an empty answer. Try again.");
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
            // Looking up the model checks the key, and that it can use the model, for free
            await SendAsync(
                apiKey,
                client => client.Models.Retrieve(ClaudeModel, cancellationToken: cancellationToken),
                cancellationToken);
            return (true, null);
        }
        catch (ClaudeApiException ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Runs one SDK call with the manager's key and turns every failure into a
    /// <see cref="ClaudeApiException"/> the manager can act on. The SDK has already retried
    /// rate limits, server errors and dropped connections by the time one surfaces here.
    /// </summary>
    private async Task<T> SendAsync<T>(string apiKey, Func<AnthropicClient, Task<T>> call, CancellationToken cancellationToken)
    {
        var client = new AnthropicClient
        {
            ApiKey = apiKey,
            HttpClient = _httpClient,
            Timeout = TimeSpan.FromMinutes(5),
        };

        try
        {
            return await call(client);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Claude rejected the API key");
            throw new ClaudeApiException("Claude rejected the API key. Check it in Settings.", ex);
        }
        catch (AnthropicForbiddenException ex)
        {
            _logger.LogWarning(ex, "Claude API key lacks permission");
            throw new ClaudeApiException("This API key does not have permission to use Claude.", ex);
        }
        catch (AnthropicNotFoundException ex)
        {
            _logger.LogWarning(ex, "Claude model {Model} not found", ClaudeModel);
            throw new ClaudeApiException($"The Claude model {ClaudeModel} is not available to this API key.", ex);
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Claude rate limit reached");
            throw new ClaudeApiException("Claude is rate limiting requests. Wait a minute and try again.", ex);
        }
        catch (Anthropic5xxException ex)
        {
            _logger.LogError(ex, "Claude server error");
            throw new ClaudeApiException("Claude is temporarily unavailable. Try again in a few minutes.", ex);
        }
        catch (AnthropicBadRequestException ex)
        {
            _logger.LogError(ex, "Claude rejected the request");
            throw new ClaudeApiException($"Claude rejected the request: {ex.Message}", ex);
        }
        catch (AnthropicIOException ex)
        {
            _logger.LogError(ex, "Network error calling Claude");
            throw new ClaudeApiException("Could not reach the Claude API. Check your internet connection.", ex);
        }
        catch (AnthropicApiException ex)
        {
            // Anything else, e.g. a 402 when the account has run out of credit
            _logger.LogError(ex, "Claude API error");
            throw new ClaudeApiException($"Claude returned an error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timed out calling Claude");
            throw new ClaudeApiException("Claude did not respond in time. Try again.", ex);
        }
    }

    /// <summary>
    /// Asks Claude for an answer shaped by <paramref name="schema"/> and returns its JSON text.
    /// </summary>
    private async Task<string> CreateJsonAsync(
        string apiKey,
        string prompt,
        Dictionary<string, JsonElement> schema,
        string purpose,
        CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = ClaudeModel,
            MaxTokens = MaxTokens,
            // A policy decline is retried server-side on the model Anthropic recommends for
            // that refusal category, instead of coming back as an empty answer
            Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
            Fallbacks = new Default(),
            OutputConfig = new BetaOutputConfig { Format = new BetaJsonOutputFormat { Schema = schema } },
            Messages = [new() { Role = Role.User, Content = prompt }],
        };

        var response = await SendAsync(
            apiKey,
            client => client.Beta.Messages.Create(parameters, cancellationToken),
            cancellationToken);

        // The stop reason decides whether there is an answer at all; check it before content
        if (response.StopReason == "refusal")
        {
            _logger.LogWarning("Claude declined the {Purpose}: {Details}", purpose, response.StopDetails);
            throw new ClaudeApiException("Claude declined to analyse these notes.");
        }

        if (response.StopReason == "max_tokens")
        {
            throw new ClaudeApiException("Claude's answer was cut off before it finished. Try again.");
        }

        // After a server-side fallback the substitute model's answer follows a fallback
        // block; anything before it came from the model that declined
        var json = new StringBuilder();
        foreach (var block in response.Content)
        {
            if (block.TryPickFallback(out _))
            {
                json.Clear();
            }
            else if (block.TryPickText(out var text))
            {
                json.Append(text.Text);
            }
        }

        return json.ToString();
    }
}
