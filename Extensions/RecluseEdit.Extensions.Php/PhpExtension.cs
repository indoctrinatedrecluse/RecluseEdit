using RecluseEdit.Extensions.Php.Providers;
using RecluseEdit.Extensions.Php.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Php;

/// <summary>
/// RecluseEdit extension providing language support, snippets, completions, and compiler checks
/// for modern PHP 8+ and Composer workflows.
/// </summary>
public class PhpExtension : IExtension
{
    public string Id => "recluse.php";
    public string Name => "PHP Language Pack";
    public string Version => "1.0.0";
    public string Description => "Modern PHP 8+ language support featuring match expressions, constructor property promotion, enums, typed properties, and PHP CLI/Composer toolchain verification.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Language
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "php",
            DisplayName = "PHP",
            Extensions = [".php", ".phtml", ".php3", ".php4", ".php5", ".php8"],
            HighlightingName = "PHP"
        });

        // 2. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new PhpCompletionProvider());

        // 3. Register Toolchain Checks
        host.RegisterToolchainCheck(new PhpToolchainCheck());
        host.RegisterToolchainCheck(new ComposerToolchainCheck());

        host.Log("PHP Language Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

