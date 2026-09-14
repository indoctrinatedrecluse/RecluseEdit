using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Pluggable provider interface for AI language models and conversational assistants.
/// Enables runtime switching between DeepSeek, OpenAI/ChatGPT, Google Antigravity/Gemini, Anthropic, Ollama, and custom endpoints.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Unique provider identifier (e.g. "provider.deepseek", "provider.openai", "provider.google_antigravity", "provider.ollama").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Underlying AI provider category.
    /// </summary>
    AiProviderType ProviderType { get; }

    /// <summary>
    /// Recommended authentication mechanism.
    /// </summary>
    AiAuthMode DefaultAuthMode { get; }

    /// <summary>
    /// Default API endpoint URL.
    /// </summary>
    string DefaultEndpoint { get; }

    /// <summary>
    /// Default model name.
    /// </summary>
    string DefaultModel { get; }

    /// <summary>
    /// List of recommended models available for this provider.
    /// </summary>
    IReadOnlyList<string> RecommendedModels { get; }

    /// <summary>
    /// Whether this provider requires an API key or account token (false for local Ollama).
    /// </summary>
    bool RequiresAuthentication => DefaultAuthMode != AiAuthMode.LocalNoAuth;

    /// <summary>
    /// Normalizes and validates the user-configured endpoint URL for this provider.
    /// </summary>
    string NormalizeEndpoint(string endpoint);

    /// <summary>
    /// Generates a streaming text completion for the given prompt or code snippet.
    /// </summary>
    Task<string> GenerateCompletionAsync(
        string prompt,
        string? systemPrompt = null,
        Action<string>? onDeltaReceived = null,
        CancellationToken ct = default) => Task.FromResult(string.Empty);
}
