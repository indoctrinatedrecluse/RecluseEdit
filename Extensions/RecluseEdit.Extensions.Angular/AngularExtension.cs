using RecluseEdit.Extensions.Angular.Providers;
using RecluseEdit.Extensions.Angular.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Angular;

/// <summary>
/// RecluseEdit extension providing language support, snippets, completions, and compiler checks
/// for the modern Angular ecosystem (Signals, control flow syntax, standalone components).
/// </summary>
public class AngularExtension : IExtension
{
    public string Id => "recluse.angular";
    public string Name => "Angular Language & Framework Pack";
    public string Version => "1.0.0";
    public string Description => "Modern Angular support featuring Signals, control flow (@if, @for, @switch), standalone components, and Angular CLI toolchain integration.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "angular-html",
            DisplayName = "Angular HTML Template",
            Extensions = [".component.html"],
            HighlightingName = "HTML"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "angular-ts",
            DisplayName = "Angular TypeScript",
            Extensions = [".component.ts", ".service.ts", ".directive.ts", ".pipe.ts", ".guard.ts"],
            HighlightingName = "JavaScript"
        });

        // 2. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new AngularCompletionProvider());

        // 3. Register Toolchain Checks
        host.RegisterToolchainCheck(new AngularCliToolchainCheck());

        host.Log("Angular Language & Framework Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
