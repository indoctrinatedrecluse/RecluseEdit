using RecluseEdit.Extensions.RestClient.Providers;
using RecluseEdit.Extensions.RestClient.Syntaxes;
using RecluseEdit.Extensions.RestClient.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.RestClient;

public class RestClientExtension : IExtension
{
    public string Id => "recluse.restclient";
    public string Name => "REST Client & API Workbench Pack";
    public string Version => "1.0.0";
    public string Description => "Provides .http/.rest syntax highlighting, REST request runner, JSON response formatter, and API workbench.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken ct = default)
    {
        // 1. Register Language
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "http",
            DisplayName = "HTTP / REST",
            Extensions = [".http", ".rest"],
            HighlightingName = "HTTP"
        });

        // 2. Register Syntax Highlighting
        host.RegisterSyntaxHighlighting("http", HttpSyntaxDefinition.CreateDefinition());

        // 3. Register Inline Completion
        host.RegisterInlineCompletion(new HttpCompletionProvider());

        // 4. Register Toolchain Check
        host.RegisterToolchainCheck(new CurlToolchainCheck());

        // 5. Register Side Panel
        host.RegisterSidePanel(new RestClientSidePanelProvider());

        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
