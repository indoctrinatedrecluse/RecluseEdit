using RecluseEdit.Extensions.React.Providers;
using RecluseEdit.Extensions.React.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.React;

/// <summary>
/// RecluseEdit extension providing language support, snippets, completions, and compiler checks
/// for the React, Redux, and GraphQL web ecosystems.
/// </summary>
public class ReactExtension : IExtension
{
    public string Id => "recluse.react";
    public string Name => "React, Redux & GraphQL Language Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive React JSX/TSX, Redux Toolkit, and GraphQL language support with toolchain checks.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "jsx",
            DisplayName = "React JSX",
            Extensions = [".jsx"],
            HighlightingName = "JavaScript"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "tsx",
            DisplayName = "React TSX",
            Extensions = [".tsx"],
            HighlightingName = "JavaScript"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "graphql",
            DisplayName = "GraphQL",
            Extensions = [".graphql", ".gql"],
            HighlightingName = "JavaScript"
        });

        // 2. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new ReactCompletionProvider());
        host.RegisterInlineCompletion(new ReduxCompletionProvider());
        host.RegisterInlineCompletion(new GraphQlCompletionProvider());

        // 3. Register Toolchain Checks
        host.RegisterToolchainCheck(new NodeJsToolchainCheck());
        host.RegisterToolchainCheck(new NpmToolchainCheck());
        host.RegisterToolchainCheck(new TypeScriptToolchainCheck());

        host.Log("React, Redux & GraphQL Language Pack initialized with compiler checks.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

