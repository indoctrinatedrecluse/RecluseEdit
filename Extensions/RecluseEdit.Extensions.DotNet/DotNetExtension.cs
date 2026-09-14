using RecluseEdit.Extensions.DotNet.Providers;
using RecluseEdit.Extensions.DotNet.Syntaxes;
using RecluseEdit.Extensions.DotNet.Themes;
using RecluseEdit.Extensions.DotNet.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.DotNet;

/// <summary>
/// RecluseEdit extension providing language support, syntax highlighting, completions,
/// and toolchains for ASP.NET Core Minimal APIs, Blazor components (.razor), Razor Pages (.cshtml),
/// and Entity Framework Core.
/// </summary>
public class DotNetExtension : IExtension
{
    public string Id => "recluse.dotnet";
    public string Name => "ASP.NET Core & Blazor Web Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive language support, syntax highlighting, completions, and toolchains for ASP.NET Core Minimal APIs, Blazor components, Razor Pages, and EF Core.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "razor",
            DisplayName = "Razor / Blazor Component",
            Extensions = [".razor"],
            HighlightingName = "Razor"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "cshtml",
            DisplayName = "Razor Pages / MVC",
            Extensions = [".cshtml"],
            HighlightingName = "Razor"
        });

        // 2. Register Syntax Definitions
        try
        {
            var razorSyntax = RazorSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("razor", razorSyntax);
            host.RegisterSyntaxHighlighting("cshtml", razorSyntax);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Razor syntax definition: {ex.Message}");
        }

        // 3. Register Inline Completion
        host.RegisterInlineCompletion(new DotNetWebCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new DotNetToolchainCheck());

        // 5. Register Theme
        host.RegisterTheme(new DotNetPurpleTheme());

        host.Log("ASP.NET Core & Blazor Web Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

