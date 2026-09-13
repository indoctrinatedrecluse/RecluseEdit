using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.NodeBackend.Providers;

/// <summary>
/// Provides inline completions for Fastify high-performance Node.js framework:
/// instance creation, route handlers, schema validation, lifecycle hooks, and plugin registration.
/// </summary>
public class FastifyCompletionProvider : IInlineCompletionProvider
{
    public string Id => "nodebackend.fastify.inline";
    public string Name => "Fastify High-Performance Framework";

    public IReadOnlyList<string> SupportedLanguages => ["javascript", "typescript", "js", "ts"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "const fastify = require('fastify')(",
            "{ logger: true });"
        },
        {
            "fastify.get('",
            "/api/v1/items', async (request, reply) => {\n  return { success: true, data: [] };\n});"
        },
        {
            "fastify.post('",
            "/api/v1/items', {\n  schema: {\n    body: {\n      type: 'object',\n      required: ['name'],\n      properties: {\n        name: { type: 'string' }\n      }\n    }\n  }\n}, async (request, reply) => {\n  reply.code(201);\n  return { success: true, item: request.body };\n});"
        },
        {
            "fastify.register(",
            "async function (instance, opts) {\n  instance.get('/ping', async () => ({ pong: true }));\n}, { prefix: '/api' });"
        },
        {
            "fastify.addHook('",
            "onRequest', async (request, reply) => {\n  // Pre-handler hook\n});"
        },
        {
            "fastify.listen({",
            " port: 3000, host: '0.0.0.0' }, (err, address) => {\n  if (err) {\n    fastify.log.error(err);\n    process.exit(1);\n  }\n  console.log(`Server listening at ${address}`);\n});"
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

