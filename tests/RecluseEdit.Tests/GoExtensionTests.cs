using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Go;
using RecluseEdit.Extensions.Go.Providers;
using RecluseEdit.Extensions.Go.Syntaxes;
using RecluseEdit.Extensions.Go.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class GoExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public Dictionary<string, IHighlightingDefinition> Syntaxes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) => Syntaxes[languageId] = definition;
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    [TestMethod]
    public void TestGoMetadata()
    {
        var ext = new GoExtension();
        Assert.AreEqual("recluse.go", ext.Id);
        Assert.AreEqual("Go Backend & Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "Go");
    }

    [TestMethod]
    public async Task TestGoRegistration()
    {
        var ext = new GoExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "go" && l.Extensions.Contains(".go")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "gomod" && l.Extensions.Contains("go.mod")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "gotemplate" && l.Extensions.Contains(".gotmpl")));

        // Verify Syntax Highlighting Definitions
        Assert.IsTrue(host.Syntaxes.ContainsKey("go"));
        Assert.AreEqual("Go", host.Syntaxes["go"].Name);
        Assert.IsTrue(host.Syntaxes.ContainsKey("gomod"));
        Assert.AreEqual("GoMod", host.Syntaxes["gomod"].Name);

        // Verify Inline Providers (Core, Backend, Mod)
        Assert.HasCount(3, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "go.core.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "go.backend.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "go.mod.completion"));

        // Verify Toolchain Checks (Go Compiler, golangci-lint)
        Assert.HasCount(2, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "go"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "golangci-lint"));
    }

    [TestMethod]
    public void TestGoSyntaxDefinition()
    {
        var def = GoSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Go", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "ControlFlow");
        CollectionAssert.Contains(colors, "Types");
        CollectionAssert.Contains(colors, "Builtins");
        CollectionAssert.Contains(colors, "Constants");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Rune");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "StructTagKey");
        CollectionAssert.Contains(colors, "StructTagVal");
    }

    [TestMethod]
    public void TestGoModSyntaxDefinition()
    {
        var def = GoModSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("GoMod", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "Modifiers");
        CollectionAssert.Contains(colors, "Versions");
        CollectionAssert.Contains(colors, "Packages");
        CollectionAssert.Contains(colors, "Comment");
    }

    [TestMethod]
    public async Task TestGoCompletionProviders()
    {
        // 1. Core Provider
        var core = new GoCompletionProvider();
        Assert.IsTrue(core.SupportedLanguages.Contains("go"));

        var ctxMain = new InlineCompletionContext
        {
            CurrentLineText = "    func mai",
            TextBeforeCaret = "    func mai",
            CaretOffset = 12,
            LineNumber = 1,
            ColumnNumber = 12,
            LanguageId = "go",
            FullText = "    func mai"
        };
        var resMain = await core.GetInlineSuggestionAsync(ctxMain);
        Assert.IsNotNull(resMain);
        StringAssert.Contains(resMain, "fmt.Println");

        var ctxErr = new InlineCompletionContext
        {
            CurrentLineText = "    if err !=",
            TextBeforeCaret = "    if err !=",
            CaretOffset = 13,
            LineNumber = 2,
            ColumnNumber = 13,
            LanguageId = "go",
            FullText = "    if err !="
        };
        var resErr = await core.GetInlineSuggestionAsync(ctxErr);
        Assert.IsNotNull(resErr);
        StringAssert.Contains(resErr, "fmt.Errorf");

        var ctxGoroutine = new InlineCompletionContext
        {
            CurrentLineText = "\tgo func",
            TextBeforeCaret = "\tgo func",
            CaretOffset = 8,
            LineNumber = 3,
            ColumnNumber = 8,
            LanguageId = "go",
            FullText = "\tgo func"
        };
        var resGoroutine = await core.GetInlineSuggestionAsync(ctxGoroutine);
        Assert.IsNotNull(resGoroutine);
        StringAssert.Contains(resGoroutine, "()");

        // 2. Backend Provider
        var backend = new GoBackendCompletionProvider();
        Assert.IsTrue(backend.SupportedLanguages.Contains("go"));

        var ctxGin = new InlineCompletionContext
        {
            CurrentLineText = "    r := gin.D",
            TextBeforeCaret = "    r := gin.D",
            CaretOffset = 14,
            LineNumber = 1,
            ColumnNumber = 14,
            LanguageId = "go",
            FullText = "    r := gin.D"
        };
        var resGin = await backend.GetInlineSuggestionAsync(ctxGin);
        Assert.IsNotNull(resGin);
        StringAssert.Contains(resGin, "efault()");
        StringAssert.Contains(resGin, "r.Run");

        var ctxFiber = new InlineCompletionContext
        {
            CurrentLineText = "    app := fiber.N",
            TextBeforeCaret = "    app := fiber.N",
            CaretOffset = 18,
            LineNumber = 1,
            ColumnNumber = 18,
            LanguageId = "go",
            FullText = "    app := fiber.N"
        };
        var resFiber = await backend.GetInlineSuggestionAsync(ctxFiber);
        Assert.IsNotNull(resFiber);
        StringAssert.Contains(resFiber, "ew()");

        var ctxGorm = new InlineCompletionContext
        {
            CurrentLineText = "    db, err := gorm.O",
            TextBeforeCaret = "    db, err := gorm.O",
            CaretOffset = 21,
            LineNumber = 1,
            ColumnNumber = 21,
            LanguageId = "go",
            FullText = "    db, err := gorm.O"
        };
        var resGorm = await backend.GetInlineSuggestionAsync(ctxGorm);
        Assert.IsNotNull(resGorm);
        StringAssert.Contains(resGorm, "pen(postgres.Open(dsn)");

        // 3. Module Provider
        var mod = new GoModCompletionProvider();
        Assert.IsTrue(mod.SupportedLanguages.Contains("gomod"));

        var ctxMod = new InlineCompletionContext
        {
            CurrentLineText = "module ",
            TextBeforeCaret = "module ",
            CaretOffset = 7,
            LineNumber = 1,
            ColumnNumber = 7,
            LanguageId = "gomod",
            FullText = "module "
        };
        var resMod = await mod.GetInlineSuggestionAsync(ctxMod);
        Assert.IsNotNull(resMod);
        StringAssert.Contains(resMod, "require");
    }

    [TestMethod]
    public async Task TestGoToolchains()
    {
        var compilerCheck = new GoCompilerToolchainCheck();
        Assert.AreEqual("Go Compiler", compilerCheck.ToolName);
        Assert.AreEqual("go", compilerCheck.Command);

        var compilerReport = await compilerCheck.CheckAsync();
        Assert.IsNotNull(compilerReport);
        Assert.IsTrue(compilerReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
        if (compilerReport.Status == ToolchainStatus.Available)
        {
            Assert.IsTrue(compilerReport.DetectedVersion?.StartsWith("go") ?? false);
        }

        var lintCheck = new GolangciLintToolchainCheck();
        Assert.AreEqual("golangci-lint", lintCheck.ToolName);
        Assert.AreEqual("golangci-lint", lintCheck.Command);

        var lintReport = await lintCheck.CheckAsync();
        Assert.IsNotNull(lintReport);
        Assert.IsTrue(lintReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
