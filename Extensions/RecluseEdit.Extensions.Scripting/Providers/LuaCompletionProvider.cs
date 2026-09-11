using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline ghost-text completions and snippets for Lua,
/// including local functions, pairs/ipairs loops, modules, metatables, and error handling.
/// </summary>
public class LuaCompletionProvider : IInlineCompletionProvider
{
    public string Id => "lua.core.completion";
    public string Name => "Lua Core Idioms & Snippets";
    public IReadOnlyList<string> SupportedLanguages => ["lua"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.Ordinal)
    {
        { "local function ", "name(param)\n\t\nend" },
        { "for k, v in pair", "s(tbl) do\n\tprint(k, v)\nend" },
        { "for i, v in ipair", "s(tbl) do\n\tprint(i, v)\nend" },
        { "for i = 1,", " #items do\n\t\nend" },
        { "if not ", "success then\n\terror(\"Operation failed\")\nend" },
        { "local M = ", "{}\n\nfunction M.new()\n\tlocal self = setmetatable({}, { __index = M })\n\treturn self\nend\n\nreturn M" },
        { "setmetatable(", "instance, { __index = Class })" },
        { "local ok, res = pca", "ll(function()\n\treturn do_something()\nend)" },
        { "table.inse", "rt(tbl, value)" },
        { "table.conc", "at(tbl, \", \")" },
        { "string.form", "at(\"%s: %d\", label, count)" }
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
