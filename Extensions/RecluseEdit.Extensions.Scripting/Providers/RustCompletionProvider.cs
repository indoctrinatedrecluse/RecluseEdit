using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline ghost-text completions and snippets for Rust,
/// including functions, tests, pattern matching, derive macros, structs, and error handling.
/// </summary>
public class RustCompletionProvider : IInlineCompletionProvider
{
    public string Id => "rust.core.completion";
    public string Name => "Rust Core Idioms & Snippets";
    public IReadOnlyList<string> SupportedLanguages => ["rust"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.Ordinal)
    {
        { "fn mai", "n() -> Result<(), Box<dyn std::error::Error>> {\n\tprintln!(\"Hello, World!\");\n\tOk(())\n}" },
        { "fn new", "() -> Self {\n\tSelf {}\n}" },
        { "#[der", "ive(Debug, Clone, PartialEq)]" },
        { "#[tes", "t]\nfn test_example() {\n\tassert_eq!(2 + 2, 4);\n}" },
        { "match ", "result {\n\tOk(val) => val,\n\tErr(err) => return Err(err.into()),\n}" },
        { "if let Some(", "val) = opt {\n\t\n}" },
        { "if let Ok(", "val) = res {\n\t\n}" },
        { "while let Some(", "item) = iter.next() {\n\t\n}" },
        { "struct ", "Item {\n\tpub id: u64,\n\tpub name: String,\n}" },
        { "enum ", "Status {\n\tPending,\n\tSuccess(String),\n\tFailed(String),\n}" },
        { "impl ", "Item {\n\tpub fn new(id: u64, name: String) -> Self {\n\t\tSelf { id, name }\n\t}\n}" },
        { "println!", "(\"{}\", value);" },
        { "format!", "(\"{}\", value)" },
        { "vec!", "[1, 2, 3];" },
        { "let mut ", "items = Vec::new();" },
        { "for ", "item in items.iter() {\n\t\n}" },
        { "use std::col", "lections::HashMap;" },
        { "use std::sy", "nc::Arc;" },
        { "use std::fs", "::File;" }
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
