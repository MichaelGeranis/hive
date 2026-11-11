using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hive.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure.Services;

/// <summary>
/// <see cref="IOllamaApiService"/> over Ollama's REST API. The answer's shape is enforced with
/// the same JSON schema Claude gets, through Ollama's structured outputs (<c>format</c>).
/// </summary>
public class OllamaApiService : IOllamaApiService
{
    // Ollama's default context window is a few thousand tokens and silently drops what does
    // not fit, which would lose the oldest notes. The window is sized to the prompt instead.
    private const int MinContextTokens = 8192;
    private const int MaxContextTokens = 131072;
    private const int AnswerTokens = 2048;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaApiService> _logger;

    public OllamaApiService(HttpClient httpClient, ILogger<OllamaApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        OllamaConnection connection,
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default)
    {
        if (notes.Count == 0)
        {
            return SentimentPrompt.NoNotes();
        }

        var content = await ChatJsonAsync(
            connection,
            SentimentPrompt.Build(notes),
            SentimentPrompt.Schema,
            tooLarge: "There are too many notes in the analysis window for the local model. Shorten the window in Settings.",
            didNotFit: "The notes did not fit in the local model's context. Shorten the analysis window in Settings.",
            cancellationToken);

        try
        {
            return SentimentPrompt.Parse(content, notes)
                ?? throw new OllamaApiException("The local model returned an empty answer. Try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ollama returned unreadable sentiment JSON");
            throw new OllamaApiException("The local model returned an answer Hive could not read. Try again.", ex);
        }
    }

    public async Task<RetroSummaryResult> SummarizeRetrosAsync(
        OllamaConnection connection,
        IReadOnlyList<RetroForAnalysis> retros,
        CancellationToken cancellationToken = default)
    {
        if (retros.Count == 0)
        {
            throw new OllamaApiException("There are no retros to summarise.");
        }

        var content = await ChatJsonAsync(
            connection,
            RetroPrompt.Build(retros),
            RetroPrompt.Schema,
            tooLarge: "The retros are too long for the local model to read at once. Read fewer retros in Settings.",
            didNotFit: "The retros did not fit in the local model's context. Read fewer retros in Settings.",
            cancellationToken);

        try
        {
            return RetroPrompt.Parse(content)
                ?? throw new OllamaApiException("The local model returned an empty answer. Try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ollama returned unreadable retro summary JSON");
            throw new OllamaApiException("The local model returned an answer Hive could not read. Try again.", ex);
        }
    }

    public async Task<TaskReviewResult> ReviewTaskAsync(
        OllamaConnection connection,
        TaskForReview task,
        CancellationToken cancellationToken = default)
    {
        var content = await ChatJsonAsync(
            connection,
            TaskPrompt.Build(task),
            TaskPrompt.Schema,
            tooLarge: $"The description of \"{task.Title}\" is too long for the local model to read at once.",
            didNotFit: $"The description of \"{task.Title}\" did not fit in the local model's context.",
            cancellationToken);

        try
        {
            return TaskPrompt.Parse(content)
                ?? throw new OllamaApiException("The local model returned an empty answer. Try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ollama returned unreadable task review JSON");
            throw new OllamaApiException("The local model returned an answer Hive could not read. Try again.", ex);
        }
    }

    public async Task<(bool IsValid, string? Error)> CheckConnectionAsync(
        OllamaConnection connection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendAsync(
                connection,
                () => _httpClient.GetAsync(Endpoint(connection, "api/tags"), cancellationToken),
                cancellationToken);

            var tags = await response.Content.ReadFromJsonAsync<TagsResponse>(JsonOptions, cancellationToken);
            var installed = tags?.Models?.Select(m => m.Name).OfType<string>().ToList() ?? new List<string>();

            // "llama3.1" and "llama3.1:latest" name the same model
            var wanted = connection.Model.Contains(':') ? connection.Model : $"{connection.Model}:latest";
            if (installed.Any(name => string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)))
            {
                return (true, null);
            }

