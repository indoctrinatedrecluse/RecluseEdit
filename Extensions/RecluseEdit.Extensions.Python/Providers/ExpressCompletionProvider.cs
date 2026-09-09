using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Python.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for Express.js full-stack web applications,
/// routing, REST APIs, and middleware.
/// </summary>
public class ExpressCompletionProvider : IInlineCompletionProvider
{
    public string Id => "express.inline.completion";
    public string Name => "Express.js REST & Middleware Completions";

    public IReadOnlyList<string> SupportedLanguages => ["javascript", "typescript", "js", "ts"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Initialization & Middleware
        {
            "const express = ",
            "require('express');\nconst app = express();\nconst PORT = process.env.PORT || 3000;"
        },
        {
            "app.use(express.json(",
            "));\napp.use(express.urlencoded({ extended: true }));"
        },
        {
            "const router = ",
            "express.Router();"
        },

        // REST Endpoints
        {
            "app.get(",
            "'/api/v1/resource', (req, res) => {\n  res.json({ success: true, data: [] });\n});"
        },
        {
            "app.post(",
            "'/api/v1/resource', (req, res) => {\n  const payload = req.body;\n  res.status(201).json({ success: true, item: payload });\n});"
        },
        {
            "app.put(",
            "'/api/v1/resource/:id', (req, res) => {\n  const { id } = req.params;\n  res.json({ id, ...req.body });\n});"
        },
        {
            "app.delete(",
            "'/api/v1/resource/:id', (req, res) => {\n  const { id } = req.params;\n  res.status(204).send();\n});"
        },

        // Router endpoints
        {
            "router.get(",
            "('/', (req, res) => {\n  res.json({ status: 'ok' });\n});"
        },
        {
            "router.post(",
            "('/', (req, res) => {\n  res.status(201).json(req.body);\n});"
        },

        // Error handling & Server listen
        {
            "app.use((err, req, res, next) => ",
            "{\n  console.error(err.stack);\n  res.status(500).json({ error: err.message });\n});"
        },
        {
            "app.listen(",
            "PORT, () => {\n  console.log(`Server listening on port ${PORT}`);\n});"
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
