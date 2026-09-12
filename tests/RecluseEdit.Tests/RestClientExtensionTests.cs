using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.RestClient;
using RecluseEdit.Extensions.RestClient.Providers;
using RecluseEdit.Extensions.RestClient.Services;
using RecluseEdit.Extensions.RestClient.Syntaxes;
using RecluseEdit.Extensions.RestClient.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class RestClientExtensionTests
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
    public void TestRestClientMetadata()
    {
        var ext = new RestClientExtension();
        Assert.AreEqual("recluse.restclient", ext.Id);
        Assert.AreEqual("REST Client & API Workbench Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "REST", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task TestRestClientRegistration()
    {
        var ext = new RestClientExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Language
        Assert.IsTrue(host.Languages.Any(l => l.Id == "http" && l.Extensions.Contains(".http") && l.Extensions.Contains(".rest")));

        // Syntax
        Assert.IsTrue(host.Syntaxes.ContainsKey("http"));
        Assert.AreEqual("HTTP", host.Syntaxes["http"].Name);

        // Inline completion
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "restclient.http.inline"));

        // Toolchain
        Assert.HasCount(1, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "curl"));

        // Side panel
        Assert.IsTrue(host.SidePanels.Any(p => p.Id == "recluse.restclient"));
    }

    [TestMethod]
    public void TestHttpSyntaxDefinition()
    {
        var def = HttpSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("HTTP", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Method");
        CollectionAssert.Contains(colors, "Boundary");
        CollectionAssert.Contains(colors, "Variable");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "HeaderName");
        CollectionAssert.Contains(colors, "Url");
    }

    [TestMethod]
    public async Task TestHttpCompletionProvider()
    {
        var provider = new HttpCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("http"));

        var ctxHeader = new InlineCompletionContext
        {
            CurrentLineText = "Content-Type: ",
            TextBeforeCaret = "Content-Type: ",
            CaretOffset = 14,
            LineNumber = 1,
            ColumnNumber = 15,
            LanguageId = "http",
            FullText = "Content-Type: "
        };
        var resHeader = await provider.GetInlineSuggestionAsync(ctxHeader);
        Assert.IsNotNull(resHeader);
        Assert.AreEqual("application/json", resHeader);

        var ctxAuth = new InlineCompletionContext
        {
            CurrentLineText = "Authorization: ",
            TextBeforeCaret = "Authorization: ",
            CaretOffset = 15,
            LineNumber = 1,
            ColumnNumber = 16,
            LanguageId = "http",
            FullText = "Authorization: "
        };
        var resAuth = await provider.GetInlineSuggestionAsync(ctxAuth);
        Assert.IsNotNull(resAuth);
        StringAssert.Contains(resAuth, "Bearer");

        var ctxMethod = new InlineCompletionContext
        {
            CurrentLineText = "POST ",
            TextBeforeCaret = "POST ",
            CaretOffset = 5,
            LineNumber = 1,
            ColumnNumber = 6,
            LanguageId = "http",
            FullText = "POST "
        };
        var resMethod = await provider.GetInlineSuggestionAsync(ctxMethod);
        Assert.IsNotNull(resMethod);
        StringAssert.Contains(resMethod, "Content-Type: application/json");
    }

    [TestMethod]
    public void TestHttpRequestParser()
    {
        var rawHttp = """
            # Sample request
            POST https://api.example.com/v1/users HTTP/1.1
            Content-Type: application/json
            Authorization: Bearer secret_token

            {
              "name": "Jane Doe",
              "email": "jane@example.com"
            }
            """;

        var req = HttpRequestEngine.ParseRawRequest(rawHttp);
        Assert.AreEqual("POST", req.Method);
        Assert.AreEqual("https://api.example.com/v1/users", req.Url);
        Assert.HasCount(2, req.Headers);
        Assert.AreEqual("application/json", req.Headers["Content-Type"]);
        Assert.AreEqual("Bearer secret_token", req.Headers["Authorization"]);
        Assert.IsNotNull(req.Body);
        StringAssert.Contains(req.Body, "Jane Doe");
    }

    [TestMethod]
    public void TestJsonFormatting()
    {
        var minifiedJson = "{\"id\":1,\"name\":\"test\",\"active\":true}";
        var formatted = HttpRequestEngine.FormatBody(minifiedJson, "application/json");

        StringAssert.Contains(formatted, "\n");
        StringAssert.Contains(formatted, "  \"id\": 1");
        StringAssert.Contains(formatted, "  \"name\": \"test\"");
    }

    [TestMethod]
    public async Task TestCurlToolchainCheck()
    {
        var curlCheck = new CurlToolchainCheck();
        Assert.AreEqual("curl", curlCheck.Command);
        var report = await curlCheck.CheckAsync();
        Assert.IsNotNull(report);
        Assert.IsTrue(report.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
