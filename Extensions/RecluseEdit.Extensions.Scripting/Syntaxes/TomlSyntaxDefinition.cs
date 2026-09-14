using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for TOML / Cargo.toml files in Dark+ styling.
/// Supports tables, keys, strings, booleans, numbers, and comments.
/// </summary>
public static class TomlSyntaxDefinition
{
    private const string TomlXshd = """"
        <?xml version="1.0"?>
        <SyntaxDefinition name="TOML" extensions=".toml" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Section" foreground="#569CD6" fontWeight="bold" />
            <Color name="Key" foreground="#9CDCFE" />
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="Boolean" foreground="#569CD6" fontWeight="bold" />

            <RuleSet ignoreCase="false">
                <!-- Comments: # -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Tables / Sections: [section] or [[array_section]] -->
                <Span color="Section">
                    <Begin>\[</Begin>
                    <End>\]</End>
                </Span>

                <!-- Multiline strings: """...""" or '''...''' -->
                <Span color="String" multiline="true">
                    <Begin>"""</Begin>
                    <End>"""</End>
                </Span>
                <Span color="String" multiline="true">
                    <Begin>'''</Begin>
                    <End>'''</End>
                </Span>

                <!-- Basic strings: "..." or '...' -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                </Span>

                <!-- Booleans -->
                <Keywords color="Boolean">
                    <Word>true</Word>
                    <Word>false</Word>
                </Keywords>

                <!-- Numbers and ISO 8601 timestamps -->
                <Rule color="Digits">\b\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})?)?\b</Rule>
                <Rule color="Digits">\b0[xX][0-9a-fA-F_]+\b|\b0[oO][0-7_]+\b|\b0[bB][01_]+\b|\b[+-]?\d[0-9_]*(\.[0-9_]+)?([eE][+-]?[0-9_]+)?\b</Rule>

                <!-- Keys before '=' -->
                <Rule color="Key">[A-Za-z0-9_\-]+(?=\s*=)</Rule>
            </RuleSet>
        </SyntaxDefinition>
        """";

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(TomlXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
