using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Python.Syntaxes;

/// <summary>
/// Provides a modern AvalonEdit XML Syntax Highlighting Definition (XSHD) for Python 3.12+,
/// including decorators, f-strings with interpolation, type hints, dunder methods, and pattern matching.
/// </summary>
public static class PythonSyntaxDefinition
{
    private const string PythonXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Python" extensions=".py;.pyw;.pyi;.pyd" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Decorator" foreground="#DCDCAA" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Builtins" foreground="#4EC9B0" />
            <Color name="Dunder" foreground="#C586C0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Single Line Comment (#) -->
                <Span color="Comment">
                    <Begin>\#</Begin>
                </Span>

                <!-- Triple Single-Quoted Multiline Strings ('''...''') -->
                <Span color="DocComment" multiline="true">
                    <Begin>'''</Begin>
                    <End>'''</End>
                </Span>

                <!-- Triple Double-Quoted Multiline Strings -->
                <Span color="DocComment" multiline="true">
                    <Begin>&quot;&quot;&quot;</Begin>
                    <End>&quot;&quot;&quot;</End>
                </Span>

                <!-- Formatted F-Strings (f"..." or f'...') with {expression} interpolation -->
                <Span color="String">
                    <Begin>[fF]&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                        <Span color="Variable">
                            <Begin>\{</Begin>
                            <End>\}</End>
                        </Span>
                    </RuleSet>
                </Span>
                <Span color="String">
                    <Begin>[fF]'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                        <Span color="Variable">
                            <Begin>\{</Begin>
                            <End>\}</End>
                        </Span>
                    </RuleSet>
                </Span>

                <!-- Standard Double-Quoted Strings (with r, b, u prefixes) -->
                <Span color="String">
                    <Begin>[rRbBuU]?&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Standard Single-Quoted Strings (with r, b, u prefixes) -->
                <Span color="String">
                    <Begin>[rRbBuU]?'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Decorators (@app.route, @property, @staticmethod, etc.) -->
                <Rule color="Decorator">
                    @[a-zA-Z_][a-zA-Z0-9_\.]*
                </Rule>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>elif</Word>
                    <Word>else</Word>
                    <Word>for</Word>
                    <Word>while</Word>
                    <Word>break</Word>
                    <Word>continue</Word>
                    <Word>return</Word>
                    <Word>yield</Word>
                    <Word>try</Word>
                    <Word>except</Word>
                    <Word>finally</Word>
                    <Word>raise</Word>
                    <Word>with</Word>
                    <Word>async</Word>
                    <Word>await</Word>
                    <Word>match</Word>
                    <Word>case</Word>
                </Keywords>

                <!-- Language Keywords & Declarations (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>def</Word>
                    <Word>class</Word>
                    <Word>import</Word>
                    <Word>from</Word>
                    <Word>as</Word>
                    <Word>lambda</Word>
                    <Word>pass</Word>
                    <Word>assert</Word>
                    <Word>global</Word>
                    <Word>nonlocal</Word>
                    <Word>del</Word>
                    <Word>in</Word>
                    <Word>is</Word>
                    <Word>and</Word>
                    <Word>or</Word>
                    <Word>not</Word>
                    <Word>None</Word>
                    <Word>True</Word>
                    <Word>False</Word>
                    <Word>self</Word>
                    <Word>cls</Word>
                </Keywords>

                <!-- Dunder Methods & Magic Attributes (#C586C0) -->
                <Keywords color="Dunder">
                    <Word>__init__</Word>
                    <Word>__str__</Word>
                    <Word>__repr__</Word>
                    <Word>__name__</Word>
                    <Word>__main__</Word>
                    <Word>__all__</Word>
                    <Word>__call__</Word>
                    <Word>__len__</Word>
                    <Word>__getitem__</Word>
                    <Word>__setitem__</Word>
                    <Word>__enter__</Word>
                    <Word>__exit__</Word>
                    <Word>__file__</Word>
                    <Word>__doc__</Word>
                    <Word>__dict__</Word>
                </Keywords>

                <!-- Built-in Functions & Utilities (#4EC9B0) -->
                <Keywords color="Builtins">
                    <Word>print</Word>
                    <Word>len</Word>
                    <Word>range</Word>
                    <Word>enumerate</Word>
                    <Word>zip</Word>
                    <Word>isinstance</Word>
                    <Word>issubclass</Word>
                    <Word>map</Word>
                    <Word>filter</Word>
                    <Word>open</Word>
                    <Word>super</Word>
                    <Word>sum</Word>
                    <Word>min</Word>
                    <Word>max</Word>
                    <Word>abs</Word>
                    <Word>all</Word>
                    <Word>any</Word>
                    <Word>iter</Word>
                    <Word>next</Word>
                </Keywords>

                <!-- Python Types & Typing Helpers (#4EC9B0) -->
                <Keywords color="Types">
                    <Word>int</Word>
                    <Word>float</Word>
                    <Word>str</Word>
                    <Word>bool</Word>
                    <Word>list</Word>
                    <Word>dict</Word>
                    <Word>set</Word>
                    <Word>tuple</Word>
                    <Word>bytes</Word>
                    <Word>object</Word>
                    <Word>type</Word>
                    <Word>Any</Word>
                    <Word>Optional</Word>
                    <Word>Union</Word>
                    <Word>List</Word>
                    <Word>Dict</Word>
                    <Word>Set</Word>
                    <Word>Tuple</Word>
                    <Word>Callable</Word>
                    <Word>Iterable</Word>
                    <Word>Iterator</Word>
                    <Word>Generator</Word>
                    <Word>Exception</Word>
                    <Word>BaseException</Word>
                    <Word>ValueError</Word>
                    <Word>TypeError</Word>
                    <Word>KeyError</Word>
                    <Word>IndexError</Word>
                </Keywords>

                <!-- Function & Method Invocations (#DCDCAA) -->
                <Rule color="Functions">
                    \b[a-zA-Z_][a-zA-Z0-9_]*(?=\s*\()
                </Rule>

                <!-- Numeric Literals (Hex, Binary, Octal, Floats, Integers) -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+|\b0[bB][01]+|\b0[oO][0-7]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?[jJ]?)
                </Rule>

                <!-- Punctuation & Operators -->
                <Rule color="Punctuation">
                    -&gt;|:=|[?:;.,()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Python syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(PythonXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

