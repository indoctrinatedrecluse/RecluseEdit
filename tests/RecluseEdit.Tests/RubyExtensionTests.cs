using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Ruby;
using RecluseEdit.Extensions.Ruby.Providers;
using RecluseEdit.Extensions.Ruby.Syntaxes;
using RecluseEdit.Extensions.Ruby.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class RubyExtensionTests
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
    public void TestRubyMetadata()
    {
        var ext = new RubyExtension();
        Assert.AreEqual("recluse.ruby", ext.Id);
        Assert.AreEqual("Ruby & Ruby on Rails Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestRubyRegistration()
    {
        var ext = new RubyExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "ruby" && l.Extensions.Contains(".rb")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "erb" && l.Extensions.Contains(".erb")));

        // Verify Ruby Highlighting Registered
        Assert.IsTrue(host.Syntaxes.ContainsKey("ruby"));
        Assert.AreEqual("Ruby", host.Syntaxes["ruby"].Name);

        // Verify Inline Completion Providers (Core + Rails)
        Assert.HasCount(2, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "ruby.inline.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "rails.inline.completion"));

        // Verify Toolchain Checks (Ruby + Bundler + Rails)
        Assert.HasCount(3, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "ruby"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "bundle"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "rails"));
    }

    [TestMethod]
    public void TestRubySyntaxDefinition()
    {
        var def = RubySyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Ruby", def.Name);
        Assert.IsNotNull(def.MainRuleSet);
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Keywords"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Symbol"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Variable"));
    }

    [TestMethod]
    public async Task TestRubyCompletions_CoreAndRails()
    {
        var rubyProvider = new RubyCompletionProvider();
        var railsProvider = new RailsCompletionProvider();

        // 1. Test Ruby def trigger
        var defCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "  def ",
            CurrentLineText = "  def ",
            CaretOffset = "  def ".Length,
            LineNumber = 1,
            ColumnNumber = "  def ".Length + 1,
            LanguageId = "ruby",
            FullText = "  def "
        };
        var defSuggestion = await rubyProvider.GetInlineSuggestionAsync(defCtx);
        Assert.IsNotNull(defSuggestion);
        Assert.Contains("method_name(args)", defSuggestion);
        Assert.Contains("end", defSuggestion);

        // 2. Test Ruby attr_accessor trigger
        var attrCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "attr_accessor ",
            CurrentLineText = "attr_accessor ",
            CaretOffset = "attr_accessor ".Length,
            LineNumber = 1,
            ColumnNumber = "attr_accessor ".Length + 1,
            LanguageId = "ruby",
            FullText = "attr_accessor "
        };
        var attrSuggestion = await rubyProvider.GetInlineSuggestionAsync(attrCtx);
        Assert.AreEqual(":attribute_name", attrSuggestion);

        // 3. Test Rails has_many trigger
        var hasManyCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "  has_many ",
            CurrentLineText = "  has_many ",
            CaretOffset = "  has_many ".Length,
            LineNumber = 1,
            ColumnNumber = "  has_many ".Length + 1,
            LanguageId = "ruby",
            FullText = "  has_many "
        };
        var hasManySuggestion = await railsProvider.GetInlineSuggestionAsync(hasManyCtx);
        Assert.AreEqual(":items, dependent: :destroy", hasManySuggestion);

        // 4. Test ERB tag trigger
        var erbCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "<h1><%= ",
            CurrentLineText = "<h1><%= ",
            CaretOffset = "<h1><%= ".Length,
            LineNumber = 1,
            ColumnNumber = "<h1><%= ".Length + 1,
            LanguageId = "erb",
            FullText = "<h1><%= "
        };
        var erbSuggestion = await railsProvider.GetInlineSuggestionAsync(erbCtx);
        Assert.AreEqual("expression %>", erbSuggestion);
    }

    [TestMethod]
    public async Task TestRubyToolchainChecks_ExecuteSafely()
    {
        var rubyCheck = new RubyToolchainCheck();
        var rubyReport = await rubyCheck.CheckAsync();
        Assert.AreEqual("Ruby", rubyReport.ToolName);
        Assert.AreEqual("ruby", rubyReport.Command);
        Assert.IsTrue(rubyReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var bundlerCheck = new BundlerToolchainCheck();
        var bundlerReport = await bundlerCheck.CheckAsync();
        Assert.AreEqual("Bundler", bundlerReport.ToolName);
        Assert.AreEqual("bundle", bundlerReport.Command);
        Assert.IsTrue(bundlerReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var railsCheck = new RailsToolchainCheck();
        var railsReport = await railsCheck.CheckAsync();
        Assert.AreEqual("Ruby on Rails", railsReport.ToolName);
        Assert.AreEqual("rails", railsReport.Command);
        Assert.IsTrue(railsReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
