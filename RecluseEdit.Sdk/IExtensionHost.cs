using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Sdk;

/// <summary>
/// Host context supplied to an extension upon initialization to register capabilities.
/// </summary>
public interface IExtensionHost
{
    void RegisterLanguage(LanguageDefinition language);
    void RegisterInlineCompletion(IInlineCompletionProvider provider);
    void RegisterIntelliSense(IIntelliSenseProvider provider);
    void RegisterToolchainCheck(IToolchainCheck toolchainCheck);
    void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition);
    IReadOnlyList<LanguageDefinition> GetRegisteredLanguages();
    void Log(string message);
}
