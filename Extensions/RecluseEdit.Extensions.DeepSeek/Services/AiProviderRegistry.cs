using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.DeepSeek.Services;

/// <summary>
/// Pre-configured metadata for an AI provider.
/// </summary>
public record AiProviderDescriptor(
    string Id,
    string DisplayName,
    AiProviderType Type,
    string DefaultEndpoint,
    string DefaultModel,
    IReadOnlyList<string> RecommendedModels,
    AiAuthMode DefaultAuthMode,
    string Description
) : IAiProvider
{
    public AiProviderType ProviderType => Type;
    public AiAuthMode DefaultAuthMode { get; } = DefaultAuthMode;
    public bool RequiresAuthentication => DefaultAuthMode != AiAuthMode.LocalNoAuth;

    public string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) ||
            (Type != AiProviderType.DeepSeek && string.Equals(endpoint.Trim(), "https://api.deepseek.com/chat/completions", StringComparison.OrdinalIgnoreCase)))
        {
            return DefaultEndpoint;
        }

        var ep = endpoint.Trim();

        // Anthropic native endpoint
        if (Type == AiProviderType.Anthropic && (ep.Contains("api.anthropic.com") || ep.EndsWith("/messages", StringComparison.OrdinalIgnoreCase)))
        {
            if (ep.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase)) return ep;
            return $"{ep.TrimEnd('/')}/v1/messages";
        }

        // Standard OpenAI-compatible endpoints
        if (ep.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            return ep;
        if (ep.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            return $"{ep}/chat/completions";
        return $"{ep.TrimEnd('/')}/chat/completions";
    }

    public async Task<string> GenerateCompletionAsync(
        string prompt,
        string? systemPrompt = null,
        Action<string>? onDeltaReceived = null,
        CancellationToken ct = default)
    {
        var settingsService = new DeepSeekSettingsService();
        var settings = settingsService.CurrentSettings;
        var client = new DeepSeekApiClient();
        return await client.GenerateCompletionDirectAsync(settings, this, prompt, systemPrompt, onDeltaReceived, ct);
    }
}

/// <summary>
/// Registry of supported AI model providers and credential discovery services.
/// </summary>
public static class AiProviderRegistry
{
    private static readonly HttpClient ProbeClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public static readonly IReadOnlyList<AiProviderDescriptor> Providers =
    [
        new AiProviderDescriptor(
            "deepseek",
            "DeepSeek",
            AiProviderType.DeepSeek,
            "https://api.deepseek.com/chat/completions",
            "deepseek-chat",
            ["deepseek-chat", "deepseek-reasoner"],
            AiAuthMode.ApiKey,
            "DeepSeek V3 & R1 reasoning models with standard API key."
        ),
        new AiProviderDescriptor(
            "openai",
            "OpenAI / ChatGPT",
            AiProviderType.OpenAi,
            "https://api.openai.com/v1/chat/completions",
            "gpt-4o",
            ["gpt-4o", "gpt-4o-mini", "o1", "o3-mini", "chatgpt-4o-latest"],
            AiAuthMode.ApiKey,
            "OpenAI GPT-4o & reasoning models via API key or ChatGPT subscription access token."
        ),
        new AiProviderDescriptor(
            "google_antigravity",
            "Google Antigravity & Gemini",
            AiProviderType.GoogleAntigravity,
            "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
            "gemini-2.5-flash",
            ["gemini-2.5-flash", "gemini-2.5-pro", "gemini-2.0-flash"],
            AiAuthMode.ApiKey,
            "Gemini 2.5 via Google Account bearer token / Antigravity subscription or Gemini API key."
        ),
        new AiProviderDescriptor(
            "anthropic",
            "Anthropic Claude",
            AiProviderType.Anthropic,
            "https://api.anthropic.com/v1/messages",
            "claude-3-7-sonnet-20250219",
            ["claude-3-7-sonnet-20250219", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022"],
            AiAuthMode.ApiKey,
            "Anthropic Claude 3.7 & 3.5 Sonnet/Haiku models via API key."
        ),
        new AiProviderDescriptor(
            "ollama",
            "Ollama (Local Offline)",
            AiProviderType.Ollama,
            "http://localhost:11434/v1/chat/completions",
            "llama3.3",
            ["llama3.3", "qwen2.5-coder", "deepseek-r1", "mistral"],
            AiAuthMode.LocalNoAuth,
            "Zero-auth private local offline LLM running on localhost:11434."
        ),
        new AiProviderDescriptor(
            "custom",
            "Custom OpenAI-Compatible",
            AiProviderType.Custom,
            "http://localhost:8000/v1/chat/completions",
            "default",
            ["default"],
            AiAuthMode.ApiKey,
            "Any OpenAI-compatible gateway (Groq, Together, OpenRouter, vLLM, LM Studio)."
        )
    ];

    public static AiProviderDescriptor GetProvider(string? providerId)
    {
        var match = Providers.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
        return match ?? Providers[0];
    }

    /// <summary>
    /// Attempts to automatically discover credentials from the local system (environment variables, gcloud CLI, Ollama port).
    /// </summary>
    public static async Task<(bool Found, string? Token, string? Info, string? SuggestedModel)> AutoDetectCredentialsAsync(string providerId)
    {
        var provider = GetProvider(providerId);

        switch (provider.Type)
        {
            case AiProviderType.GoogleAntigravity:
            {
                // 1. Check GEMINI_API_KEY, GOOGLE_API_KEY, or ANTIGRAVITY_API_KEY
                var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                    ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY") 
                    ?? Environment.GetEnvironmentVariable("ANTIGRAVITY_API_KEY");
                if (!string.IsNullOrWhiteSpace(geminiKey))
                {
                    return (true, geminiKey.Trim(), "Detected Gemini/Antigravity API key from environment variables.", "gemini-2.5-flash");
                }

                // 2. Check gcloud auth token if gcloud is installed
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "gcloud",
                        Arguments = OperatingSystem.IsWindows() ? "/c gcloud auth print-access-token" : "auth print-access-token",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        var token = (await p.StandardOutput.ReadToEndAsync()).Trim();
                        await p.WaitForExitAsync();
                        if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(token) && !token.StartsWith("ERROR"))
                        {
                            return (true, token, "Detected Google Account OAuth access token via gcloud CLI.", "gemini-2.5-flash");
                        }
                    }
                }
                catch
                {
                    // gcloud not in PATH or failed
                }

                // 3. Check Google Application Default Credentials (ADC) path
                var adcPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (string.IsNullOrEmpty(adcPath) && OperatingSystem.IsWindows())
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var candidate = Path.Combine(appData, "gcloud", "application_default_credentials.json");
                    if (File.Exists(candidate)) adcPath = candidate;
                }
                if (!string.IsNullOrEmpty(adcPath) && File.Exists(adcPath))
                {
                    return (true, $"file:{adcPath}", $"Detected Google ADC credentials file at {Path.GetFileName(adcPath)}.", "gemini-2.5-flash");
                }

