using System.Text.RegularExpressions;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Extensions.BuiltIn;

/// <summary>
/// Built-in extension providing web application development support:
/// HTML, CSS, JavaScript/TypeScript inline autocompletion and snippets.
/// </summary>
public class WebDevExtension : IExtension
{
    public string Id => "recluse.webdev";
    public string Name => "Web Development Essentials";
    public string Version => "1.0.0";
    public string Description => "Built-in web intelligence providing HTML, CSS, and JS inline completions";
    public string Author => "RecluseEdit Team";

    public void Initialize(IExtensionContext context)
    {
        // Register Web Completion Provider
        context.RegisterInlineCompletion(new WebInlineCompletionProvider());
        context.Log("Web Development Essentials extension initialized.");
    }

    public void Deinitialize()
    {
    }
}

/// <summary>
/// Inline completion provider for web technologies (HTML, CSS, JavaScript, TypeScript).
/// </summary>
public class WebInlineCompletionProvider : IInlineCompletionProvider
{
    public string Id => "web.inline.completion";
    public string Name => "Web Inline Completions";

    public IReadOnlyList<string> SupportedLanguages =>
        ["html", "xml", "css", "javascript", "typescript", "json"];

    // HTML completions triggered when user types specific tag starts or patterns
    private static readonly Dictionary<string, string> HtmlCompletions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "<!DOCTYPE", " html>\n<html lang=\"en\">\n<head>\n  <meta charset=\"UTF-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n  <title>New App</title>\n</head>\n<body>\n  \n</body>\n</html>" },
        { "<!doc", "type html>" },
        { "<div", "></div>" },
        { "<span", "></span>" },
        { "<button", " type=\"button\"></button>" },
        { "<input", " type=\"text\" name=\"\" id=\"\" />" },
        { "<a", " href=\"\"></a>" },
        { "<img", " src=\"\" alt=\"\" />" },
        { "<p", "></p>" },
        { "<h1", "></h1>" },
        { "<h2", "></h2>" },
        { "<h3", "></h3>" },
        { "<ul", ">\n  <li></li>\n</ul>" },
        { "<li", "></li>" },
        { "<form", " action=\"\" method=\"POST\">\n  \n</form>" },
        { "<script", " src=\"\"></script>" },
        { "<style", ">\n  \n</style>" },
        { "<link", " rel=\"stylesheet\" href=\"style.css\" />" },
        { "<table", ">\n  <thead>\n    <tr><th>Header</th></tr>\n  </thead>\n  <tbody>\n    <tr><td>Data</td></tr>\n  </tbody>\n</table>" },
        { "<nav", "></nav>" },
        { "<header", "></header>" },
        { "<footer", "></footer>" },
        { "<main", "></main>" },
        { "<section", "></section>" },
        { "<article", "></article>" }
    };

    // JS/TS completions
    private static readonly Dictionary<string, string> JsCompletions = new()
    {
        { "console.l", "og();" },
        { "console.log(", "'');" },
        { "console.e", "rror();" },
        { "console.w", "arn();" },
        { "document.get", "ElementById('');" },
        { "document.query", "Selector('');" },
        { "document.querySelectorAll(", "'');" },
        { "addEvent", "Listener('click', (event) => {\n  \n});" },
        { ".addEvent", "Listener('click', (e) => {\n  \n});" },
        { "window.addEvent", "Listener('DOMContentLoaded', () => {\n  \n});" },
        { "fetch(", "'/api/data')\n  .then(res => res.json())\n  .then(data => console.log(data));" },
        { "async func", "tion name() {\n  \n}" },
        { "function ", "name() {\n  \n}" },
        { "const ", "name = ;" },
        { "export default ", "function() {\n  \n};" },
        { "import ", "{  } from '';" },
        { "JSON.str", "ingify();" },
        { "JSON.par", "se();" },
        { "setTimeout(", "() => {\n  \n}, 1000);" },
        { "setInterval(", "() => {\n  \n}, 1000);" }
    };

    // CSS completions
    private static readonly Dictionary<string, string> CssCompletions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "display: ", "flex;" },
        { "display: f", "lex;" },
        { "display: g", "rid;" },
        { "display: n", "one;" },
        { "flex-dir", "ection: column;" },
        { "justify-", "content: center;" },
        { "align-", "items: center;" },
        { "box-si", "zing: border-box;" },
        { "margin: ", "0 auto;" },
        { "padding: ", "0;" },
        { "position: ", "relative;" },
        { "position: a", "bsolute;" },
        { "position: f", "ixed;" },
        { "font-fa", "mily: system-ui, -apple-system, sans-serif;" },
        { "background-", "color: #1e1e1e;" },
        { "border-rad", "ius: 4px;" },
        { "overflow: ", "hidden;" },
        { "cursor: ", "pointer;" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var lang = context.LanguageId.ToLowerInvariant();
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;

        if (col < 0 || col > line.Length)
        {
            col = line.Length;
        }

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Task.FromResult<string?>(null);
        }

        // Match based on language
        if (lang is "html" or "xml")
        {
            var match = MatchDictionarySuffix(prefix, HtmlCompletions);
            if (match != null) return Task.FromResult<string?>(match);
        }
        else if (lang is "javascript" or "typescript")
        {
            var match = MatchDictionarySuffix(prefix, JsCompletions);
            if (match != null) return Task.FromResult<string?>(match);
        }
        else if (lang is "css")
        {
            var match = MatchDictionarySuffix(prefix, CssCompletions);
            if (match != null) return Task.FromResult<string?>(match);
        }

        // Also fallback to word completion from document if typing an identifier
        var lastWordMatch = Regex.Match(prefix, @"\b([a-zA-Z_][a-zA-Z0-9_]{2,})$");
        if (lastWordMatch.Success)
        {
            var wordPrefix = lastWordMatch.Value;
            var docWords = ExtractUniqueWords(context.FullText);
            var candidate = docWords.FirstOrDefault(w => w.StartsWith(wordPrefix, StringComparison.OrdinalIgnoreCase) && w.Length > wordPrefix.Length);
            if (candidate != null)
            {
                return Task.FromResult<string?>(candidate[wordPrefix.Length..]);
            }
        }

        return Task.FromResult<string?>(null);
    }

    private static string? MatchDictionarySuffix(string prefix, Dictionary<string, string> dict)
    {
        // Find if the end of prefix ends with any key or key prefix
        foreach (var (key, completion) in dict)
        {
            if (prefix.EndsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return completion;
            }

            // Also check partial keys (e.g. key is "<button", prefix ends with "<butt")
            for (var len = key.Length - 1; len >= 3; len--)
            {
                var subKey = key[..len];
                if (prefix.EndsWith(subKey, StringComparison.OrdinalIgnoreCase))
                {
                    var remainder = key[len..] + completion;
                    return remainder;
                }
            }
        }
        return null;
    }

    private static IEnumerable<string> ExtractUniqueWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var matches = Regex.Matches(text, @"\b[a-zA-Z_][a-zA-Z0-9_]{2,}\b");
        return matches.Select(m => m.Value).Distinct().OrderByDescending(w => w.Length);
    }
}

