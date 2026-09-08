using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Angular;
using RecluseEdit.Extensions.Angular.Providers;
using RecluseEdit.Extensions.Angular.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class AngularExtensionTests
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
    public void TestAngularMetadata()
    {
        var ext = new AngularExtension();
        Assert.AreEqual("recluse.angular", ext.Id);
        Assert.AreEqual("Angular Language & Framework Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestAngularRegistration()
    {
        var ext = new AngularExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "angular-html" && l.Extensions.Contains(".component.html")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "angular-ts" && l.Extensions.Contains(".component.ts")));

        // Verify Inline Completion Providers
        Assert.HasCount(1, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "angular.inline.completion"));

        // Verify Toolchain Checks
        Assert.HasCount(1, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "ng"));
    }

    [TestMethod]
    public async Task TestAngularCompletions_SignalsAndControlFlow()
    {
        var provider = new AngularCompletionProvider();

        // Test Signal trigger
        var signalCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "  count = signal(",
            CurrentLineText = "  count = signal(",
            CaretOffset = "  count = signal(".Length,
            LineNumber = 1,
            ColumnNumber = "  count = signal(".Length + 1,
            LanguageId = "angular-ts",
            FullText = "  count = signal("
        };
        var signalSuggestion = await provider.GetInlineSuggestionAsync(signalCtx);
        Assert.AreEqual("initialValue);", signalSuggestion);

        // Test @if control flow trigger
        var ifCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "@if (",
            CurrentLineText = "@if (",
            CaretOffset = "@if (".Length,
            LineNumber = 1,
            ColumnNumber = "@if (".Length + 1,
            LanguageId = "angular-html",
            FullText = "@if ("
        };
        var ifSuggestion = await provider.GetInlineSuggestionAsync(ifCtx);
        Assert.IsNotNull(ifSuggestion);
        Assert.Contains("@else", ifSuggestion);

        // Test @for control flow trigger
        var forCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "@for (",
            CurrentLineText = "@for (",
            CaretOffset = "@for (".Length,
            LineNumber = 1,
            ColumnNumber = "@for (".Length + 1,
            LanguageId = "angular-html",
            FullText = "@for ("
        };
        var forSuggestion = await provider.GetInlineSuggestionAsync(forCtx);
        Assert.IsNotNull(forSuggestion);
        Assert.Contains("track item.id", forSuggestion);
    }

    [TestMethod]
    public async Task TestAngularToolchainCheck_ExecutesSafely()
    {
        var check = new AngularCliToolchainCheck();
        var report = await check.CheckAsync();

        Assert.AreEqual("Angular CLI (ng)", report.ToolName);
        Assert.AreEqual("ng", report.Command);
        Assert.IsTrue(report.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
        Assert.IsFalse(string.IsNullOrEmpty(report.Description));
    }
}
