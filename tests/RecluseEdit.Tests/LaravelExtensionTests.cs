using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.Laravel;
using RecluseEdit.Extensions.Laravel.Providers;
using RecluseEdit.Extensions.Laravel.Syntaxes;
using RecluseEdit.Extensions.Laravel.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class LaravelExtensionTests
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
    public void TestLaravelMetadata()
    {
        var ext = new LaravelExtension();
        Assert.AreEqual("recluse.laravel", ext.Id);
        Assert.AreEqual("Laravel Framework & Blade Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
    }

    [TestMethod]
    public async Task TestLaravelRegistration()
    {
        var ext = new LaravelExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Verify Languages
        Assert.IsTrue(host.Languages.Any(l => l.Id == "blade" && l.Extensions.Contains(".blade.php")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "laravel" && l.Extensions.Contains("artisan")));

        // Verify Syntax Highlighting Registered
        Assert.IsTrue(host.Syntaxes.ContainsKey("blade"));
        Assert.AreEqual("Blade", host.Syntaxes["blade"].Name);

        // Verify Inline Providers (Blade, Eloquent, Routes)
        Assert.HasCount(3, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "laravel.blade.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "laravel.eloquent.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "laravel.routes.completion"));

        // Verify Toolchain Checks (Laravel Installer, Artisan)
        Assert.HasCount(2, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "laravel"));
        Assert.IsTrue(host.ToolchainChecks.Any(t => t.Command == "php artisan"));
    }

    [TestMethod]
    public void TestBladeSyntaxDefinition()
    {
        var def = BladeSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Blade", def.Name);
        Assert.IsNotNull(def.MainRuleSet);
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Directive"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Comment"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "Variable"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "RawHtml"));
        Assert.IsTrue(def.NamedHighlightingColors.Any(c => c.Name == "HtmlTag"));
    }

    [TestMethod]
    public async Task TestLaravelCompletions_AllProviders()
    {
        var bladeProvider = new BladeCompletionProvider();
        var eloquentProvider = new EloquentCompletionProvider();
        var routeProvider = new LaravelRouteCompletionProvider();

        // 1. Blade directive completion
        var bladeCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "@section(",
            CurrentLineText = "@section(",
            CaretOffset = "@section(".Length,
            LineNumber = 1,
            ColumnNumber = "@section(".Length + 1,
            LanguageId = "blade",
            FullText = "@section("
        };
        var bladeSuggestion = await bladeProvider.GetInlineSuggestionAsync(bladeCtx);
        Assert.IsNotNull(bladeSuggestion);
        Assert.Contains("@endsection", bladeSuggestion);

        // 2. Blade Vite completion
        var viteCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "@vite(",
            CurrentLineText = "@vite(",
            CaretOffset = "@vite(".Length,
            LineNumber = 1,
            ColumnNumber = "@vite(".Length + 1,
            LanguageId = "blade",
            FullText = "@vite("
        };
        var viteSuggestion = await bladeProvider.GetInlineSuggestionAsync(viteCtx);
        Assert.IsNotNull(viteSuggestion);
        Assert.Contains("resources/css/app.css", viteSuggestion);

        // 3. Eloquent relationship completion
        var eloCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "public function user(): BelongsTo",
            CurrentLineText = "public function user(): BelongsTo",
            CaretOffset = "public function user(): BelongsTo".Length,
            LineNumber = 1,
            ColumnNumber = "public function user(): BelongsTo".Length + 1,
            LanguageId = "php",
            FullText = "public function user(): BelongsTo"
        };
        var eloSuggestion = await eloquentProvider.GetInlineSuggestionAsync(eloCtx);
        Assert.IsNotNull(eloSuggestion);
        Assert.Contains("belongsTo(User::class)", eloSuggestion);

        // 4. Eloquent migration schema completion
        var migCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "Schema::create(",
            CurrentLineText = "Schema::create(",
            CaretOffset = "Schema::create(".Length,
            LineNumber = 1,
            ColumnNumber = "Schema::create(".Length + 1,
            LanguageId = "php",
            FullText = "Schema::create("
        };
        var migSuggestion = await eloquentProvider.GetInlineSuggestionAsync(migCtx);
        Assert.IsNotNull(migSuggestion);
        Assert.Contains("Blueprint $table", migSuggestion);
        Assert.Contains("$table->timestamps()", migSuggestion);

        // 5. Laravel route completion
        var routeCtx = new InlineCompletionContext
        {
            TextBeforeCaret = "Route::resource(",
            CurrentLineText = "Route::resource(",
            CaretOffset = "Route::resource(".Length,
            LineNumber = 1,
            ColumnNumber = "Route::resource(".Length + 1,
            LanguageId = "php",
            FullText = "Route::resource("
        };
        var routeSuggestion = await routeProvider.GetInlineSuggestionAsync(routeCtx);
        Assert.IsNotNull(routeSuggestion);
        Assert.Contains("ResourceController::class", routeSuggestion);
    }

    [TestMethod]
    public async Task TestLaravelToolchains_ExecuteSafely()
    {
        var cliCheck = new LaravelCliToolchainCheck();
        var cliReport = await cliCheck.CheckAsync();
        Assert.AreEqual("Laravel Installer", cliReport.ToolName);
        Assert.AreEqual("laravel", cliReport.Command);
        Assert.IsTrue(cliReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var artCheck = new ArtisanToolchainCheck();
        var artReport = await artCheck.CheckAsync();
        Assert.AreEqual("Artisan CLI", artReport.ToolName);
        Assert.AreEqual("php artisan", artReport.Command);
        Assert.IsTrue(artReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
