using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Database.Providers;

/// <summary>
/// Provides inline ghost-text completions for SQL statements, clauses, DDL, and transactions.
/// </summary>
public class SqlCompletionProvider : IInlineCompletionProvider
{
    public string Id => "database.sql.inline";
    public string Name => "SQL Query & DDL Templates";

    public IReadOnlyList<string> SupportedLanguages => ["sql"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SELECT * FROM ", "table_name WHERE condition;" },
        { "SELECT ", "* FROM " },
        { "INSERT INTO ", "table_name (column1, column2) VALUES ('value1', 'value2');" },
        { "UPDATE ", "table_name SET column1 = 'value1' WHERE condition;" },
        { "DELETE FROM ", "table_name WHERE condition;" },
        { "CREATE TABLE IF NOT EXISTS ", "items (\n    id INTEGER PRIMARY KEY AUTOINCREMENT,\n    name VARCHAR(255) NOT NULL,\n    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP\n);" },
        { "CREATE TABLE ", "items (\n    id INTEGER PRIMARY KEY AUTOINCREMENT,\n    name VARCHAR(255) NOT NULL\n);" },
        { "ALTER TABLE ", "table_name ADD COLUMN new_col VARCHAR(255);" },
        { "INNER JOIN ", "other_table ON primary_table.id = other_table.foreign_id" },
        { "LEFT JOIN ", "other_table ON primary_table.id = other_table.foreign_id" },
        { "CREATE INDEX ", "idx_items_name ON items (name);" },
        { "BEGIN TRANSACTION", ";\n\n-- Statements\n\nCOMMIT;" },
        { "ORDER BY ", "id DESC" },
        { "GROUP BY ", "category HAVING COUNT(*) > 1" }
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
