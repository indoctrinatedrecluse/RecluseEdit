using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Database;
using RecluseEdit.Extensions.Database.Providers;
using RecluseEdit.Extensions.Database.Syntaxes;
using RecluseEdit.Extensions.Database.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class DatabaseExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public List<ISidePanelProvider> SidePanels { get; } = [];
        public Dictionary<string, IHighlightingDefinition> Syntaxes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) => Syntaxes[languageId] = definition;
        public void RegisterSidePanel(ISidePanelProvider panelProvider) => SidePanels.Add(panelProvider);
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    [TestMethod]
    public void TestDatabaseMetadata()
    {
        var ext = new DatabaseExtension();
        Assert.AreEqual("recluse.database", ext.Id);
        Assert.AreEqual("Database & SQL Explorer Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "SQL");
    }

    [TestMethod]
    public async Task TestDatabaseRegistration()
    {
        var ext = new DatabaseExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Language
        Assert.IsTrue(host.Languages.Any(l => l.Id == "sql" && l.Extensions.Contains(".sql")));

        // Syntax definition
        Assert.IsTrue(host.Syntaxes.ContainsKey("sql"));
        Assert.AreEqual("SQL", host.Syntaxes["sql"].Name);

        // Completion provider
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "database.sql.inline"));

        // Toolchains
        Assert.HasCount(3, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "sqlite3"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "psql"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "mysql"));

        // Side panel
        Assert.IsTrue(host.SidePanels.Any(p => p.Id == "recluse.database"));
    }

    [TestMethod]
    public void TestSqlSyntaxDefinition()
    {
        var def = SqlSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("SQL", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "Types");
        CollectionAssert.Contains(colors, "Functions");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Digits");
    }

    [TestMethod]
    public async Task TestSqlCompletionProvider()
    {
        var provider = new SqlCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("sql"));

        var ctxSelect = new InlineCompletionContext
        {
            CurrentLineText = "SELECT * FROM ",
            TextBeforeCaret = "SELECT * FROM ",
            CaretOffset = 14,
            LineNumber = 1,
            ColumnNumber = 15,
            LanguageId = "sql",
            FullText = "SELECT * FROM "
        };
        var resSelect = await provider.GetInlineSuggestionAsync(ctxSelect);
        Assert.IsNotNull(resSelect);
        StringAssert.Contains(resSelect, "WHERE");

        var ctxInsert = new InlineCompletionContext
        {
            CurrentLineText = "INSERT INTO ",
            TextBeforeCaret = "INSERT INTO ",
            CaretOffset = 12,
            LineNumber = 1,
            ColumnNumber = 13,
            LanguageId = "sql",
            FullText = "INSERT INTO "
        };
        var resInsert = await provider.GetInlineSuggestionAsync(ctxInsert);
        Assert.IsNotNull(resInsert);
        StringAssert.Contains(resInsert, "VALUES");

        var ctxTable = new InlineCompletionContext
        {
            CurrentLineText = "CREATE TABLE IF NOT EXISTS ",
            TextBeforeCaret = "CREATE TABLE IF NOT EXISTS ",
            CaretOffset = 27,
            LineNumber = 1,
            ColumnNumber = 28,
            LanguageId = "sql",
            FullText = "CREATE TABLE IF NOT EXISTS "
        };
        var resTable = await provider.GetInlineSuggestionAsync(ctxTable);
        Assert.IsNotNull(resTable);
        StringAssert.Contains(resTable, "PRIMARY KEY");
    }

    [TestMethod]
    public async Task TestDatabaseToolchains()
    {
        var sqliteCheck = new SqliteToolchainCheck();
        Assert.AreEqual("sqlite3", sqliteCheck.Command);
        var sqliteReport = await sqliteCheck.CheckAsync();
        Assert.IsNotNull(sqliteReport);
        Assert.IsTrue(sqliteReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var psqlCheck = new PostgresToolchainCheck();
        Assert.AreEqual("psql", psqlCheck.Command);
        var psqlReport = await psqlCheck.CheckAsync();
        Assert.IsNotNull(psqlReport);

        var mysqlCheck = new MySqlToolchainCheck();
        Assert.AreEqual("mysql", mysqlCheck.Command);
        var mysqlReport = await mysqlCheck.CheckAsync();
        Assert.IsNotNull(mysqlReport);
    }
}
