using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.RestClient.Providers;

/// <summary>
/// Provides inline completions for OpenAPI 3.0 / 3.1 & Swagger specifications in YAML and JSON formats.
/// </summary>
public class OpenApiCompletionProvider : IInlineCompletionProvider
{
    public string Id => "restclient.openapi.inline";
    public string Name => "OpenAPI & Swagger Schema Templates";

    public IReadOnlyList<string> SupportedLanguages => ["yaml", "yml", "json"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Root Specs
        { "openapi: ", "3.1.0\ninfo:\n  title: Sample API\n  version: 1.0.0\n  description: API specification created with RecluseEdit\npaths: {}" },
        { "\"openapi\": ", "\"3.1.0\",\n\"info\": {\n  \"title\": \"Sample API\",\n  \"version\": \"1.0.0\"\n}," },

        // Path definitions
        { "paths:\n  /", "users:\n    get:\n      summary: Get all users\n      responses:\n        '200':\n          description: Success\n          content:\n            application/json:\n              schema:\n                type: array\n                items:\n                  $ref: '#/components/schemas/User'" },
        { "get:\n", "      summary: Retrieve resource\n      operationId: getResource\n      responses:\n        '200':\n          description: OK\n          content:\n            application/json:\n              schema:\n                type: object" },
        { "post:\n", "      summary: Create resource\n      operationId: createResource\n      requestBody:\n        required: true\n        content:\n          application/json:\n            schema:\n              $ref: '#/components/schemas/CreateRequest'\n      responses:\n        '201':\n          description: Created" },

        // Components & Schemas
        { "components:\n", "  schemas:\n    User:\n      type: object\n      required:\n        - id\n        - name\n      properties:\n        id:\n          type: string\n          format: uuid\n        name:\n          type: string\n        email:\n          type: string\n          format: email" },
        { "parameters:\n", "  - name: id\n    in: path\n    required: true\n    schema:\n      type: string" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var prefix = !string.IsNullOrEmpty(context.TextBeforeCaret)
            ? context.TextBeforeCaret
            : context.CurrentLineText;

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
