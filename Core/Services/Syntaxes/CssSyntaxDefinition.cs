using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Core.Services.Syntaxes;

/// <summary>
/// Provides a modern AvalonEdit XML Syntax Highlighting Definition (XSHD) for CSS, SCSS, and LESS,
/// including CSS variables (--var), pseudo-classes/elements, modern at-rules, units, and color literals.
/// </summary>
public static class CssSyntaxDefinition
{
    private const string CssXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="CSSModern" extensions=".css;.scss;.less" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Property" foreground="#9CDCFE" />
            <Color name="Value" foreground="#CE9178" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="SelectorClass" foreground="#D7BA7D" />
            <Color name="SelectorId" foreground="#4EC9B0" />
            <Color name="SelectorTag" foreground="#569CD6" />
            <Color name="PseudoClass" foreground="#D7BA7D" />
            <Color name="AtRule" foreground="#C586C0" fontWeight="bold" />
            <Color name="Function" foreground="#DCDCAA" />
            <Color name="ColorHex" foreground="#CE9178" />
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Multi-Line Comment (/* ... */) -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Single Line Comment (// in SCSS/LESS) -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Double Quoted Strings -->
                <Span color="Value">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Single Quoted Strings -->
                <Span color="Value">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- At-rules (@media, @keyframes, @container, etc.) -->
                <Rule color="AtRule">
                    @[a-zA-Z\-]+
                </Rule>

                <!-- CSS Variables -->
                <Rule color="Variable">
                    --[a-zA-Z0-9_\-]+
                </Rule>

                <!-- CSS Functions (var(), calc(), rgb(), url(), etc.) -->
                <Rule color="Function">
                    \b(var|calc|min|max|clamp|rgb|rgba|hsl|hsla|oklch|url|linear-gradient|radial-gradient|conic-gradient|attr|env|translate|scale|rotate|matrix)(?=\s*\()
                </Rule>

                <!-- Hex Colors (#FFF, #1E1E1E, #333333AA) -->
                <Rule color="ColorHex">
                    \#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b
                </Rule>

                <!-- ID Selectors (#my-element) -->
                <Rule color="SelectorId">
                    \#[a-zA-Z_][a-zA-Z0-9_\-]*
                </Rule>

                <!-- Class Selectors (.btn, .active) -->
                <Rule color="SelectorClass">
                    \.[a-zA-Z_][a-zA-Z0-9_\-]*
                </Rule>

                <!-- Pseudo-classes & Pseudo-elements (:hover, ::before, :has(), etc.) -->
                <Rule color="PseudoClass">
                    :{1,2}[a-zA-Z\-]+(?:\([^\)]*\))?
                </Rule>

                <!-- Numeric Values with Units (px, rem, %, vh, etc.) -->
                <Rule color="Digits">
                    \b\d+(\.\d+)?(px|rem|em|vh|vw|dvh|dvw|cqw|cqh|%|deg|rad|turn|s|ms|fr|ch|ex)\b|\b\d+(\.\d+)?\b
                </Rule>

                <!-- Common CSS Properties (Light Cyan #9CDCFE) -->
                <Rule color="Property">
                    (?&lt;=[{\s;])[a-zA-Z\-]+(?=\s*:)
                </Rule>

                <!-- Common CSS Property Values -->
                <Keywords color="Value">
                    <Word>none</Word>
                    <Word>block</Word>
                    <Word>inline</Word>
                    <Word>inline-block</Word>
                    <Word>flex</Word>
                    <Word>inline-flex</Word>
                    <Word>grid</Word>
                    <Word>inline-grid</Word>
                    <Word>table</Word>
                    <Word>absolute</Word>
                    <Word>relative</Word>
                    <Word>fixed</Word>
                    <Word>sticky</Word>
                    <Word>static</Word>
                    <Word>auto</Word>
                    <Word>hidden</Word>
                    <Word>visible</Word>
                    <Word>scroll</Word>
                    <Word>inherit</Word>
                    <Word>initial</Word>
                    <Word>unset</Word>
                    <Word>transparent</Word>
                    <Word>currentcolor</Word>
                    <Word>center</Word>
                    <Word>left</Word>
                    <Word>right</Word>
                    <Word>top</Word>
                    <Word>bottom</Word>
                    <Word>bold</Word>
                    <Word>italic</Word>
                    <Word>normal</Word>
                    <Word>nowrap</Word>
                    <Word>wrap</Word>
                    <Word>pointer</Word>
                    <Word>default</Word>
                    <Word>cover</Word>
                    <Word>contain</Word>
                    <Word>border-box</Word>
                    <Word>content-box</Word>
                    <Word>column</Word>
                    <Word>row</Word>
                    <Word>space-between</Word>
                    <Word>space-around</Word>
                    <Word>space-evenly</Word>
                </Keywords>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [?:;{}()\[\]+\-*/&gt;~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled CSS syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(CssXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
