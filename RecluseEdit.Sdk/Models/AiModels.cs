using System;
using System.Collections.Generic;

namespace RecluseEdit.Sdk.Models;

/// <summary>
/// Supported AI model providers.
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// DeepSeek AI API (deepseek-chat, deepseek-reasoner).
    /// </summary>
    DeepSeek,

    /// <summary>
    /// OpenAI API or ChatGPT account subscription (GPT-4o, GPT-4o-mini, o1, o3-mini).
    /// </summary>
    OpenAi,

    /// <summary>
    /// Google Antigravity & Gemini API (Gemini 2.5 Pro, Gemini 2.5 Flash, Gemini 2.0 Flash).
    /// Supports Google Account bearer tokens / ADC and direct Gemini API keys.
    /// </summary>
    GoogleAntigravity,

    /// <summary>
    /// Anthropic Claude API (Claude 3.5 Sonnet, Claude 3.7 Sonnet, Claude 3.5 Haiku).
    /// </summary>
    Anthropic,

    /// <summary>
    /// Local offline inference via Ollama (llama3.3, qwen2.5-coder, deepseek-r1, mistral).
    /// Zero external API calls, running on localhost:11434.
    /// </summary>
    Ollama,

    /// <summary>
    /// Custom OpenAI-compatible endpoint (Groq, Together, OpenRouter, vLLM, LM Studio).
    /// </summary>
    Custom
}

/// <summary>
/// Authentication mode used to communicate with an AI provider.
/// </summary>
public enum AiAuthMode
{
    /// <summary>
    /// Bring Your Own Key (standard secret API key).
    /// </summary>
    ApiKey,

    /// <summary>
    /// Account / Subscription Access Token (e.g. ChatGPT web session token, Google Account OAuth / Antigravity token).
    /// </summary>
    AccountToken,

    /// <summary>
    /// Local instance requiring zero authentication (e.g. Ollama on localhost:11434).
    /// </summary>
    LocalNoAuth
}

/// <summary>
/// Metadata describing a specific AI model.
/// </summary>
public class AiModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AiProviderType Provider { get; set; }
    public int ContextWindow { get; set; } = 128000;
    public bool SupportsTools { get; set; } = true;
    public bool SupportsStreaming { get; set; } = true;
    public string? Description { get; set; }

    public AiModelInfo(string id, string displayName, AiProviderType provider, int contextWindow = 128000, bool supportsTools = true)
    {
        Id = id;
        DisplayName = displayName;
        Provider = provider;
        ContextWindow = contextWindow;
        SupportsTools = supportsTools;
    }
}

