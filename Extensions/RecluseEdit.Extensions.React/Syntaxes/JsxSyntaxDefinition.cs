using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.React.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for React JSX and TSX,
/// including JSX tags, JSX attributes, embedded expressions, React Hooks, and TypeScript keywords.
/// </summary>
public static class JsxSyntaxDefinition
{
    private const string JsxXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="ReactJSX" extensions=".jsx;.tsx" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Hooks" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="JsxTag" foreground="#569CD6" />
            <Color name="JsxComponent" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="JsxAttribute" foreground="#9CDCFE" />
            <Color name="JsxBracket" foreground="#808080" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
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

                <!-- Template Literals (`...`) with ${...} -->
                <Span color="String" multiline="true">
                    <Begin>`</Begin>
                    <End>`</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                        <Span color="Variable" multiline="true">
                            <Begin>\$\{</Begin>
                            <End>\}</End>
                        </Span>
                    </RuleSet>
                </Span>

                <!-- Double Quoted Strings -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Single Quoted Strings -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- JSX Closing Tags (</Component> or </div>) -->
                <Rule color="JsxComponent">
                    (?&lt;=&lt;/)[A-Z][a-zA-Z0-9_.]*(?=&gt;)
                </Rule>
                <Rule color="JsxTag">
                    (?&lt;=&lt;/)[a-z][a-zA-Z0-9_\-]*(?=&gt;)
                </Rule>

                <!-- JSX Opening Custom Component Tags (<Component or <MySub.Component) -->
                <Rule color="JsxComponent">
                    (?&lt;=&lt;)[A-Z][a-zA-Z0-9_.]*(?=[\s/&gt;])
                </Rule>

                <!-- JSX HTML Standard Tags (<div, <span, <button, etc.) -->
                <Rule color="JsxTag">
                    (?&lt;=&lt;)[a-z][a-zA-Z0-9_\-]*(?=[\s/&gt;])
                </Rule>

                <!-- JSX Attributes (className=, onClick=, style=, etc.) -->
                <Rule color="JsxAttribute">
                    \b[a-zA-Z_][a-zA-Z0-9_\-]*(?=\s*=)
                </Rule>

                <!-- React Hooks Built-ins (Teal Bold #4EC9B0) -->
                <Keywords color="Hooks">
                    <Word>useState</Word>
                    <Word>useEffect</Word>
                    <Word>useCallback</Word>
                    <Word>useMemo</Word>
                    <Word>useRef</Word>
                    <Word>useContext</Word>
                    <Word>useReducer</Word>
                    <Word>useId</Word>
                    <Word>useTransition</Word>
                    <Word>useDeferredValue</Word>
                    <Word>useImperativeHandle</Word>
                    <Word>useLayoutEffect</Word>
                    <Word>useInsertionEffect</Word>
                    <Word>useSyncExternalStore</Word>
                    <Word>useSelector</Word>
                    <Word>useDispatch</Word>
                    <Word>useQuery</Word>
                    <Word>useMutation</Word>
                    <Word>useSubscription</Word>
                </Keywords>

                <!-- Control Flow (Magenta #C586C0) -->
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

                <!-- Declarations & TS/JS Keywords (Blue #569CD6) -->
                <Keywords color="Keywords">
                    <Word>import</Word>
                    <Word>export</Word>
                    <Word>from</Word>
                    <Word>as</Word>
                    <Word>default</Word>
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
                    <Word>extends</Word>
                    <Word>implements</Word>
                    <Word>public</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>readonly</Word>
                    <Word>static</Word>
                    <Word>abstract</Word>
                    <Word>override</Word>
                    <Word>async</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                    <Word>undefined</Word>
                    <Word>this</Word>
                    <Word>new</Word>
                    <Word>typeof</Word>
                    <Word>instanceof</Word>
                    <Word>keyof</Word>
                    <Word>void</Word>
                </Keywords>

                <!-- React & TypeScript Types (Teal #4EC9B0) -->
                <Keywords color="Types">
                    <Word>ReactNode</Word>
                    <Word>ReactElement</Word>
                    <Word>FC</Word>
                    <Word>FunctionalComponent</Word>
                    <Word>Component</Word>
                    <Word>PureComponent</Word>
                    <Word>JSX</Word>
                    <Word>PropsWithChildren</Word>
                    <Word>CSSProperties</Word>
                    <Word>MouseEvent</Word>
                    <Word>ChangeEvent</Word>
                    <Word>FormEvent</Word>
                    <Word>KeyboardEvent</Word>
                    <Word>string</Word>
                    <Word>number</Word>
                    <Word>boolean</Word>
                    <Word>any</Word>
                    <Word>unknown</Word>
                    <Word>never</Word>
                    <Word>void</Word>
                    <Word>object</Word>
                    <Word>Promise</Word>
                    <Word>Array</Word>
                    <Word>Record</Word>
                </Keywords>

                <!-- Function / Component Calls -->
                <Rule color="Functions">
                    \b[a-zA-Z_$][a-zA-Z0-9_$]*(?=\s*\()
                </Rule>

                <!-- Numbers -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+n?|\b0[bB][01]+n?|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?n?)
                </Rule>

                <!-- Tag Brackets (<, >, </, />) -->
                <Rule color="JsxBracket">
                    &lt;/|/&gt;|&lt;|&gt;
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    =&gt;|[?,.;:()\[\]{}+\-*\/%=&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled JSX/TSX syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(JsxXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

