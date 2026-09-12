using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.RestClient.Providers;

/// <summary>
/// Provides inline ghost-text completions for .http and .rest request files,
/// headers, methods, and payload templates.
/// </summary>
public class HttpCompletionProvider : IInlineCompletionProvider
{
    public string Id => "restclient.http.inline";
    public string Name => "HTTP / REST Request Templates";

    public IReadOnlyList<string> SupportedLanguages => ["http", "rest"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Content-Type: ", "application/json" },
        { "Accept: ", "application/json" },
        { "Authorization: ", "Bearer {{token}}" },
        { "User-Agent: ", "RecluseEdit-RestClient/1.0" },
        { "X-API-Key: ", "{{apiKey}}" },
        { "Cache-Control: ", "no-cache" },
        { "GET ", "https://api.example.com/resource\nAccept: application/json" },
        { "POST ", "https://api.example.com/resource\nContent-Type: application/json\n\n{\n  \"name\": \"example\"\n}" },
        { "PUT ", "https://api.example.com/resource/1\nContent-Type: application/json\n\n{\n  \"name\": \"updated\"\n}" },
        { "PATCH ", "https://api.example.com/resource/1\nContent-Type: application/json\n\n{\n  \"status\": \"active\"\n}" },
        { "DELETE ", "https://api.example.com/resource/1" },
        { "###", " New Request\nGET https://api.example.com/" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (trigger, suggestion) in Completions)
        {
            if (prefix.EndsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

