using RecluseEdit.Extensions.Frontend.Providers;
using RecluseEdit.Extensions.Frontend.Syntaxes;
using RecluseEdit.Extensions.Frontend.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Frontend;

/// <summary>
/// RecluseEdit extension providing language support, syntaxes, completions, and compiler checks
/// for Vue 3 SFC, Svelte 5, Astro, SolidJS, Next.js App Router, Remix, and Node bundlers (Vite, Webpack, Turbopack, Rollup).
/// </summary>
public class FrontendExtension : IExtension
{
    public string Id => "recluse.frontend";
    public string Name => "Frontend Frameworks & Node Tooling Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive Vue 3, Svelte 5, Astro, SolidJS, Next.js, Remix, and Node compiler/bundler integration.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "vue",
            DisplayName = "Vue 3 Single File Component",
            Extensions = [".vue"],
            HighlightingName = "Vue"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "svelte",
            DisplayName = "Svelte Component",
            Extensions = [".svelte"],
            HighlightingName = "Svelte"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "astro",
            DisplayName = "Astro Component",
            Extensions = [".astro"],
            HighlightingName = "Astro"
        });

        // 2. Register Custom Syntax Highlighting Definitions
        try
        {
            host.RegisterSyntaxHighlighting("vue", VueSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Vue syntax: {ex.Message}");
        }

        try
        {
            host.RegisterSyntaxHighlighting("svelte", SvelteSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Svelte syntax: {ex.Message}");
        }

        try
        {
            host.RegisterSyntaxHighlighting("astro", AstroSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Astro syntax: {ex.Message}");
        }

        // 3. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new VueCompletionProvider());
        host.RegisterInlineCompletion(new SvelteCompletionProvider());
        host.RegisterInlineCompletion(new AstroCompletionProvider());
        host.RegisterInlineCompletion(new ModernFrameworksCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new ViteToolchainCheck());
        host.RegisterToolchainCheck(new NextToolchainCheck());
        host.RegisterToolchainCheck(new AstroToolchainCheck());
        host.RegisterToolchainCheck(new TurboToolchainCheck());
        host.RegisterToolchainCheck(new PnpmToolchainCheck());
        host.RegisterToolchainCheck(new BunToolchainCheck());

        host.Log("Frontend Frameworks & Node Tooling Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

