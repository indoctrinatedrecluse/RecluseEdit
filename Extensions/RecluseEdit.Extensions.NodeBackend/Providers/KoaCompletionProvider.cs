using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.NodeBackend.Providers;

/// <summary>
/// Provides inline completions for Koa.js cascading middleware, context object,
/// and router endpoints.
/// </summary>
public class KoaCompletionProvider : IInlineCompletionProvider
{
    public string Id => "nodebackend.koa.inline";
    public string Name => "Koa Cascading Middleware & Context";

    public IReadOnlyList<string> SupportedLanguages => ["javascript", "typescript", "js", "ts"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "const Koa = require('koa');",
            "\nconst Router = require('@koa/router');\n\nconst app = new Koa();\nconst router = new Router();\nconst PORT = process.env.PORT || 3000;"
        },
        {
            "app.use(async (ctx, next) => {",
            "\n  const start = Date.now();\n  await next();\n  const ms = Date.now() - start;\n  ctx.set('X-Response-Time', `${ms}ms`);\n});"
        },
        {
            "router.get('/api/",
            "items', async (ctx) => {\n  ctx.body = { success: true, data: [] };\n});"
        },
        {
            "router.post('/api/",
            "items', async (ctx) => {\n  const payload = ctx.request.body;\n  ctx.status = 201;\n  ctx.body = { success: true, item: payload };\n});"
        },
        {
            "ctx.throw(",
            "404, 'Resource not found');"
        }
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

