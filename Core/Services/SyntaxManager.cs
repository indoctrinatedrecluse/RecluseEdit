using System.IO;
using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages language definitions, file associations, and AvalonEdit highlighting definitions.
/// </summary>
public class SyntaxManager
{
    private readonly List<LanguageDefinition> _languages = [];
    private readonly Dictionary<string, IHighlightingDefinition> _customDefinitions = new(StringComparer.OrdinalIgnoreCase);

    public static LanguageDefinition PlainText { get; } = new()
    {
        Id = "plaintext",
        DisplayName = "Plain Text",
        Extensions = [".txt", ".log"],
        HighlightingName = null
    };

    public IReadOnlyList<LanguageDefinition> SupportedLanguages => _languages.AsReadOnly();

    public SyntaxManager()
    {
        RegisterDefaultLanguages();
    }

    private void RegisterDefaultLanguages()
    {
        _languages.Add(PlainText);

        _languages.Add(new LanguageDefinition
        {
            Id = "html",
            DisplayName = "HTML",
            Extensions = [".html", ".htm", ".xhtml"],
            HighlightingName = "HTML"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "css",
            DisplayName = "CSS",
            Extensions = [".css", ".scss", ".less"],
            HighlightingName = "CSS"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "javascript",
            DisplayName = "JavaScript",
            Extensions = [".js", ".mjs", ".cjs"],
            HighlightingName = "JavaScript"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "typescript",
            DisplayName = "TypeScript",
            Extensions = [".ts", ".tsx"],
            HighlightingName = "JavaScript" // AvalonEdit defaults JS highlighting for TS
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "json",
            DisplayName = "JSON",
            Extensions = [".json"],
            HighlightingName = "JavaScript"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "xml",
            DisplayName = "XML",
            Extensions = [".xml", ".xaml", ".svg", ".config", ".csproj"],
            HighlightingName = "XML"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "csharp",
            DisplayName = "C#",
            Extensions = [".cs"],
            HighlightingName = "C#"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "markdown",
            DisplayName = "Markdown",
            Extensions = [".md", ".markdown"],
            HighlightingName = null
        });
    }

    public void RegisterLanguage(LanguageDefinition language)
    {
        var existing = _languages.FindIndex(l => l.Id.Equals(language.Id, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            _languages[existing] = language;
        }
        else
        {
            _languages.Add(language);
        }
    }

    public void RegisterSyntaxDefinition(string languageId, IHighlightingDefinition definition)
    {
        _customDefinitions[languageId] = definition;
    }

    public LanguageDefinition GetLanguageForFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return PlainText;
        }

        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext))
        {
            return PlainText;
        }

        foreach (var lang in _languages)
        {
            if (lang.Extensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return lang;
            }
        }

        return PlainText;
    }

    public LanguageDefinition? GetLanguageById(string languageId)
    {
        return _languages.FirstOrDefault(l => l.Id.Equals(languageId, StringComparison.OrdinalIgnoreCase));
    }

    public IHighlightingDefinition? GetHighlighting(LanguageDefinition language)
    {
        if (_customDefinitions.TryGetValue(language.Id, out var customDef))
        {
            return customDef;
        }

        if (!string.IsNullOrEmpty(language.HighlightingName))
        {
            return HighlightingManager.Instance.GetDefinition(language.HighlightingName);
        }

        return null;
    }
}

