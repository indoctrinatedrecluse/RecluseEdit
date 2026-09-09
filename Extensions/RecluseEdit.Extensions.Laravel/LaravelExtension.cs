using RecluseEdit.Extensions.Laravel.Providers;
using RecluseEdit.Extensions.Laravel.Syntaxes;
using RecluseEdit.Extensions.Laravel.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Laravel;

/// <summary>
/// RecluseEdit extension providing language support, syntax highlighting, completions, and compiler checks
/// for modern Laravel development, Blade templates, Eloquent ORM, and Artisan CLI.
/// </summary>
public class LaravelExtension : IExtension
{
    public string Id => "recluse.laravel";
    public string Name => "Laravel Framework & Blade Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive Laravel framework support featuring Blade template highlighting, Eloquent model & relationship completions, Artisan & routing snippets, and toolchain checks.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "blade",
            DisplayName = "Laravel Blade Template",
            Extensions = [".blade.php"],
            HighlightingName = "Blade"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "laravel",
            DisplayName = "Laravel Artisan",
            Extensions = ["artisan"],
            HighlightingName = "PHP"
        });

        // 2. Register Custom Syntax Highlighting Definitions
        try
        {
            var bladeDef = BladeSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("blade", bladeDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Blade syntax definition: {ex.Message}");
        }

        // 3. Register Inline Autocomplete & Snippet Providers
        host.RegisterInlineCompletion(new BladeCompletionProvider());
        host.RegisterInlineCompletion(new EloquentCompletionProvider());
        host.RegisterInlineCompletion(new LaravelRouteCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new LaravelCliToolchainCheck());
        host.RegisterToolchainCheck(new ArtisanToolchainCheck());

        host.Log("Laravel Framework & Blade Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
