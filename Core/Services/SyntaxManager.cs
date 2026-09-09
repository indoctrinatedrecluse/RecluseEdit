using System.IO;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using RecluseEdit.Core.Services.Syntaxes;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages language definitions, file associations, and AvalonEdit highlighting definitions
/// with dark-theme optimized palettes.
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
        RegisterCustomSyntaxes();
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
            Extensions = [".ts", ".mts", ".cts"],
            HighlightingName = "JavaScript"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "json",
            DisplayName = "JSON",
            Extensions = [".json", ".jsonc"],
            HighlightingName = "JSON"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "xml",
            DisplayName = "XML / XAML",
            Extensions = [".xml", ".xaml", ".svg", ".config", ".csproj", ".props", ".targets", ".axaml"],
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
            HighlightingName = "Markdown"
        });

        _languages.Add(new LanguageDefinition
        {
            Id = "php",
            DisplayName = "PHP",
            Extensions = [".php", ".phtml", ".php3", ".php4", ".php5", ".php8"],
            HighlightingName = "PHP"
        });
    }

    private void RegisterCustomSyntaxes()
    {
        try
        {
            var jsonDef = CreateJsonHighlightingDefinition();
            RegisterSyntaxDefinition("json", jsonDef);
        }
        catch { }

        try
        {
            // Fallback gracefully to default highlighting if custom definition fails
            var mdDef = MarkdownSyntaxDefinition.CreateDefinition();
            RegisterSyntaxDefinition("markdown", mdDef);
        }
        catch { }

        try
        {
            var jsDef = JavaScriptSyntaxDefinition.CreateDefinition();
            RegisterSyntaxDefinition("javascript", jsDef);
            RegisterSyntaxDefinition("typescript", jsDef);
        }
        catch { }

        try
        {
            var cssDef = CssSyntaxDefinition.CreateDefinition();
            RegisterSyntaxDefinition("css", cssDef);
        }
        catch { }

        try
        {
            var phpDef = ModernPhpSyntaxDefinition.CreateDefinition();
            RegisterSyntaxDefinition("php", phpDef);
        }
        catch { }
    }

    private static IHighlightingDefinition CreateJsonHighlightingDefinition()
    {
        const string jsonXshd = """
            <?xml version="1.0"?>
            <SyntaxDefinition name="JSON" extensions=".json" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                <Color name="Digits" foreground="#B5CEA8" />
                <Color name="String" foreground="#CE9178" />
                <Color name="PropertyName" foreground="#9CDCFE" />
                <Color name="Punctuation" foreground="#D4D4D4" />
                <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
                <Color name="Comment" foreground="#6A9955" />

                <RuleSet>
                    <Span color="Comment">
                        <Begin>//</Begin>
                    </Span>
                    <Span color="Comment" multiline="true">
                        <Begin>/\*</Begin>
                        <End>\*/</End>
                    </Span>
                    <Rule color="PropertyName">
                        &quot;(\\.|[^&quot;\\])*&quot;(?=\s*:)
                    </Rule>
                    <Span color="String">
                        <Begin>&quot;</Begin>
                        <End>&quot;</End>
                        <RuleSet>
                            <Span begin="\\&quot;" end="" />
                            <Span begin="\\\\" end="" />
                        </RuleSet>
                    </Span>
                    <Keywords color="Keywords">
                        <Word>true</Word>
                        <Word>false</Word>
                        <Word>null</Word>
                    </Keywords>
                    <Rule color="Digits">
                        \b0[xX][0-9a-fA-F]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?)
                    </Rule>
                    <Rule color="Punctuation">
                        [{}\[\]\:,]
                    </Rule>
                </RuleSet>
            </SyntaxDefinition>
            """;

        using var reader = XmlReader.Create(new StringReader(jsonXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
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
        ApplyDarkThemeColors(definition);
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
            var def = HighlightingManager.Instance.GetDefinition(language.HighlightingName);
            if (def != null)
            {
                ApplyDarkThemeColors(def);
                return def;
            }
        }

        return null;
    }

    private static void ApplyDarkThemeColors(IHighlightingDefinition definition)
    {
        var keywordBrush = new SimpleHighlightingBrush(Color.FromRgb(0x56, 0x9C, 0xD6));     // #569CD6 Blue
        var controlFlowBrush = new SimpleHighlightingBrush(Color.FromRgb(0xC5, 0x86, 0xC0)); // #C586C0 Magenta
        var functionBrush = new SimpleHighlightingBrush(Color.FromRgb(0xDC, 0xDC, 0xAA));    // #DCDCAA Light yellow
        var stringBrush = new SimpleHighlightingBrush(Color.FromRgb(0xCE, 0x91, 0x78));      // #CE9178 Warm orange
        var commentBrush = new SimpleHighlightingBrush(Color.FromRgb(0x6A, 0x99, 0x55));     // #6A9955 Green
        var numberBrush = new SimpleHighlightingBrush(Color.FromRgb(0xB5, 0xCE, 0xA8));      // #B5CEA8 Mint
        var attributeBrush = new SimpleHighlightingBrush(Color.FromRgb(0x9C, 0xDC, 0xFE));   // #9CDCFE Light cyan
        var variableBrush = new SimpleHighlightingBrush(Color.FromRgb(0x9C, 0xDC, 0xFE));    // #9CDCFE Sky blue
        var tagBrush = new SimpleHighlightingBrush(Color.FromRgb(0x56, 0x9C, 0xD6));         // #569CD6 Tag blue
        var typeBrush = new SimpleHighlightingBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));        // #4EC9B0 Teal
        var regexBrush = new SimpleHighlightingBrush(Color.FromRgb(0xD1, 0x69, 0x69));       // #D16969 Soft red
        var punctuationBrush = new SimpleHighlightingBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)); // #D4D4D4 Gray
        var boldBrush = new SimpleHighlightingBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));        // #FFFFFF White
        var heading1Brush = new SimpleHighlightingBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));    // #4EC9B0 Teal
        var heading2Brush = new SimpleHighlightingBrush(Color.FromRgb(0x56, 0x9C, 0xD6));    // #569CD6 Blue
        var heading3Brush = new SimpleHighlightingBrush(Color.FromRgb(0x9C, 0xDC, 0xFE));    // #9CDCFE Cyan
        var heading4Brush = new SimpleHighlightingBrush(Color.FromRgb(0xDC, 0xDC, 0xAA));    // #DCDCAA Yellow
        var tagBracketBrush = new SimpleHighlightingBrush(Color.FromRgb(0x80, 0x80, 0x80));  // #808080 Dim Gray

        foreach (var color in definition.NamedHighlightingColors)
        {
            var name = color.Name ?? "";

            if (name.Equals("Heading1", StringComparison.OrdinalIgnoreCase)) color.Foreground = heading1Brush;
            else if (name.Equals("Heading2", StringComparison.OrdinalIgnoreCase)) color.Foreground = heading2Brush;
            else if (name.Equals("Heading3", StringComparison.OrdinalIgnoreCase)) color.Foreground = heading3Brush;
            else if (name.Equals("Heading4", StringComparison.OrdinalIgnoreCase)) color.Foreground = heading4Brush;
            else if (name.Equals("Heading5", StringComparison.OrdinalIgnoreCase)) color.Foreground = controlFlowBrush;
            else if (name.Equals("Heading6", StringComparison.OrdinalIgnoreCase)) color.Foreground = stringBrush;
            else if (name.Equals("Bold", StringComparison.OrdinalIgnoreCase)) color.Foreground = boldBrush;
            else if (name.Contains("ControlFlow", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("AtRule", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = controlFlowBrush;
            }
            else if (name.Contains("Function", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Method", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Annotation", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Decorator", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = functionBrush;
            }
            else if (name.Contains("Regex", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = regexBrush;
            }
            else if (name.Contains("Comment", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Blockquote", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("HorizontalRule", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = commentBrush;
            }
            else if (name.Contains("String", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("AttributeValue", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Value", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Code", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Italic", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("ColorHex", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = stringBrush;
            }
            else if (name.Contains("Digit", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Units", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = numberBrush;
            }
            else if (name.Contains("Variable", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Parameter", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Symbol", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("LinkUrl", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = variableBrush;
            }
            else if (name.Contains("Attribute", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Property", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Selector", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = attributeBrush;
            }
            else if (name.Contains("TagBracket", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Table", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = tagBracketBrush;
            }
            else if (name.Contains("Tag", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("DocType", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("XmlDeclaration", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Link", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("ListBullet", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = tagBrush;
            }
            else if (name.Contains("Keyword", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("KeyWords", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Intrinsics", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Literals", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = keywordBrush;
            }
            else if (name.Contains("Type", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Class", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Interface", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = typeBrush;
            }
            else if (name.Contains("Punctuation", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Brace", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Colon", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Entity", StringComparison.OrdinalIgnoreCase))
            {
                color.Foreground = punctuationBrush;
            }
        }
    }
}

