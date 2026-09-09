using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Angular.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Angular TypeScript files,
/// including decorators (@Component, @Injectable), Signals (signal, computed, effect, input, output),
/// and TypeScript constructs.
/// </summary>
public static class AngularTsSyntaxDefinition
{
    private const string AngularTsXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="AngularTS" extensions=".component.ts;.service.ts;.directive.ts;.pipe.ts;.guard.ts" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Decorator" foreground="#DCDCAA" fontWeight="bold" />
            <Color name="Signals" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- TSDoc / JSDoc Comment (/** ... */) -->
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
                        <Span begin="\\`" end="" />
                        <Span begin="\\\\" end="" />
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
                        <Span begin="\\&quot;" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Single Quoted Strings -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\'" end="" />
                        <Span begin="\\\\" end="" />
                    </RuleSet>
                </Span>

                <!-- Angular Decorators (@Component, @Injectable, @Directive, @Input, etc.) -->
                <Rule color="Decorator">
                    @[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Angular Signals & Core DI Functions (Teal Bold #4EC9B0) -->
                <Keywords color="Signals">
                    <Word>signal</Word>
                    <Word>computed</Word>
                    <Word>effect</Word>
                    <Word>input</Word>
                    <Word>output</Word>
                    <Word>model</Word>
                    <Word>inject</Word>
                    <Word>toSignal</Word>
                    <Word>toObservable</Word>
                    <Word>viewChild</Word>
                    <Word>viewChildren</Word>
                    <Word>contentChild</Word>
                    <Word>contentChildren</Word>
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

                <!-- TypeScript Keywords & Declarations (Blue #569CD6) -->
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
                    <Word>super</Word>
                    <Word>new</Word>
                    <Word>typeof</Word>
                    <Word>instanceof</Word>
                    <Word>keyof</Word>
                    <Word>void</Word>
                </Keywords>

                <!-- Angular & TypeScript Types (Teal #4EC9B0) -->
                <Keywords color="Types">
                    <Word>OnInit</Word>
                    <Word>OnDestroy</Word>
                    <Word>OnChanges</Word>
                    <Word>AfterViewInit</Word>
                    <Word>ComponentRef</Word>
                    <Word>ElementRef</Word>
                    <Word>TemplateRef</Word>
                    <Word>ViewContainerRef</Word>
                    <Word>ChangeDetectorRef</Word>
                    <Word>HttpClient</Word>
                    <Word>Router</Word>
                    <Word>ActivatedRoute</Word>
                    <Word>Observable</Word>
                    <Word>Subject</Word>
                    <Word>BehaviorSubject</Word>
                    <Word>Subscription</Word>
                    <Word>WritableSignal</Word>
                    <Word>Signal</Word>
                    <Word>InputSignal</Word>
                    <Word>ModelSignal</Word>
                    <Word>OutputEmitterRef</Word>
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
                </Keywords>

                <!-- Method / Function Invocation (Yellow #DCDCAA) -->
                <Rule color="Functions">
                    \b[a-zA-Z_$][a-zA-Z0-9_$]*(?=\s*\()
                </Rule>

                <!-- Numeric Literals -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+|\b0[bB][01]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?)
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    =&gt;|[?,.;:()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Angular TypeScript syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(AngularTsXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

