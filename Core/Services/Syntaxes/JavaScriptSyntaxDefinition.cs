using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Core.Services.Syntaxes;

/// <summary>
/// Provides a modern AvalonEdit XML Syntax Highlighting Definition (XSHD) for JavaScript and TypeScript,
/// including ES2024 keywords, template literals with interpolation, regex literals, arrow functions, and types.
/// </summary>
public static class JavaScriptSyntaxDefinition
{
    private const string JsTsXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="JavaScriptModern" extensions=".js;.mjs;.cjs;.ts;.mts;.cts" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Regex" foreground="#D16969" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- JSDoc / TSDoc Comment (/** ... */) -->
                <Span color="DocComment" multiline="true">
                    <Begin>/\*\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Multi-Line Comment (/* ... */) -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Single Line Comment (//) -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Template Literal with ${expression} interpolation (`...`) -->
                <Span color="String" multiline="true">
                    <Begin>`</Begin>
                    <End>`</End>
                    <RuleSet>
                        <Span begin="\\`" end="" />
                        <Span begin="\\\\" end="" />
                        <Span color="Variable" multiline="true">
                            <Begin>\$\{</Begin>
                            <End>\}</End>
                        </Span>
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

                <!-- Single Quoted String -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\'" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Regular Expression Literals (/pattern/flags) -->
                <Span color="Regex">
                    <Begin>(?&lt;=[=(,;:!&amp;|?+\-*/~^%\[]\s*)/(?![/*])</Begin>
                    <End>/[gimsuy]*</End>
                    <RuleSet>
                        <Span begin="\\/" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Control Flow Keywords (VS Code Magenta/Purple #C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>else</Word>
                    <Word>switch</Word>
                    <Word>case</Word>
                    <Word>default</Word>
                    <Word>return</Word>
                    <Word>throw</Word>
                    <Word>try</Word>
                    <Word>catch</Word>
                    <Word>finally</Word>
                    <Word>for</Word>
                    <Word>while</Word>
                    <Word>do</Word>
                    <Word>break</Word>
                    <Word>continue</Word>
                    <Word>yield</Word>
                    <Word>await</Word>
                </Keywords>

                <!-- Language Keywords & Declarations (Blue #569CD6) -->
                <Keywords color="Keywords">
                    <Word>function</Word>
                    <Word>class</Word>
                    <Word>const</Word>
                    <Word>let</Word>
                    <Word>var</Word>
                    <Word>interface</Word>
                    <Word>type</Word>
                    <Word>enum</Word>
                    <Word>namespace</Word>
                    <Word>declare</Word>
                    <Word>implements</Word>
                    <Word>extends</Word>
                    <Word>import</Word>
                    <Word>export</Word>
                    <Word>from</Word>
                    <Word>as</Word>
                    <Word>public</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>readonly</Word>
                    <Word>static</Word>
                    <Word>abstract</Word>
                    <Word>override</Word>
                    <Word>async</Word>
                    <Word>get</Word>
                    <Word>set</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                    <Word>undefined</Word>
                    <Word>NaN</Word>
                    <Word>Infinity</Word>
                    <Word>this</Word>
                    <Word>super</Word>
                    <Word>new</Word>
                    <Word>typeof</Word>
                    <Word>instanceof</Word>
                    <Word>in</Word>
                    <Word>of</Word>
                    <Word>keyof</Word>
                    <Word>void</Word>
                    <Word>delete</Word>
                    <Word>debugger</Word>
                    <Word>with</Word>
                    <Word>satisfies</Word>
                    <Word>is</Word>
                    <Word>asserts</Word>
                    <Word>infer</Word>
                </Keywords>

                <!-- Built-in JavaScript & TypeScript Types (Teal #4EC9B0) -->
                <Keywords color="Types">
                    <Word>string</Word>
                    <Word>number</Word>
                    <Word>boolean</Word>
                    <Word>any</Word>
                    <Word>unknown</Word>
                    <Word>never</Word>
                    <Word>symbol</Word>
                    <Word>bigint</Word>
                    <Word>object</Word>
                    <Word>Promise</Word>
                    <Word>Array</Word>
                    <Word>Record</Word>
                    <Word>Map</Word>
                    <Word>Set</Word>
                    <Word>Date</Word>
                    <Word>RegExp</Word>
                    <Word>Error</Word>
                    <Word>Console</Word>
                    <Word>Math</Word>
                    <Word>JSON</Word>
                    <Word>Partial</Word>
                    <Word>Required</Word>
                    <Word>Readonly</Word>
                    <Word>Pick</Word>
                    <Word>Omit</Word>
                    <Word>Exclude</Word>
                    <Word>Extract</Word>
                    <Word>NonNullable</Word>
                    <Word>ReturnType</Word>
                </Keywords>

                <!-- Function / Method Call Invocation (Yellow #DCDCAA) -->
                <Rule color="Functions">
                    \b[a-zA-Z_$][a-zA-Z0-9_$]*(?=\s*\()
                </Rule>

                <!-- Digits & Numeric Literals (including BigInt 100n and Hex 0xAB) -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+n?|\b0[bB][01]+n?|\b0[oO][0-7]+n?|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?n?)
                </Rule>

                <!-- Punctuation & Operators -->
                <Rule color="Punctuation">
                    =&gt;|[?,.;:()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled modern JavaScript/TypeScript syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(JsTsXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

