using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for Kotlin (.kt, .kts) files in Dark+ styling,
/// including coroutines, data classes, and Ktor keywords.
/// </summary>
public static class KotlinSyntaxDefinition
{
    private const string KotlinXshd = """"
        <?xml version="1.0"?>
        <SyntaxDefinition name="Kotlin" extensions=".kt;.kts" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Modifiers" foreground="#569CD6" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Annotation" foreground="#DCDCAA" />
            <Color name="Digits" foreground="#B5CEA8" />

            <RuleSet ignoreCase="false">
                <!-- Single-line comments -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Multi-line comments -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Multi-line strings: """...""" -->
                <Span color="String" multiline="true">
                    <Begin>"""</Begin>
                    <End>"""</End>
                </Span>

                <!-- Strings -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Annotations -->
                <Rule color="Annotation">@[a-zA-Z0-9_]+</Rule>

                <!-- Digits -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F_]+[uUL]?\b|\b0[bB][01_]+[uUL]?\b|\b\d+[uULfF]?\b|\b\d+\.\d+([eE][+-]?\d+)?[fF]?\b</Rule>

                <!-- Control Flow -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>else</Word>
                    <Word>when</Word>
                    <Word>while</Word>
                    <Word>do</Word>
                    <Word>for</Word>
                    <Word>break</Word>
                    <Word>continue</Word>
                    <Word>return</Word>
                    <Word>throw</Word>
                    <Word>try</Word>
                    <Word>catch</Word>
                    <Word>finally</Word>
                </Keywords>

                <!-- Kotlin Keywords & Modifiers -->
                <Keywords color="Keywords">
                    <Word>fun</Word>
                    <Word>val</Word>
                    <Word>var</Word>
                    <Word>class</Word>
                    <Word>interface</Word>
                    <Word>object</Word>
                    <Word>package</Word>
                    <Word>import</Word>
                    <Word>this</Word>
                    <Word>super</Word>
                    <Word>typeof</Word>
                    <Word>as</Word>
                    <Word>is</Word>
                    <Word>in</Word>
                    <Word>suspend</Word>
                    <Word>data</Word>
                    <Word>sealed</Word>
                    <Word>enum</Word>
                    <Word>inline</Word>
                    <Word>override</Word>
                    <Word>open</Word>
                    <Word>abstract</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>public</Word>
                    <Word>internal</Word>
                    <Word>companion</Word>
                    <Word>by</Word>
                    <Word>lazy</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Types -->
                <Keywords color="Types">
                    <Word>Unit</Word>
                    <Word>Nothing</Word>
                    <Word>Any</Word>
                    <Word>Int</Word>
                    <Word>Long</Word>
                    <Word>Float</Word>
                    <Word>Double</Word>
                    <Word>Boolean</Word>
                    <Word>Char</Word>
                    <Word>String</Word>
                    <Word>List</Word>
                    <Word>Map</Word>
                    <Word>Set</Word>
                    <Word>CoroutineScope</Word>
                    <Word>Flow</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """";

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(KotlinXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
