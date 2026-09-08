using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Flutter.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for the Dart programming language,
/// including Dart 3 keywords, annotations, types, strings, and doc comments.
/// </summary>
public static class DartSyntaxDefinition
{
    private const string DartXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Dart" extensions=".dart" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Annotation" foreground="#DCDCAA" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Doc Comments (///) -->
                <Span color="DocComment">
                    <Begin>///</Begin>
                </Span>

                <!-- Single Line Comment (//) -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Multi-Line Comment (/* */) -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Triple Quoted Strings -->
                <Span color="String" multiline="true">
                    <Begin>'''</Begin>
                    <End>'''</End>
                </Span>
                <Span color="String" multiline="true">
                    <Begin>&quot;&quot;&quot;</Begin>
                    <End>&quot;&quot;&quot;</End>
                </Span>

                <!-- Single Quoted String -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\'" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Double Quoted String -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\&quot;" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Annotations (@override, @deprecated, etc.) -->
                <Rule color="Annotation">
                    @[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Keywords -->
                <Keywords color="Keywords">
                    <Word>abstract</Word>
                    <Word>as</Word>
                    <Word>assert</Word>
                    <Word>async</Word>
                    <Word>await</Word>
                    <Word>break</Word>
                    <Word>case</Word>
                    <Word>catch</Word>
                    <Word>class</Word>
                    <Word>const</Word>
                    <Word>continue</Word>
                    <Word>covariant</Word>
                    <Word>default</Word>
                    <Word>deferred</Word>
                    <Word>do</Word>
                    <Word>dynamic</Word>
                    <Word>else</Word>
                    <Word>enum</Word>
                    <Word>export</Word>
                    <Word>extends</Word>
                    <Word>extension</Word>
                    <Word>external</Word>
                    <Word>factory</Word>
                    <Word>false</Word>
                    <Word>final</Word>
                    <Word>finally</Word>
                    <Word>for</Word>
                    <Word>Function</Word>
                    <Word>get</Word>
                    <Word>hide</Word>
                    <Word>if</Word>
                    <Word>implements</Word>
                    <Word>import</Word>
                    <Word>in</Word>
                    <Word>interface</Word>
                    <Word>is</Word>
                    <Word>late</Word>
                    <Word>library</Word>
                    <Word>mixin</Word>
                    <Word>new</Word>
                    <Word>null</Word>
                    <Word>of</Word>
                    <Word>on</Word>
                    <Word>operator</Word>
                    <Word>part</Word>
                    <Word>required</Word>
                    <Word>rethrow</Word>
                    <Word>return</Word>
                    <Word>sealed</Word>
                    <Word>set</Word>
                    <Word>show</Word>
                    <Word>static</Word>
                    <Word>super</Word>
                    <Word>switch</Word>
                    <Word>sync</Word>
                    <Word>this</Word>
                    <Word>throw</Word>
                    <Word>true</Word>
                    <Word>try</Word>
                    <Word>typedef</Word>
                    <Word>var</Word>
                    <Word>void</Word>
                    <Word>while</Word>
                    <Word>with</Word>
                    <Word>yield</Word>
                </Keywords>

                <!-- Common Dart & Flutter Built-in Types -->
                <Keywords color="Types">
                    <Word>int</Word>
                    <Word>double</Word>
                    <Word>num</Word>
                    <Word>String</Word>
                    <Word>bool</Word>
                    <Word>List</Word>
                    <Word>Map</Word>
                    <Word>Set</Word>
                    <Word>Runes</Word>
                    <Word>Symbol</Word>
                    <Word>Object</Word>
                    <Word>DateTime</Word>
                    <Word>Duration</Word>
                    <Word>Future</Word>
                    <Word>Stream</Word>
                    <Word>Iterable</Word>
                    <Word>Widget</Word>
                    <Word>StatelessWidget</Word>
                    <Word>StatefulWidget</Word>
                    <Word>State</Word>
                    <Word>BuildContext</Word>
                    <Word>Key</Word>
                    <Word>Color</Word>
                    <Word>Theme</Word>
                    <Word>ThemeData</Word>
                    <Word>Scaffold</Word>
                    <Word>AppBar</Word>
                    <Word>Text</Word>
                    <Word>Container</Word>
                    <Word>Center</Word>
                    <Word>Column</Word>
                    <Word>Row</Word>
                    <Word>Expanded</Word>
                    <Word>Padding</Word>
                    <Word>SizedBox</Word>
                    <Word>ListView</Word>
                </Keywords>

                <!-- Digits / Numbers -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?)
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [?,.;:()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Dart syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(DartXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
