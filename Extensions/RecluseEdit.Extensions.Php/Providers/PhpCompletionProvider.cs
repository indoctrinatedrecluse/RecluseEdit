using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Php.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for modern PHP 8+ development,
/// including constructor property promotion, match expressions, enums, arrow functions, and common idioms.
/// </summary>
public class PhpCompletionProvider : IInlineCompletionProvider
{
    public string Id => "php.inline.completion";
    public string Name => "PHP 8+ Language & Snippet Completions";

    public IReadOnlyList<string> SupportedLanguages => ["php", "html"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Opening tags & strict typing
        { "<?php", "\n\ndeclare(strict_types=1);\n" },
        { "<?=", " $variable ?> " },

        // Modern PHP 8+ Constructs
        {
            "match (",
            "$status) {\n    200 => 'OK',\n    400 => 'Bad Request',\n    404 => 'Not Found',\n    default => 'Internal Error',\n};"
        },
        {
            "enum ",
            "Status: string {\n    case Pending = 'pending';\n    case Active = 'active';\n    case Inactive = 'inactive';\n}"
        },
        { "public readonly string $", "id;" },

        // Classes, Constructor Promotion, and Methods
        {
            "public function __construct(",
            "\n    public readonly string $id,\n    public string $name,\n) {\n}"
        },
        { "public function ", "handleRequest(): void {\n    \n}" },
        { "private function ", "validateInput(): bool {\n    return true;\n}" },
        { "protected function ", "processData(): array {\n    return [];\n}" },
        {
            "class ",
            "UserController {\n    public function __construct() {\n        \n    }\n}"
        },
        {
            "interface ",
            "RepositoryInterface {\n    public function findById(string $id): ?object;\n    public function save(object $entity): void;\n}"
        },

        // Exception Handling & Functional Helpers
        {
            "try {",
            "\n    \n} catch (Throwable $e) {\n    error_log($e->getMessage());\n}"
        },
        { "fn(", "$item) => $item->id;" },
        { "array_map(", "fn($item) => $item->name, $items);" },
        { "array_filter(", "fn($item) => $item !== null, $items);" },
        { "json_encode(", "$data, JSON_THROW_ON_ERROR | JSON_PRETTY_PRINT);" },
        { "json_decode(", "$payload, true, 512, JSON_THROW_ON_ERROR);" }
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

