using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Core.Services.Syntaxes;

/// <summary>
/// Provides a modern AvalonEdit XML Syntax Highlighting Definition (XSHD) for PHP 8+,
/// including match expressions, attributes (#[...]), enums, readonly properties, arrow functions, and typed declarations.
/// </summary>
public static class ModernPhpSyntaxDefinition
{
    private const string PhpXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="PHPModern" extensions=".php;.phtml;.php3;.php4;.php5;.php8" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Attribute" foreground="#DCDCAA" />
            <Color name="Punctuation" foreground="#D4D4D4" />
            <Color name="PhpTag" foreground="#569CD6" fontWeight="bold" />

            <RuleSet>
                <!-- PHPDoc Comment (/** ... */) -->
                <Span color="DocComment" multiline="true">
                    <Begin>/\*\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Multi-Line Comment (/* ... */) -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Single Line Comments (// and #) -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>
                <Span color="Comment">
                    <Begin>#(?![\[])</Begin>
                </Span>

                <!-- PHP 8 Attributes (#[Attribute]) -->
                <Span color="Attribute">
                    <Begin>#\[</Begin>
                    <End>\]</End>
                </Span>

                <!-- Double Quoted Strings with $variable and {$expr} interpolation -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\&quot;" end="" />
                        <Span begin="\\\\" end="" />
                        <Span color="Variable">
                            <Begin>\{?\$</Begin>
                            <End>\}?</End>
                        </Span>
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

                <!-- PHP Open/Close Tags (<?php, <?=, ?>) -->
                <Rule color="PhpTag">
                    &lt;\?(?:php|=)?|\?&gt;
                </Rule>

                <!-- Variables ($varName, $_POST, $this, etc.) -->
                <Rule color="Variable">
                    \$[a-zA-Z_\x7f-\xff][a-zA-Z0-9_\x7f-\xff]*
                </Rule>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>else</Word>
                    <Word>elseif</Word>
                    <Word>endif</Word>
                    <Word>switch</Word>
                    <Word>case</Word>
                    <Word>default</Word>
                    <Word>endswitch</Word>
                    <Word>match</Word>
                    <Word>for</Word>
                    <Word>endfor</Word>
                    <Word>foreach</Word>
                    <Word>endforeach</Word>
                    <Word>while</Word>
                    <Word>endwhile</Word>
                    <Word>do</Word>
                    <Word>break</Word>
                    <Word>continue</Word>
                    <Word>return</Word>
                    <Word>yield</Word>
                    <Word>try</Word>
                    <Word>catch</Word>
                    <Word>finally</Word>
                    <Word>throw</Word>
                </Keywords>

                <!-- Language Keywords & Declarations (Blue #569CD6) -->
                <Keywords color="Keywords">
                    <Word>function</Word>
                    <Word>fn</Word>
                    <Word>class</Word>
                    <Word>interface</Word>
                    <Word>trait</Word>
                    <Word>enum</Word>
                    <Word>extends</Word>
                    <Word>implements</Word>
                    <Word>abstract</Word>
                    <Word>final</Word>
                    <Word>readonly</Word>
                    <Word>public</Word>
                    <Word>protected</Word>
                    <Word>private</Word>
                    <Word>static</Word>
                    <Word>const</Word>
                    <Word>namespace</Word>
                    <Word>use</Word>
                    <Word>as</Word>
                    <Word>global</Word>
                    <Word>new</Word>
                    <Word>clone</Word>
                    <Word>instanceof</Word>
                    <Word>insteadof</Word>
                    <Word>include</Word>
                    <Word>include_once</Word>
                    <Word>require</Word>
                    <Word>require_once</Word>
                    <Word>echo</Word>
                    <Word>print</Word>
                    <Word>exit</Word>
                    <Word>die</Word>
                    <Word>isset</Word>
                    <Word>unset</Word>
                    <Word>empty</Word>
                    <Word>list</Word>
                    <Word>declare</Word>
                    <Word>strict_types</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Built-in PHP 8 Types (Teal #4EC9B0) -->
                <Keywords color="Types">
                    <Word>int</Word>
                    <Word>float</Word>
                    <Word>string</Word>
                    <Word>bool</Word>
                    <Word>array</Word>
                    <Word>object</Word>
                    <Word>callable</Word>
                    <Word>iterable</Word>
                    <Word>void</Word>
                    <Word>never</Word>
                    <Word>mixed</Word>
                    <Word>self</Word>
                    <Word>parent</Word>
                    <Word>static</Word>
                    <Word>Exception</Word>
                    <Word>Throwable</Word>
                    <Word>Error</Word>
                    <Word>DateTime</Word>
                    <Word>DateTimeImmutable</Word>
                    <Word>Closure</Word>
                    <Word>Generator</Word>
                </Keywords>

                <!-- Function / Method Call Invocation (Yellow #DCDCAA) -->
                <Rule color="Functions">
                    \b[a-zA-Z_\x7f-\xff][a-zA-Z0-9_\x7f-\xff]*(?=\s*\()
                </Rule>

                <!-- Numeric Literals -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+|\b0[bB][01]+|\b0[oO][0-7]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?)
                </Rule>

                <!-- Punctuation & Operators -->
                <Rule color="Punctuation">
                    =&gt;|-&gt;|\?-\&gt;|::|\?\?|\.\.\.|[?,.;:()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled modern PHP syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(PhpXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

