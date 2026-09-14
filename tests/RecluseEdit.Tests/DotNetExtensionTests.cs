using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.DotNet;
using RecluseEdit.Extensions.DotNet.Providers;
using RecluseEdit.Extensions.DotNet.Syntaxes;
using RecluseEdit.Extensions.DotNet.Themes;
using RecluseEdit.Extensions.DotNet.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class DotNetExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public Dictionary<string, IHighlightingDefinition> Syntaxes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<IThemeDefinition> Themes { get; } = [];
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) => Syntaxes[languageId] = definition;
        public void RegisterTheme(IThemeDefinition theme) => Themes.Add(theme);
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    private static InlineCompletionContext CreateContext(string lineText, string lang) => new()
    {
        CurrentLineText = lineText,
        TextBeforeCaret = lineText,
        CaretOffset = lineText.Length,
        LineNumber = 1,
        ColumnNumber = lineText.Length,
        LanguageId = lang,
        FullText = lineText
    };

    [TestMethod]
    public void TestDotNetExtensionMetadata()
    {
        var ext = new DotNetExtension();
        Assert.AreEqual("recluse.dotnet", ext.Id);
        Assert.AreEqual("ASP.NET Core & Blazor Web Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "ASP.NET Core");
        StringAssert.Contains(ext.Description, "Blazor");
    }

    [TestMethod]
    public async Task TestDotNetExtensionRegistration()
    {
        var ext = new DotNetExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "razor" && l.Extensions.Contains(".razor")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "cshtml" && l.Extensions.Contains(".cshtml")));

        // Verify Syntaxes
        Assert.IsTrue(host.Syntaxes.ContainsKey("razor"));
        Assert.IsTrue(host.Syntaxes.ContainsKey("cshtml"));
        Assert.AreEqual("Razor", host.Syntaxes["razor"].Name);

        // Verify Completion Provider
        Assert.IsTrue(host.InlineProviders.Any(p => p is DotNetWebCompletionProvider));

        // Verify Toolchain Check
        Assert.IsTrue(host.ToolchainChecks.Any(c => c is DotNetToolchainCheck));

        // Verify Theme
        Assert.IsTrue(host.Themes.Any(t => t is DotNetPurpleTheme));
    }

    [TestMethod]
    public void TestRazorSyntaxDefinitionValid()
    {
        var def = RazorSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Razor", def.Name);
        Assert.IsTrue(def.MainRuleSet.Rules.Count > 0 || def.MainRuleSet.Spans.Count > 0);
    }

    [TestMethod]
    public async Task TestDotNetWebCompletionProvider()
    {
        var provider = new DotNetWebCompletionProvider();
        Assert.AreEqual("dotnet.web.inline", provider.Id);
        CollectionAssert.Contains(provider.SupportedLanguages.ToList(), "csharp");
        CollectionAssert.Contains(provider.SupportedLanguages.ToList(), "razor");
        CollectionAssert.Contains(provider.SupportedLanguages.ToList(), "cshtml");

        // Minimal APIs
        var mapGet = await provider.GetInlineSuggestionAsync(CreateContext("app.MapGet(\"", "csharp"));
        Assert.IsNotNull(mapGet);
        StringAssert.Contains(mapGet, "Results.Ok");

        // Blazor Component
        var page = await provider.GetInlineSuggestionAsync(CreateContext("@page \"", "razor"));
        Assert.IsNotNull(page);
        StringAssert.Contains(page, "InteractiveServer");

        var code = await provider.GetInlineSuggestionAsync(CreateContext("@code {", "razor"));
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "[Parameter]");

        // EF Core
        var db = await provider.GetInlineSuggestionAsync(CreateContext("public class AppDbContext : DbContext", "csharp"));
        Assert.IsNotNull(db);
        StringAssert.Contains(db, "DbSet");

        // Empty context
        var empty = await provider.GetInlineSuggestionAsync(CreateContext("   ", "csharp"));
        Assert.IsNull(empty);
    }

    [TestMethod]
    public async Task TestDotNetToolchainCheck()
    {
        var check = new DotNetToolchainCheck();
        Assert.AreEqual(".NET SDK & CLI (dotnet)", check.ToolName);
        Assert.AreEqual("dotnet", check.Command);

        var report = await check.CheckAsync(CancellationToken.None);
        Assert.IsNotNull(report);
        // On dev machine running dotnet test, dotnet should be available
        Assert.AreEqual(ToolchainStatus.Available, report.Status);
        Assert.IsNotNull(report.DetectedVersion);
    }

    [TestMethod]
    public void TestDotNetPurpleTheme()
    {
        var theme = new DotNetPurpleTheme();
        Assert.AreEqual("dotnet.purple-dark", theme.Id);
        Assert.AreEqual(".NET Purple Dark", theme.DisplayName);
        Assert.AreEqual(ThemeType.Dark, theme.Type);
        Assert.AreEqual("#12101F", theme.Colors.BgPrimary);
        Assert.AreEqual("#512BD4", theme.Colors.BgActive);
    }
}

