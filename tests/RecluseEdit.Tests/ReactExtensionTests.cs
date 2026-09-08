using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.React;
using RecluseEdit.Extensions.React.Providers;
using RecluseEdit.Extensions.React.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class ReactExtensionTests
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
    public void TestExtensionMetadata()
    {
        var ext = new ReactExtension();
        Assert.AreEqual("recluse.react", ext.Id);
        Assert.AreEqual("React, Redux & GraphQL Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestExtensionRegistration()
    {
        var ext = new ReactExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "jsx" && l.Extensions.Contains(".jsx")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "tsx" && l.Extensions.Contains(".tsx")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "graphql" && l.Extensions.Contains(".graphql")));

        // Verify Inline Completion Providers
        Assert.HasCount(3, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "react.inline.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "redux.inline.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "graphql.inline.completion"));

        // Verify Toolchain Checks
        Assert.HasCount(3, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.ToolName == "Node.js"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.ToolName == "npm"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.ToolName.Contains("TypeScript")));
    }

    [TestMethod]
    public async Task TestNodeJsToolchainCheck()
    {
        var check = new NodeJsToolchainCheck();
        var report = await check.CheckAsync();

        Assert.AreEqual("Node.js", report.ToolName);
        Assert.AreEqual(ToolchainStatus.Available, report.Status);
        Assert.IsNotNull(report.DetectedVersion);
        StringAssert.StartsWith(report.DetectedVersion, "v24");
    }

    [TestMethod]
    public async Task TestNpmToolchainCheck()
    {
        var check = new NpmToolchainCheck();
        var report = await check.CheckAsync();

        Assert.AreEqual("npm", report.ToolName);
        Assert.AreEqual(ToolchainStatus.Available, report.Status);
        Assert.IsNotNull(report.DetectedVersion);
    }

    [TestMethod]
    public async Task TestTypeScriptToolchainCheck()
    {
        var check = new TypeScriptToolchainCheck();
        var report = await check.CheckAsync();

        Assert.IsNotNull(report);
        Assert.Contains("TypeScript", report.ToolName);
        // Warning or Available depending on global install
        Assert.IsTrue(report.Status is ToolchainStatus.Warning or ToolchainStatus.Available);
    }

    [TestMethod]
    public async Task TestReactCompletions()
    {
        var provider = new ReactCompletionProvider();
        var ctx = new InlineCompletionContext
        {
            TextBeforeCaret = "  useStat",
            CurrentLineText = "  useStat",
            CaretOffset = 9,
            LineNumber = 1,
            ColumnNumber = 10,
            LanguageId = "tsx",
            FullText = "  useStat"
        };

        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "<string>");
    }

    [TestMethod]
    public async Task TestReduxCompletions()
    {
        var provider = new ReduxCompletionProvider();
        var ctx = new InlineCompletionContext
        {
            TextBeforeCaret = "  createSl",
            CurrentLineText = "  createSl",
            CaretOffset = 10,
            LineNumber = 1,
            ColumnNumber = 11,
            LanguageId = "tsx",
            FullText = "  createSl"
        };

        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "ice({");
    }

    [TestMethod]
    public async Task TestGraphQlCompletions()
    {
        var provider = new GraphQlCompletionProvider();
        var ctx = new InlineCompletionContext
        {
            TextBeforeCaret = "query ",
            CurrentLineText = "query ",
            CaretOffset = 6,
            LineNumber = 1,
            ColumnNumber = 7,
            LanguageId = "graphql",
            FullText = "query "
        };

        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "GetItem");
    }
}
