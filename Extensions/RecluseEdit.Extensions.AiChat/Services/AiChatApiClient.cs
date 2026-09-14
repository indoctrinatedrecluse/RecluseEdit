using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Extensions.AiChat.Models;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.AiChat.Services;

/// <summary>
/// Universal client for interacting with AI models (DeepSeek, OpenAI/ChatGPT, Google Antigravity/Gemini, Anthropic Claude, Ollama, Custom).
/// Supports conversational turns, streaming SSE responses, and tool invocation (read, write, list, execute).
/// </summary>
public class AiChatApiClient
{
    private static readonly HttpClient DefaultHttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private readonly HttpClient _httpClient;

    public AiChatApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<ToolDefinition> AvailableTools =>
    [
        new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = "read_file",
                Description = "Read the entire text content of a file in the workspace.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "The workspace-relative or absolute path of the file to read." }
                    },
                    required = new[] { "path" }
                }
            }
        },
        new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = "write_file",
                Description = "Create or overwrite a file in the workspace with given content.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "The workspace-relative or absolute path to write." },
                        content = new { type = "string", description = "The full text content to write into the file." }
                    },
                    required = new[] { "path", "content" }
                }
            }
        },
        new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = "list_files",
                Description = "List files and directories in the workspace root or a subfolder.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Subfolder path to inspect. Leave empty or omitted for the workspace root." }
                    }
                }
            }
        },
        new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = "execute_command",
                Description = "Execute a shell command in the workspace directory. The user will be prompted to approve execution before the command runs.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "The shell command to execute, e.g. 'npm test', 'dotnet build', 'git status'." },
                        working_directory = new { type = "string", description = "Optional working directory path." }
                    },
                    required = new[] { "command" }
                }
            }
        }
    ];

    public string NormalizeEndpoint(string endpoint)
    {
        return AiProviderRegistry.GetProvider("deepseek").NormalizeEndpoint(endpoint);
    }

    /// <summary>
    /// Generates a direct streaming completion without workspace tool calling.
    /// </summary>
    public async Task<string> GenerateCompletionDirectAsync(
        AiChatSettings settings,
        AiProviderDescriptor? providerDescriptor,
        string prompt,
        string? systemPrompt = null,
        Action<string>? onDeltaReceived = null,
        CancellationToken ct = default)
    {
        var provider = providerDescriptor ?? AiProviderRegistry.GetProvider(settings.Provider);
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        }
        messages.Add(new ChatMessage { Role = "user", Content = prompt });

        return await SendChatStreamAsync(
            settings,
            messages,
            null,
            onDeltaReceived ?? (_ => { }),
            null,
            ct);
    }

    /// <summary>
    /// Sends a conversational turn with streaming output and automatic tool call execution.
    /// </summary>
    public async Task<string> SendChatStreamAsync(
        AiChatSettings settings,
        List<ChatMessage> conversationHistory,
        IWorkspaceContext? workspaceContext,
        Action<string> onDeltaReceived,
        Action<string>? onStatusUpdate = null,
        CancellationToken ct = default)
    {
        var provider = AiProviderRegistry.GetProvider(settings.Provider);
        var effectiveToken = settings.GetEffectiveToken();

        if (!settings.IsLocalNoAuth && string.IsNullOrWhiteSpace(effectiveToken))
        {
            throw new InvalidOperationException($"API key / access token is missing for provider '{provider.DisplayName}'. Please configure your credentials in the AI Chat panel.");
        }

        var endpoint = provider.NormalizeEndpoint(settings.ApiEndpoint);
        bool isAnthropicNative = provider.Type == RecluseEdit.Sdk.Models.AiProviderType.Anthropic && 
            (endpoint.Contains("api.anthropic.com") || endpoint.EndsWith("/messages", StringComparison.OrdinalIgnoreCase));

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (isAnthropicNative)
        {
            requestMessage.Headers.Add("x-api-key", effectiveToken);
            requestMessage.Headers.Add("anthropic-version", "2023-06-01");

            var systemPrompt = conversationHistory.FirstOrDefault(m => m.Role == "system")?.Content;
            var anthropicMessages = conversationHistory
                .Where(m => m.Role != "system" && m.Role != "tool")
                .Select(m => new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content ?? "" })
                .ToList();

            var anthropicPayload = new
            {
                model = string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model,
                messages = anthropicMessages,
                system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                max_tokens = settings.MaxTokens > 0 ? settings.MaxTokens : 4096,
                stream = true,
                temperature = settings.Temperature
            };

            var json = JsonSerializer.Serialize(anthropicPayload, JsonOpts);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else
        {
            if (!settings.IsLocalNoAuth && !string.IsNullOrWhiteSpace(effectiveToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveToken);
            }

            var requestPayload = new ChatCompletionRequest
            {
                Model = string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model,
                Messages = conversationHistory,
                Tools = (provider.Type == RecluseEdit.Sdk.Models.AiProviderType.Ollama || workspaceContext == null) ? null : AvailableTools,
                Stream = true,
                Temperature = settings.Temperature,
                MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : null
            };

            var json = JsonSerializer.Serialize(requestPayload, JsonOpts);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API request to {provider.DisplayName} failed with status {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var fullAssistantContent = new StringBuilder();
        var pendingToolCalls = new Dictionary<int, PendingToolCall>();

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            line = line.Trim();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data:")) continue;

            var dataPayload = line.Substring(5).Trim();
            if (dataPayload == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(dataPayload);
                var root = doc.RootElement;

                // 1. OpenAI-compatible format
                if (root.TryGetProperty("choices", out var choicesElem) && choicesElem.GetArrayLength() > 0)
                {
                    var choice = choicesElem[0];
                    if (choice.TryGetProperty("delta", out var deltaElem))
                    {
                        if (deltaElem.TryGetProperty("content", out var contentElem))
                        {
                            var content = contentElem.GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                fullAssistantContent.Append(content);
                                onDeltaReceived(content);
                            }
                        }

                        // Delta tool calls
                        if (deltaElem.TryGetProperty("tool_calls", out var tcElem) && tcElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tc in tcElem.EnumerateArray())
                            {
                                int idx = tc.TryGetProperty("index", out var ie) ? ie.GetInt32() : 0;
                                if (!pendingToolCalls.TryGetValue(idx, out var existing))
                                {
                                    string id = tc.TryGetProperty("id", out var idElem) ? idElem.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                                    existing = new PendingToolCall { Id = id };
                                    pendingToolCalls[idx] = existing;
                                }

                                if (tc.TryGetProperty("id", out var idVal) && !string.IsNullOrEmpty(idVal.GetString()))
                                    existing.Id = idVal.GetString()!;

                                if (tc.TryGetProperty("function", out var fnElem))
                                {
                                    if (fnElem.TryGetProperty("name", out var nameVal) && !string.IsNullOrEmpty(nameVal.GetString()))
                                        existing.Name = nameVal.GetString()!;
                                    if (fnElem.TryGetProperty("arguments", out var argVal) && !string.IsNullOrEmpty(argVal.GetString()))
                                        existing.Args.Append(argVal.GetString());
                                }
                            }
                        }
                    }
                }
                // 2. Anthropic SSE format
                else if (root.TryGetProperty("delta", out var anthropicDelta) && anthropicDelta.TryGetProperty("text", out var textElem))
                {
                    var text = textElem.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        fullAssistantContent.Append(text);
                        onDeltaReceived(text);
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore partial JSON parse errors in stream
            }
        }

        // If tools were called and a workspace context was provided, execute them and continue the conversation
        if (workspaceContext != null && pendingToolCalls.Count > 0)
        {
            var toolCallsList = new List<ToolCall>();
            foreach (var kvp in pendingToolCalls.OrderBy(p => p.Key))
            {
                toolCallsList.Add(new ToolCall
                {
                    Id = kvp.Value.Id,
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = kvp.Value.Name,
                        Arguments = kvp.Value.Args.ToString()
                    }
                });
            }

            // Append assistant message with tool calls to conversation history
            var assistantMsg = new ChatMessage
            {
                Role = "assistant",
                Content = fullAssistantContent.Length > 0 ? fullAssistantContent.ToString() : null,
                ToolCalls = toolCallsList
            };
            conversationHistory.Add(assistantMsg);

            // Execute each tool
            foreach (var toolCall in toolCallsList)
            {
                ct.ThrowIfCancellationRequested();
                onStatusUpdate?.Invoke($"Running tool '{toolCall.Function.Name}'...");
                var toolOutput = await ExecuteToolAsync(toolCall.Function.Name, toolCall.Function.Arguments, workspaceContext, ct);

                conversationHistory.Add(new ChatMessage
                {
                    Role = "tool",
                    Name = toolCall.Function.Name,
                    ToolCallId = toolCall.Id,
                    Content = toolOutput
                });
            }

            onStatusUpdate?.Invoke($"{provider.DisplayName} is processing tool output...");

            // Recurse to let model generate final response using tool results
            return await SendChatStreamAsync(settings, conversationHistory, workspaceContext, onDeltaReceived, onStatusUpdate, ct);
        }

        return fullAssistantContent.ToString();
    }

    /// <summary>
    /// Dispatches a tool execution to the provided workspace context.
    /// </summary>
    public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, IWorkspaceContext context, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            switch (toolName.ToLowerInvariant())
            {
                case "read_file":
                {
                    var path = root.TryGetProperty("path", out var p) ? p.GetString() : "";
                    if (string.IsNullOrWhiteSpace(path)) return "Error: 'path' parameter is required.";
                    var content = await context.ReadFileAsync(path, ct);
                    return content;
                }

                case "write_file":
                {
                    var path = root.TryGetProperty("path", out var p) ? p.GetString() : "";
                    var content = root.TryGetProperty("content", out var c) ? c.GetString() : "";
                    if (string.IsNullOrWhiteSpace(path)) return "Error: 'path' parameter is required.";
                    await context.WriteFileAsync(path, content ?? "", ct);
                    return $"File '{path}' written successfully ({content?.Length ?? 0} characters).";
                }

                case "list_files":
                {
                    var path = root.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                    var files = await context.ListFilesAsync(path, ct);
                    if (files.Count == 0) return "(Directory is empty)";
                    return string.Join("\n", files);
                }

                case "execute_command":
                {
                    var command = root.TryGetProperty("command", out var c) ? c.GetString() : "";
                    var cwd = root.TryGetProperty("working_directory", out var w) ? w.GetString() : null;
                    if (string.IsNullOrWhiteSpace(command)) return "Error: 'command' parameter is required.";

                    var result = await context.ExecuteCommandAsync(command, cwd, ct);
                    if (!result.UserApproved)
                    {
                        return $"User confirmation denied: The user declined permission to execute command '{command}'.";
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"Exit Code: {result.ExitCode}");
                    if (!string.IsNullOrEmpty(result.StandardOutput))
                    {
                        sb.AppendLine("Output:\n" + result.StandardOutput);
                    }
                    if (!string.IsNullOrEmpty(result.StandardError))
                    {
                        sb.AppendLine("Error Output:\n" + result.StandardError);
                    }
                    return sb.ToString();
                }

                default:
                    return $"Unknown tool: '{toolName}'";
            }
        }
        catch (Exception ex)
        {
            return $"Tool execution failed: {ex.Message}";
        }
    }

    private class PendingToolCall
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public StringBuilder Args { get; } = new();
    }
}

/// <summary>
/// Backward-compatibility alias for DeepSeekApiClient.
/// </summary>
public class DeepSeekApiClient : AiChatApiClient
{
    public DeepSeekApiClient(HttpClient? httpClient = null) : base(httpClient) { }
}
