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
    public string Description => "Provides .http/.rest & .graphql syntax highlighting, REST request runner, GraphQL & OpenAPI tooling, and API workbench.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken ct = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "http",
            DisplayName = "HTTP / REST",
            Extensions = [".http", ".rest"],
            HighlightingName = "HTTP"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "graphql",
            DisplayName = "GraphQL",
            Extensions = [".graphql", ".gql"],
            HighlightingName = "GraphQL"
        });

        // 2. Register Syntax Highlighting
        host.RegisterSyntaxHighlighting("http", HttpSyntaxDefinition.CreateDefinition());

        try
        {
            host.RegisterSyntaxHighlighting("graphql", GraphQLSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register GraphQL syntax: {ex.Message}");
        }

        // 3. Register Inline Completion
        host.RegisterInlineCompletion(new HttpCompletionProvider());
        host.RegisterInlineCompletion(new GraphQlCompletionProvider());
        host.RegisterInlineCompletion(new OpenApiCompletionProvider());

        // 4. Register Toolchain Check
        host.RegisterToolchainCheck(new CurlToolchainCheck());

        // 5. Register Side Panel
        host.RegisterSidePanel(new RestClientSidePanelProvider());

        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
