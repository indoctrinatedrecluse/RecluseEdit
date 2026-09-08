using RecluseEdit.Extensions.Flutter.Providers;
using RecluseEdit.Extensions.Flutter.Syntaxes;
using RecluseEdit.Extensions.Flutter.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Flutter;

/// <summary>
/// RecluseEdit extension providing language support, snippets, completions, and compiler checks
/// for Flutter and Dart applications.
/// </summary>
public class FlutterExtension : IExtension
{
    public string Id => "recluse.flutter";
    public string Name => "Flutter & Dart Language Pack";
    public string Version => "1.0.0";
    public string Description => "First-class Flutter and Dart development support with widget snippets, Dart 3 syntax highlighting, and Flutter/Dart SDK compiler verification.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Dart Language Definition
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "dart",
            DisplayName = "Dart / Flutter",
            Extensions = [".dart"],
            HighlightingName = "Dart"
        });

        // 2. Register Dart XSHD Syntax Highlighting Definition
        try
        {
            var dartDef = DartSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("dart", dartDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Dart syntax definition: {ex.Message}");
        }

        // 3. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new FlutterCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new FlutterToolchainCheck());
        host.RegisterToolchainCheck(new DartToolchainCheck());

        host.Log("Flutter & Dart Language Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