            return (false, $"Ollama is running, but the model {connection.Model} is not installed. Run: ollama pull {connection.Model}");
        }
        catch (OllamaApiException ex)
        {
            return (false, ex.Message);
        }
        catch (JsonException)
        {
            return (false, $"Something answered at {connection.BaseUrl}, but it does not look like Ollama.");
        }
    }

    /// <summary>
    /// Asks the model for an answer shaped by <paramref name="schema"/> and returns its JSON
    /// text. The prompt must fit in the context whole, or the call fails with the message given.
    /// </summary>
    private async Task<string> ChatJsonAsync(
        OllamaConnection connection,
        string prompt,
        Dictionary<string, JsonElement> schema,
        string tooLarge,
        string didNotFit,
        CancellationToken cancellationToken)
    {
        var contextTokens = ContextTokensFor(prompt);
        if (contextTokens > MaxContextTokens)
        {
            throw new OllamaApiException(tooLarge);
        }

        var request = new ChatRequest(
            Model: connection.Model,
            Messages: [new ChatMessage("user", prompt)],
            Stream: false,
            Format: schema,
            // Temperature 0: the same notes should give the same reading
            Options: new ChatOptions(Temperature: 0, NumCtx: contextTokens));

        var response = await SendAsync(
            connection,
            () => _httpClient.PostAsJsonAsync(Endpoint(connection, "api/chat"), request, JsonOptions, cancellationToken),
            cancellationToken);

        ChatResponse? chat;
        try
        {
            chat = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ollama returned an unreadable response");
            throw new OllamaApiException("Ollama returned an answer Hive could not read. Try again.", ex);
        }

        if (chat?.DoneReason == "length")
        {
            throw new OllamaApiException("The local model's answer was cut off before it finished. Try again.");
        }

        // A prompt that filled the whole window was cut short, so some of it was not read
        if (chat?.PromptEvalCount >= contextTokens)
        {
            throw new OllamaApiException(didNotFit);
        }

        var content = chat?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new OllamaApiException("The local model returned an empty answer. Try again.");
        }

        return content;
    }

    private static int ContextTokensFor(string prompt)
    {
        // About three characters per token, rounded up, is a safe overestimate for English text
        var needed = prompt.Length / 3 + AnswerTokens;
        var size = MinContextTokens;
        while (size < needed && size <= MaxContextTokens)
        {
            size *= 2;
        }

        return size;
    }

    private static Uri Endpoint(OllamaConnection connection, string path) =>
        new(new Uri(connection.BaseUrl.TrimEnd('/') + "/"), path);

    /// <summary>
    /// Runs one request and turns every failure into an <see cref="OllamaApiException"/> the
    /// manager can act on.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        OllamaConnection connection,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach Ollama at {BaseUrl}", connection.BaseUrl);
            throw new OllamaApiException(
                $"Could not reach Ollama at {connection.BaseUrl}. Check that Ollama is running.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timed out waiting for Ollama");
            throw new OllamaApiException(
                "The local model did not answer in time. A large model on a slow machine can take minutes; try a smaller model or a shorter window.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var error = await ReadErrorAsync(response, cancellationToken);
        _logger.LogWarning("Ollama returned {Status}: {Error}", (int)response.StatusCode, error);

        if (response.StatusCode == HttpStatusCode.NotFound && error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new OllamaApiException(
                $"The model {connection.Model} is not installed in Ollama. Run: ollama pull {connection.Model}");
        }

        throw new OllamaApiException(error is null
            ? $"Ollama returned an error ({(int)response.StatusCode})."
            : $"Ollama returned an error: {error}");
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, cancellationToken);
            return string.IsNullOrWhiteSpace(body?.Error) ? null : body.Error;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record ChatRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        bool Stream,
        Dictionary<string, JsonElement> Format,
        ChatOptions Options);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatOptions(
        double Temperature,
        [property: JsonPropertyName("num_ctx")] int NumCtx);

    private sealed record ChatResponse(
        ChatMessage? Message,
        [property: JsonPropertyName("done_reason")] string? DoneReason,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount);

    private sealed record TagsResponse(IReadOnlyList<TagModel>? Models);

    private sealed record TagModel(string? Name);

    private sealed record ErrorResponse(string? Error);
}
