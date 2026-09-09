using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RecluseEdit.Extensions.DeepSeek.Models;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.DeepSeek.Services;

/// <summary>
/// Client for interacting with the DeepSeek API or compatible OpenAI endpoints.
/// Supports conversational turns, streaming SSE responses, and tool invocation (read, write, list, execute).
/// </summary>
public class DeepSeekApiClient
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

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
        var ep = endpoint.Trim();
        if (string.IsNullOrWhiteSpace(ep))
        {
            return "https://api.deepseek.com/chat/completions";
        }

        if (ep.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return ep;
        }

        if (ep.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            return $"{ep}/chat/completions";
        }

        if (ep.EndsWith("/"))
        {
            return $"{ep}chat/completions";
        }

        return $"{ep}/chat/completions";
    }

    /// <summary>
    /// Sends a conversational turn with streaming output and automatic tool call execution.
    /// </summary>
    public async Task<string> SendChatStreamAsync(
        DeepSeekSettings settings,
        List<ChatMessage> conversationHistory,
        IWorkspaceContext workspaceContext,
        Action<string> onDeltaReceived,
        Action<string>? onStatusUpdate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("API key is missing. Please configure your DeepSeek API key in the extension settings.");
        }

        var endpoint = NormalizeEndpoint(settings.ApiEndpoint);

        // Prepare request
        var requestPayload = new ChatCompletionRequest
        {
            Model = string.IsNullOrWhiteSpace(settings.Model) ? "deepseek-chat" : settings.Model,
            Messages = conversationHistory,
            Tools = AvailableTools,
            Stream = true,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : null
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var json = JsonSerializer.Serialize(requestPayload, JsonOpts);
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var fullAssistantContent = new StringBuilder();
        var pendingToolCalls = new Dictionary<int, (string id, string name, StringBuilder args)>();

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
                var chunk = JsonSerializer.Deserialize<ChatCompletionResponse>(dataPayload, JsonOpts);
                var choice = chunk?.Choices?.FirstOrDefault();
                if (choice?.Delta != null)
                {
                    // Delta text content
                    if (!string.IsNullOrEmpty(choice.Delta.Content))
                    {
                        fullAssistantContent.Append(choice.Delta.Content);
                        onDeltaReceived(choice.Delta.Content);
                    }

                    // Delta tool calls
                    if (choice.Delta.ToolCalls != null)
                    {
                        foreach (var tc in choice.Delta.ToolCalls)
                        {
                            var idx = tc.Index ?? 0;
                            if (!pendingToolCalls.TryGetValue(idx, out var existing))
                            {
                                existing = (tc.Id ?? Guid.NewGuid().ToString(), tc.Function?.Name ?? "", new StringBuilder());
                                pendingToolCalls[idx] = existing;
                            }
                            if (!string.IsNullOrEmpty(tc.Id)) existing.id = tc.Id;
                            if (!string.IsNullOrEmpty(tc.Function?.Name)) existing.name = tc.Function.Name;
                            if (!string.IsNullOrEmpty(tc.Function?.Arguments)) existing.args.Append(tc.Function.Arguments);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore partial JSON parse errors in stream
            }
        }

        // If tools were called, execute them and continue the conversation
        if (pendingToolCalls.Count > 0)
        {
            var toolCallsList = new List<ToolCall>();
            foreach (var kvp in pendingToolCalls.OrderBy(p => p.Key))
            {
                toolCallsList.Add(new ToolCall
                {
                    Id = kvp.Value.id,
                    Type = "function",
                    Function = new FunctionCall
                    {
                        Name = kvp.Value.name,
                        Arguments = kvp.Value.args.ToString()
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

            onStatusUpdate?.Invoke("DeepSeek is processing tool output...");

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
}
