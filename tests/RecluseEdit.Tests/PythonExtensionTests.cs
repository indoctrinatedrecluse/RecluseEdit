using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Python;
using RecluseEdit.Extensions.Python.Providers;
using RecluseEdit.Extensions.Python.Syntaxes;
using RecluseEdit.Extensions.Python.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class PythonExtensionTests
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
    public void TestPythonMetadata()
    {
        var ext = new PythonExtension();
        Assert.AreEqual("recluse.python", ext.Id);
        Assert.AreEqual("Python & Full-Stack Web Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestPythonRegistration()
    {
        var ext = new PythonExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "python" && l.Extensions.Contains(".py")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "jinja" && l.Extensions.Contains(".jinja")));

        // Verify Syntaxes
        Assert.IsTrue(host.Syntaxes.ContainsKey("python"));
        Assert.AreEqual("Python", host.Syntaxes["python"].Name);
        Assert.IsTrue(host.Syntaxes.ContainsKey("jinja"));
        Assert.AreEqual("Jinja", host.Syntaxes["jinja"].Name);

        // Verify Inline Providers (4 providers: Core, Flask/Django/FastAPI, Python Frontends, Express)
        Assert.HasCount(4, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "python.inline.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "python.flask.django.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "python.frontends.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "express.inline.completion"));

        // Verify Toolchain Checks (Python, Pip, Django CLI)
        Assert.HasCount(3, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "python"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "pip"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "django-admin"));
    }

    [TestMethod]
    public void TestPythonSyntaxDefinition()
    {
        var def = PythonSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Python", def.Name);
        Assert.IsNotNull(def.MainRuleSet);
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Keywords"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "ControlFlow"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Decorator"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Functions"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Types"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Dunder"));
    }

    [TestMethod]
    public void TestJinjaSyntaxDefinition()
    {
        var def = JinjaSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Jinja", def.Name);
        Assert.IsNotNull(def.MainRuleSet);
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Keywords"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Filter"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Variable"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "HtmlTag"));
    }

    [TestMethod]
    public async Task TestPythonCompletions_AllProviders()
    {
        var pyProvider = new PythonCompletionProvider();
        var webProvider = new FlaskDjangoCompletionProvider();
        var uiProvider = new PythonFrontendCompletionProvider();
        var expProvider = new ExpressCompletionProvider();

        // 1. Python core completion
        var pyCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "def ",
            CurrentLineText = "def ",
            CaretOffset = "def ".Length,
            LineNumber = 1,
            ColumnNumber = "def ".Length + 1,
            LanguageId = "python",
            FullText = "def "
        };
        var pySuggestion = await pyProvider.GetInlineSuggestionAsync(pyCtx);
        Assert.IsNotNull(pySuggestion);
        Assert.Contains("function_name(args):", pySuggestion);

        // 2. Flask route completion
        var flaskCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "@app.route(",
            CurrentLineText = "@app.route(",
            CaretOffset = "@app.route(".Length,
            LineNumber = 1,
            ColumnNumber = "@app.route(".Length + 1,
            LanguageId = "python",
            FullText = "@app.route("
        };
        var flaskSuggestion = await webProvider.GetInlineSuggestionAsync(flaskCtx);
        Assert.IsNotNull(flaskSuggestion);
        Assert.Contains("methods=[\"GET\", \"POST\"]", flaskSuggestion);

        // 3. Jinja block completion
        var jinjaCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "{% block ",
            CurrentLineText = "{% block ",
            CaretOffset = "{% block ".Length,
            LineNumber = 1,
            ColumnNumber = "{% block ".Length + 1,
            LanguageId = "jinja",
            FullText = "{% block "
        };
        var jinjaSuggestion = await webProvider.GetInlineSuggestionAsync(jinjaCtx);
        Assert.AreEqual("content %}\n{% endblock %}", jinjaSuggestion);

        // 4. Streamlit completion
        var stCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "st.title(",
            CurrentLineText = "st.title(",
            CaretOffset = "st.title(".Length,
            LineNumber = 1,
            ColumnNumber = "st.title(".Length + 1,
            LanguageId = "python",
            FullText = "st.title("
        };
        var stSuggestion = await uiProvider.GetInlineSuggestionAsync(stCtx);
        Assert.AreEqual("\"Application Title\")", stSuggestion);

        // 5. Gradio completion
        var grCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "gr.Interface(",
            CurrentLineText = "gr.Interface(",
            CaretOffset = "gr.Interface(".Length,
            LineNumber = 1,
            ColumnNumber = "gr.Interface(".Length + 1,
            LanguageId = "python",
            FullText = "gr.Interface("
        };
        var grSuggestion = await uiProvider.GetInlineSuggestionAsync(grCtx);
        Assert.IsNotNull(grSuggestion);
        Assert.Contains("outputs=\"text\").launch()", grSuggestion);

        // 6. Express completion
        var expCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "const express = ",
            CurrentLineText = "const express = ",
            CaretOffset = "const express = ".Length,
            LineNumber = 1,
            ColumnNumber = "const express = ".Length + 1,
            LanguageId = "javascript",
            FullText = "const express = "
        };
        var expSuggestion = await expProvider.GetInlineSuggestionAsync(expCtx);
        Assert.IsNotNull(expSuggestion);
        Assert.Contains("require('express');", expSuggestion);
    }

    [TestMethod]
    public async Task TestPythonToolchainChecks_ExecuteSafely()
    {
        var pyCheck = new PythonToolchainCheck();
        var pyReport = await pyCheck.CheckAsync();
        Assert.AreEqual("Python", pyReport.ToolName);
        Assert.AreEqual("python", pyReport.Command);
        Assert.IsTrue(pyReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var pipCheck = new PipToolchainCheck();
        var pipReport = await pipCheck.CheckAsync();
        Assert.AreEqual("Pip", pipReport.ToolName);
        Assert.AreEqual("pip", pipReport.Command);
        Assert.IsTrue(pipReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var djangoCheck = new DjangoToolchainCheck();
        var djangoReport = await djangoCheck.CheckAsync();
        Assert.AreEqual("Django CLI", djangoReport.ToolName);
        Assert.AreEqual("django-admin", djangoReport.Command);
        Assert.IsTrue(djangoReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
