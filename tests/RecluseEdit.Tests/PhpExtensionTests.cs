using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Php;
using RecluseEdit.Extensions.Php.Providers;
using RecluseEdit.Extensions.Php.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class PhpExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) { }
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    [TestMethod]
    public void TestPhpMetadata()
    {
        var ext = new PhpExtension();
        Assert.AreEqual("recluse.php", ext.Id);
        Assert.AreEqual("PHP Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestPhpRegistration()
    {
        var ext = new PhpExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "php" && l.Extensions.Contains(".php")));

        // Verify Inline Completion Providers
        Assert.HasCount(1, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "php.inline.completion"));

        // Verify Toolchain Checks (PHP CLI + Composer)
        Assert.HasCount(2, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "php"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "composer"));
    }

    [TestMethod]
    public async Task TestPhpCompletions_MatchAndConstructorPromotion()
    {
        var provider = new PhpCompletionProvider();

        // Test <?php trigger
        var phpTagCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "<?php",
            CurrentLineText = "<?php",
            CaretOffset = "<?php".Length,
            LineNumber = 1,
            ColumnNumber = "<?php".Length + 1,
            LanguageId = "php",
            FullText = "<?php"
        };
        var phpTagSuggestion = await provider.GetInlineSuggestionAsync(phpTagCtx);
        Assert.IsNotNull(phpTagSuggestion);
        Assert.Contains("declare(strict_types=1);", phpTagSuggestion);

        // Test match ( trigger
        var matchCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "$res = match (",
            CurrentLineText = "$res = match (",
            CaretOffset = "$res = match (".Length,
            LineNumber = 1,
            ColumnNumber = "$res = match (".Length + 1,
            LanguageId = "php",
            FullText = "$res = match ("
        };
        var matchSuggestion = await provider.GetInlineSuggestionAsync(matchCtx);
        Assert.IsNotNull(matchSuggestion);
        Assert.Contains("default =>", matchSuggestion);

        // Test constructor promotion trigger
        var ctorCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "public function __construct(",
            CurrentLineText = "public function __construct(",
            CaretOffset = "public function __construct(".Length,
            LineNumber = 1,
            ColumnNumber = "public function __construct(".Length + 1,
            LanguageId = "php",
            FullText = "public function __construct("
        };
        var ctorSuggestion = await provider.GetInlineSuggestionAsync(ctorCtx);
        Assert.IsNotNull(ctorSuggestion);
        Assert.Contains("public readonly string $id", ctorSuggestion);
    }

    [TestMethod]
    public async Task TestPhpToolchainChecks_ExecuteSafely()
    {
        var phpCheck = new PhpToolchainCheck();
        var phpReport = await phpCheck.CheckAsync();
        Assert.AreEqual("PHP CLI", phpReport.ToolName);
        Assert.AreEqual("php", phpReport.Command);
        Assert.IsTrue(phpReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var composerCheck = new ComposerToolchainCheck();
        var composerReport = await composerCheck.CheckAsync();
        Assert.AreEqual("Composer", composerReport.ToolName);
        Assert.AreEqual("composer", composerReport.Command);
        Assert.IsTrue(composerReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
