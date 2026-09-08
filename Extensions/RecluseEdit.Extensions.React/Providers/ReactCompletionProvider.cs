using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.React.Providers;

/// <summary>
/// Provides inline autocomplete and snippets for React (JSX/TSX), including React Hooks and component boilerplate.
/// </summary>
public class ReactCompletionProvider : IInlineCompletionProvider
{
    public string Id => "react.inline.completion";
    public string Name => "React Hooks & JSX Completions";

    public IReadOnlyList<string> SupportedLanguages => ["jsx", "tsx", "javascript", "typescript"];

    private static readonly Dictionary<string, string> ReactCompletions = new()
    {
        // React Hooks
        { "useState", "<string>('');" },
        { "const [state, set", "State] = useState(initialState);" },
        { "useEff", "ect(() => {\n  \n  return () => {\n    \n  };\n}, []);" },
        { "useCall", "back(() => {\n  \n}, []);" },
        { "useMem", "o(() => {\n  return computeExpensiveValue();\n}, []);" },
        { "useRef", "(null);" },
        { "useCont", "ext(MyContext);" },
        { "useRed", "ucer(reducer, initialArg, init);" },
        { "useId", "();" },
        { "useLayoutEff", "ect(() => {\n  \n}, []);" },

        // Component Snippets
        { "rfc", "() {\n  return (\n    <div>\n      \n    </div>\n  );\n}" },
        { "rafc", " = () => {\n  return (\n    <div>\n      \n    </div>\n  );\n};" },
        { "export const App = () =>", " {\n  return (\n    <main className=\"container\">\n      <h1>Hello React!</h1>\n    </main>\n  );\n};" },

        // Common JSX Attributes & Handlers
        { "classN", "ame=\"\"" },
        { "htmlF", "or=\"\"" },
        { "onCli", "ck={(event) => {\n  \n}}" },
        { "onCha", "nge={(event) => set(event.target.value)}" },
        { "onSub", "mit={(event) => {\n  event.preventDefault();\n}}" },
        { "style={{", " display: 'flex', flexDirection: 'column' }}" },
        { "<React.Fr", "agment>\n  \n</React.Fragment>" },
        { "<Sus", "pense fallback={<div>Loading...</div>}>\n  \n</Suspense>" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        // Check for hook or snippet triggers
        foreach (var (key, completion) in ReactCompletions)
        {
            if (prefix.EndsWith(key, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(completion);
            }

            // Check partial prefix (e.g. prefix ends with "useStat")
            for (var len = key.Length - 1; len >= 4; len--)
            {
                var subKey = key[..len];
                if (prefix.EndsWith(subKey, StringComparison.Ordinal))
                {
                    var remainder = key[len..] + completion;
                    return Task.FromResult<string?>(remainder);
                }
            }
        }

        return Task.FromResult<string?>(null);
    }
}
