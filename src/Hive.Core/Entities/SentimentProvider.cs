namespace Hive.Core.Entities;

/// <summary>
/// Which model reads the 1:1 notes for sentiment analysis.
/// </summary>
public enum SentimentProvider
{
    /// <summary>A model served by Ollama on the manager's own machine; nothing leaves it.</summary>
    Ollama,

    /// <summary>The Claude API, with the manager's own API key.</summary>
    Claude
}
