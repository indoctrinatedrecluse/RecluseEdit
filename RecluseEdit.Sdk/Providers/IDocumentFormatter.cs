using System.Collections.Generic;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Provider interface for pluggable document formatters.
/// </summary>
public interface IDocumentFormatter
{
    string FormatterId { get; }
    string DisplayName { get; }
    IReadOnlyList<string> SupportedLanguages { get; }

    bool CanFormat(string language, string filePath);
    string Format(string sourceCode, string language, FormattingOptions options);
}

