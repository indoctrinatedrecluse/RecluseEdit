using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Extensions;

/// <summary>
/// Context provided to an extension during initialization to register languages,
/// highlighting definitions, completion providers, and commands.
/// </summary>
public interface IExtensionContext
{
    /// <summary>
    /// Register a new language definition.
    /// </summary>
    void RegisterLanguage(LanguageDefinition language);

    /// <summary>
    /// Register an inline completion provider.
    /// </summary>
    void RegisterInlineCompletion(IInlineCompletionProvider provider);

    /// <summary>
    /// Register a custom syntax highlighting definition for a language ID.
    /// </summary>
    void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition);

    /// <summary>
    /// Get all currently registered languages.
    /// </summary>
    IReadOnlyList<LanguageDefinition> GetRegisteredLanguages();

    /// <summary>
    /// Logs an information message from the extension.
    /// </summary>
    void Log(string message);
}

