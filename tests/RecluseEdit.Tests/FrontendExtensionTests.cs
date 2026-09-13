using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Frontend;
using RecluseEdit.Extensions.Frontend.Providers;
using RecluseEdit.Extensions.Frontend.Syntaxes;
using RecluseEdit.Extensions.Frontend.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class FrontendExtensionTests
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
    public void TestFrontendMetadata()
    {
        var ext = new FrontendExtension();
        Assert.AreEqual("recluse.frontend", ext.Id);
        Assert.AreEqual("Frontend Frameworks & Node Tooling Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "Vue", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task TestFrontendRegistration()
    {
        var ext = new FrontendExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "vue" && l.Extensions.Contains(".vue")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "svelte" && l.Extensions.Contains(".svelte")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "astro" && l.Extensions.Contains(".astro")));

        // Syntaxes
        Assert.IsTrue(host.Syntaxes.ContainsKey("vue"));
        Assert.AreEqual("Vue", host.Syntaxes["vue"].Name);
        Assert.IsTrue(host.Syntaxes.ContainsKey("svelte"));
        Assert.AreEqual("Svelte", host.Syntaxes["svelte"].Name);
        Assert.IsTrue(host.Syntaxes.ContainsKey("astro"));
        Assert.AreEqual("Astro", host.Syntaxes["astro"].Name);

        // Inline providers
        Assert.HasCount(4, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "frontend.vue.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "frontend.svelte.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "frontend.astro.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "frontend.modern.inline"));

        // Toolchain checks
        Assert.HasCount(6, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "vite"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "next"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "astro"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "turbo"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "pnpm"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "bun"));
    }

    [TestMethod]
    public void TestVueSyntaxDefinition()
    {
        var def = VueSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Vue", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "VueDirectives");
        CollectionAssert.Contains(colors, "VueReactivity");
        CollectionAssert.Contains(colors, "Tags");
        CollectionAssert.Contains(colors, "Interpolation");
    }

    [TestMethod]
    public void TestSvelteSyntaxDefinition()
    {
        var def = SvelteSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Svelte", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "SvelteRunes");
        CollectionAssert.Contains(colors, "SvelteBlocks");
        CollectionAssert.Contains(colors, "SvelteBindings");
        CollectionAssert.Contains(colors, "Tags");
    }

    [TestMethod]
    public void TestAstroSyntaxDefinition()
    {
        var def = AstroSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Astro", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "FrontmatterFence");
        CollectionAssert.Contains(colors, "AstroDirectives");
        CollectionAssert.Contains(colors, "AstroGlobals");
        CollectionAssert.Contains(colors, "Tags");
    }

    [TestMethod]
    public async Task TestVueCompletionProvider()
    {
        var provider = new VueCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("vue"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "const count = ref(",
            TextBeforeCaret = "const count = ref(",
            CaretOffset = 18,
            LineNumber = 1,
            ColumnNumber = 19,
            LanguageId = "vue",
            FullText = "const count = ref("
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        Assert.AreEqual("0);", suggestion);
    }

    [TestMethod]
    public async Task TestSvelteCompletionProvider()
    {
        var provider = new SvelteCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("svelte"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "let count = $state(",
            TextBeforeCaret = "let count = $state(",
            CaretOffset = 19,
            LineNumber = 1,
            ColumnNumber = 20,
            LanguageId = "svelte",
            FullText = "let count = $state("
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        Assert.AreEqual("0);", suggestion);
    }

    [TestMethod]
    public async Task TestAstroCompletionProvider()
    {
        var provider = new AstroCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("astro"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "---",
            TextBeforeCaret = "---",
            CaretOffset = 3,
            LineNumber = 1,
            ColumnNumber = 4,
            LanguageId = "astro",
            FullText = "---"
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "Astro.props");
    }

    [TestMethod]
    public async Task TestModernFrameworksCompletionProvider()
    {
        var provider = new ModernFrameworksCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("typescript"));
        Assert.IsTrue(provider.SupportedLanguages.Contains("tsx"));

        // SolidJS
        var ctxSolid = new InlineCompletionContext
        {
            CurrentLineText = "const [count, setCount] = createSignal(",
            TextBeforeCaret = "const [count, setCount] = createSignal(",
            CaretOffset = 39,
            LineNumber = 1,
            ColumnNumber = 40,
            LanguageId = "tsx",
            FullText = "const [count, setCount] = createSignal("
        };
        var resSolid = await provider.GetInlineSuggestionAsync(ctxSolid);
        Assert.IsNotNull(resSolid);
        Assert.AreEqual("0);", resSolid);

        // Next.js App Router Page
        var ctxNext = new InlineCompletionContext
        {
            CurrentLineText = "export default function Page(",
            TextBeforeCaret = "export default function Page(",
            CaretOffset = 29,
            LineNumber = 1,
            ColumnNumber = 30,
            LanguageId = "tsx",
            FullText = "export default function Page("
        };
        var resNext = await provider.GetInlineSuggestionAsync(ctxNext);
        Assert.IsNotNull(resNext);
        StringAssert.Contains(resNext, "Next.js Page");

        // Vite Config
        var ctxVite = new InlineCompletionContext
        {
            CurrentLineText = "export default defineConfig({",
            TextBeforeCaret = "export default defineConfig({",
            CaretOffset = 29,
            LineNumber = 1,
            ColumnNumber = 30,
            LanguageId = "typescript",
            FullText = "export default defineConfig({"
        };
        var resVite = await provider.GetInlineSuggestionAsync(ctxVite);
        Assert.IsNotNull(resVite);
        StringAssert.Contains(resVite, "plugins: []");
    }

    [TestMethod]
    public async Task TestFrontendToolchains()
    {
        var viteCheck = new ViteToolchainCheck();
        Assert.AreEqual("vite", viteCheck.Command);
        var viteReport = await viteCheck.CheckAsync();
        Assert.IsNotNull(viteReport);

        var nextCheck = new NextToolchainCheck();
        Assert.AreEqual("next", nextCheck.Command);
        var nextReport = await nextCheck.CheckAsync();
        Assert.IsNotNull(nextReport);

        var astroCheck = new AstroToolchainCheck();
        Assert.AreEqual("astro", astroCheck.Command);
        var astroReport = await astroCheck.CheckAsync();
        Assert.IsNotNull(astroReport);

        var turboCheck = new TurboToolchainCheck();
        Assert.AreEqual("turbo", turboCheck.Command);
        var turboReport = await turboCheck.CheckAsync();
        Assert.IsNotNull(turboReport);

        var pnpmCheck = new PnpmToolchainCheck();
        Assert.AreEqual("pnpm", pnpmCheck.Command);
        var pnpmReport = await pnpmCheck.CheckAsync();
        Assert.IsNotNull(pnpmReport);

        var bunCheck = new BunToolchainCheck();
        Assert.AreEqual("bun", bunCheck.Command);
        var bunReport = await bunCheck.CheckAsync();
        Assert.IsNotNull(bunReport);
    }
}

