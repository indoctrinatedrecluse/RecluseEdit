using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Flutter;
using RecluseEdit.Extensions.Flutter.Providers;
using RecluseEdit.Extensions.Flutter.Syntaxes;
using RecluseEdit.Extensions.Flutter.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class FlutterExtensionTests
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
    public void TestFlutterMetadata()
    {
        var ext = new FlutterExtension();
        Assert.AreEqual("recluse.flutter", ext.Id);
        Assert.AreEqual("Flutter & Dart Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestFlutterRegistration()
    {
        var ext = new FlutterExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "dart" && l.Extensions.Contains(".dart")));

        // Verify Dart Highlighting Registered
        Assert.IsTrue(host.Syntaxes.ContainsKey("dart"));
        Assert.AreEqual("Dart", host.Syntaxes["dart"].Name);

        // Verify Inline Completion Providers
        Assert.HasCount(1, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "flutter.inline.completion"));

        // Verify Toolchain Checks (Flutter + Dart)
        Assert.HasCount(2, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "flutter"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "dart"));
    }

    [TestMethod]
    public void TestDartSyntaxDefinition()
    {
        var def = DartSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Dart", def.Name);
        Assert.IsNotNull(def.MainRuleSet);
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Keywords"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Types"));
    }

    [TestMethod]
    public async Task TestFlutterCompletions_WidgetsAndState()
    {
        var provider = new FlutterCompletionProvider();

        // Test stless trigger
        var stlessCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "stless",
            CurrentLineText = "stless",
            CaretOffset = "stless".Length,
            LineNumber = 1,
            ColumnNumber = "stless".Length + 1,
            LanguageId = "dart",
            FullText = "stless"
        };
        var stlessSuggestion = await provider.GetInlineSuggestionAsync(stlessCtx);
        Assert.IsNotNull(stlessSuggestion);
        Assert.Contains("StatelessWidget", stlessSuggestion);

        // Test setState trigger
        var setStateCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "    setState(",
            CurrentLineText = "    setState(",
            CaretOffset = "    setState(".Length,
            LineNumber = 1,
            ColumnNumber = "    setState(".Length + 1,
            LanguageId = "dart",
            FullText = "    setState("
        };
        var setStateSuggestion = await provider.GetInlineSuggestionAsync(setStateCtx);
        Assert.AreEqual("() {\n  \n});", setStateSuggestion);

        // Test Scaffold trigger
        var scaffoldCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "return Scaffold(",
            CurrentLineText = "return Scaffold(",
            CaretOffset = "return Scaffold(".Length,
            LineNumber = 1,
            ColumnNumber = "return Scaffold(".Length + 1,
            LanguageId = "dart",
            FullText = "return Scaffold("
        };
        var scaffoldSuggestion = await provider.GetInlineSuggestionAsync(scaffoldCtx);
        Assert.IsNotNull(scaffoldSuggestion);
        Assert.Contains("AppBar", scaffoldSuggestion);
    }

    [TestMethod]
    public async Task TestFlutterToolchainChecks_ExecuteSafely()
    {
        var flutterCheck = new FlutterToolchainCheck();
        var flutterReport = await flutterCheck.CheckAsync();
        Assert.AreEqual("Flutter SDK", flutterReport.ToolName);
        Assert.AreEqual("flutter", flutterReport.Command);
        Assert.IsTrue(flutterReport.Status is ToolchainStatus.Available or ToolchainStatus.Missing or ToolchainStatus.Warning);

        var dartCheck = new DartToolchainCheck();
        var dartReport = await dartCheck.CheckAsync();
        Assert.AreEqual("Dart SDK", dartReport.ToolName);
        Assert.AreEqual("dart", dartReport.Command);
        Assert.IsTrue(dartReport.Status is ToolchainStatus.Available or ToolchainStatus.Missing or ToolchainStatus.Warning);
    }
}