                return (false, null, "No Google credentials detected in environment, gcloud CLI, or ADC.", null);
            }

            case AiProviderType.OpenAi:
            {
                var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(openAiKey))
                {
                    return (true, openAiKey.Trim(), "Detected OPENAI_API_KEY from environment variables.", "gpt-4o");
                }

                var chatGptToken = Environment.GetEnvironmentVariable("CHATGPT_ACCESS_TOKEN") 
                    ?? Environment.GetEnvironmentVariable("OPENAI_SESSION_TOKEN")
                    ?? Environment.GetEnvironmentVariable("CODEX_API_KEY");
                if (!string.IsNullOrWhiteSpace(chatGptToken))
                {
                    return (true, chatGptToken.Trim(), "Detected ChatGPT/Codex account session token from environment.", "chatgpt-4o-latest");
                }

                return (false, null, "No OPENAI_API_KEY or ChatGPT/Codex session token detected in environment.", null);
            }

            case AiProviderType.DeepSeek:
            {
                var deepSeekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
                if (!string.IsNullOrWhiteSpace(deepSeekKey))
                {
                    return (true, deepSeekKey.Trim(), "Detected DEEPSEEK_API_KEY from environment variables.", "deepseek-chat");
                }
                return (false, null, "No DEEPSEEK_API_KEY detected in environment variables.", null);
            }

            case AiProviderType.Anthropic:
            {
                var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
                if (!string.IsNullOrWhiteSpace(anthropicKey))
                {
                    return (true, anthropicKey.Trim(), "Detected Anthropic/Claude API key from environment variables.", "claude-3-7-sonnet-20250219");
                }
                return (false, null, "No ANTHROPIC_API_KEY detected in environment variables.", null);
            }

            case AiProviderType.Ollama:
            {
                try
                {
                    var res = await ProbeClient.GetAsync("http://localhost:11434/api/tags");
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("models", out var modelsElem) && modelsElem.GetArrayLength() > 0)
                        {
                            var firstModel = modelsElem[0].GetProperty("name").GetString();
                            return (true, string.Empty, $"Ollama is running on localhost:11434 with {modelsElem.GetArrayLength()} model(s).", firstModel);
                        }
                        return (true, string.Empty, "Ollama is running on localhost:11434 (ready).", "llama3.3");
                    }
                }
                catch
                {
                    // Ollama not running locally
                }
                return (false, null, "Ollama service is not responding on http://localhost:11434.", null);
            }

            default:
                return (false, null, "Auto-detection not supported for custom endpoints.", null);
        }
    }
}
