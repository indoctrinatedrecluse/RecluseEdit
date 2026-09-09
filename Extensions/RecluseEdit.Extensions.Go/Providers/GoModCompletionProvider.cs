using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Go.Providers;

/// <summary>
/// Provides inline autocomplete suggestions for go.mod, go.work, and Go workspace configurations.
/// </summary>
public class GoModCompletionProvider : IInlineCompletionProvider
{
    public string Id => "go.mod.completion";
    public string Name => "Go Modules & Workspaces (go.mod, go.work)";

    public IReadOnlyList<string> SupportedLanguages => ["gomod", "go"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        {
            "module ",
            "github.com/organization/project\n\ngo 1.23.1\n\nrequire (\n\tgithub.com/gin-gonic/gin v1.10.0\n)"
        },
        {
            "require (",
            "\n\tgithub.com/gin-gonic/gin v1.10.0\n\tgithub.com/google/uuid v1.6.0\n\tgorm.io/gorm v1.25.12\n\tgorm.io/driver/postgres v1.5.9\n)"
        },
        {
            "replace (",
            "\n\texample.com/pkg => ./internal/pkg\n)"
        },
        {
            "use (",
            "\n\t./service-api\n\t./service-worker\n)"
        },
        {
            "toolchain ",
            "go1.23.2"
        },
        {
            "retract ",
            "[v1.0.0, v1.0.1] // Published with critical flaw"
        }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText.TrimStart();

        foreach (var (prefix, suggestion) in Completions)
        {
            if (line.EndsWith(prefix, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
