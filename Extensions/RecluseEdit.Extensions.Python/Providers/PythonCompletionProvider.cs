using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Python.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for core Python language constructs,
/// functions, classes, list comprehensions, exception handling, and typing idioms.
/// </summary>
public class PythonCompletionProvider : IInlineCompletionProvider
{
    public string Id => "python.inline.completion";
    public string Name => "Python Core Language & Idiom Completions";

    public IReadOnlyList<string> SupportedLanguages => ["python"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Function and Class Declarations
        {
            "def ",
            "function_name(args):\n    \"\"\"Docstring\"\"\"\n    pass"
        },
        {
            "async def ",
            "handler_name(args):\n    \"\"\"Asynchronous handler\"\"\"\n    pass"
        },
        {
            "class ",
            "ClassName:\n    def __init__(self):\n        pass"
        },

        // Entry point & Context Managers
        {
            "if __name__",
            " == \"__main__\":\n    main()"
        },
        {
            "with open(",
            "\"filename.txt\", \"r\", encoding=\"utf-8\") as f:\n    content = f.read()"
        },

        // Control Flow & Exception Handling
        {
            "try:",
            "\n    pass\nexcept Exception as e:\n    print(f\"Error: {e}\")"
        },
        {
            "match ",
            "status:\n    case 200:\n        return \"OK\"\n    case _:\n        return \"Unknown\""
        },

        // Decorators
        {
            "@property",
            "\ndef property_name(self):\n    return self._property_name"
        },
        {
            "@staticmethod",
            "\ndef static_method():\n    pass"
        },
        {
            "@classmethod",
            "\ndef class_method(cls):\n    pass"
        },

        // Comprehensions & Lambdas
        { "[x for ", "x in items if condition]" },
        { "{k: ", "v for k, v in items.items()}" },
        { "lambda ", "x: x * 2" },

        // Imports & Typing
        { "from typing import ", "List, Dict, Optional, Any, Tuple, Union, Callable" },
        { "from dataclasses import ", "dataclass, field" },
        { "import ", "sys, os, json" }
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

