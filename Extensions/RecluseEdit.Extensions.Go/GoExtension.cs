using RecluseEdit.Extensions.Go.Providers;
using RecluseEdit.Extensions.Go.Syntaxes;
using RecluseEdit.Extensions.Go.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Go;

/// <summary>
/// RecluseEdit extension providing language support, syntax highlighting, completions, and compiler checks
/// for modern Go development, backend web frameworks (Gin, Fiber, Chi, Echo, net/http), GORM, and go.mod modules.
/// </summary>
public class GoExtension : IExtension
{
    public string Id => "recluse.go";
    public string Name => "Go Backend & Language Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive Go backend and language support featuring syntax highlighting for Go and go.mod, completions for Gin, Fiber, Chi, Echo, GORM, net/http, and Go toolchain checks.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "go",
            DisplayName = "Go",
            Extensions = [".go"],
            HighlightingName = "Go"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "gomod",
            DisplayName = "Go Module",
            Extensions = ["go.mod", "go.work", "go.sum", ".mod", ".work"],
            HighlightingName = "GoMod"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "gotemplate",
            DisplayName = "Go Template",
            Extensions = [".gotmpl", ".gohtml"],
            HighlightingName = "HTML"
        });

        // 2. Register Custom Syntax Highlighting Definitions
        try
        {
            var goDef = GoSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("go", goDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Go syntax definition: {ex.Message}");
        }

        try
        {
            var modDef = GoModSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("gomod", modDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register GoMod syntax definition: {ex.Message}");
        }

        // 3. Register Inline Autocomplete & Snippet Providers
        host.RegisterInlineCompletion(new GoCompletionProvider());
        host.RegisterInlineCompletion(new GoBackendCompletionProvider());
        host.RegisterInlineCompletion(new GoModCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new GoCompilerToolchainCheck());
        host.RegisterToolchainCheck(new GolangciLintToolchainCheck());

        host.Log("Go Backend & Language Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
