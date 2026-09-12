using RecluseEdit.Extensions.Database.Providers;
using RecluseEdit.Extensions.Database.Syntaxes;
using RecluseEdit.Extensions.Database.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Database;

public class DatabaseExtension : IExtension
{
    public string Id => "recluse.database";
    public string Name => "Database & SQL Explorer Pack";
    public string Version => "1.0.0";
    public string Description => "Provides SQL syntax highlighting, completions, SQLite/database query runner, and schema explorer.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken ct = default)
    {
        // 1. Language definition
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "sql",
            DisplayName = "SQL",
            Extensions = [".sql"],
            HighlightingName = "SQL"
        });

        // 2. Syntax highlighting
        host.RegisterSyntaxHighlighting("sql", SqlSyntaxDefinition.CreateDefinition());

        // 3. Completions
        host.RegisterInlineCompletion(new SqlCompletionProvider());

        // 4. Toolchain checks
        host.RegisterToolchainCheck(new SqliteToolchainCheck());
        host.RegisterToolchainCheck(new PostgresToolchainCheck());
        host.RegisterToolchainCheck(new MySqlToolchainCheck());

        // 5. Side Panel
        host.RegisterSidePanel(new DatabaseSidePanelProvider());

        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
