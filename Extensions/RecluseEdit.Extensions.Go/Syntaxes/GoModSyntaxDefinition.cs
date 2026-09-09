using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Go.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for go.mod, go.work, and go.sum files.
/// </summary>
public static class GoModSyntaxDefinition
{
    private const string GoModXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="GoMod" extensions="go.mod;go.work;go.sum;.mod;.work" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="Modifiers" foreground="#C586C0" fontWeight="bold" />
            <Color name="Versions" foreground="#B5CEA8" />
            <Color name="Packages" foreground="#9CDCFE" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Comments -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- String Literals -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                </Span>

                <!-- Directives (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>module</Word>
                    <Word>go</Word>
                    <Word>toolchain</Word>
                    <Word>require</Word>
                    <Word>replace</Word>
                    <Word>exclude</Word>
                    <Word>retract</Word>
                    <Word>use</Word>
                </Keywords>

                <!-- Modifiers (#C586C0) -->
                <Keywords color="Modifiers">
                    <Word>indirect</Word>
                </Keywords>

                <!-- Arrow operator for replace directive (=>) -->
                <Rule color="Operators">
                    =&gt;
                </Rule>

                <!-- Semantic Version numbers (e.g. v1.2.3, v0.0.0-20240101-abcdef) -->
                <Rule color="Versions">
                    \bv\d+(\.\d+)*(-[a-zA-Z0-9_\-\.]+)?\b |
                    \b\d+\.\d+(\.\d+)?\b
                </Rule>

                <!-- Package identifiers / repository domain paths -->
                <Rule color="Packages">
                    \b[a-zA-Z0-9_\-\.]+\.[a-zA-Z]{2,}(/[a-zA-Z0-9_\-\.]+)+\b
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [\(\)]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = new StringReader(GoModXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
