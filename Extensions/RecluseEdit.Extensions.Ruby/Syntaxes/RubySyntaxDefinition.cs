using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Ruby.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for the Ruby programming language,
/// including Ruby keywords, symbols, instance/class variables, string interpolation, and block syntax.
/// </summary>
public static class RubySyntaxDefinition
{
    private const string RubyXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Ruby" extensions=".rb;.rake;.gemspec;.ru" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="RailsMacro" foreground="#C586C0" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Symbol" foreground="#9CDCFE" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />
            <Color name="Regex" foreground="#D16969" />

            <RuleSet>
                <!-- Rails Macros & Helpers (Magenta #C586C0) -->
                <Keywords color="RailsMacro">
                    <Word>has_many</Word>
                    <Word>belongs_to</Word>
                    <Word>has_one</Word>
                    <Word>has_and_belongs_to_many</Word>
                    <Word>validates</Word>
                    <Word>validate</Word>
                    <Word>before_action</Word>
                    <Word>after_action</Word>
                    <Word>around_action</Word>
                    <Word>scope</Word>
                    <Word>resources</Word>
                    <Word>resource</Word>
                    <Word>root</Word>
                    <Word>namespace</Word>
                </Keywords>

                <!-- Method Invocations (Yellow #DCDCAA) -->
                <Rule color="Functions">
                    \b[a-zA-Z_][a-zA-Z0-9_]*[?!]?(?=\s*\()
                </Rule>
                <!-- Multiline Comment (=begin ... =end) -->
                <Span color="Comment" multiline="true">
                    <Begin>^=begin</Begin>
                    <End>^=end</End>
                </Span>

                <!-- Single Line Comment (#) -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Double Quoted Strings with Interpolation -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\&quot;" end="" />
                        <Span begin="\\\\" end="" />
                        <!-- #{expression} interpolation inside strings -->
                        <Span color="Variable" multiline="false">
                            <Begin>#\{</Begin>
                            <End>\}</End>
                        </Span>
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

                <!-- Percent Strings (%w, %i, %q, %Q) -->
                <Span color="String">
                    <Begin>%[wiqQ]?\[</Begin>
                    <End>\]</End>
                </Span>
                <Span color="String">
                    <Begin>%[wiqQ]?\{</Begin>
                    <End>\}</End>
                </Span>
                <Span color="String">
                    <Begin>%[wiqQ]?\(</Begin>
                    <End>\)</End>
                </Span>

                <!-- Symbols (:symbol or key:) -->
                <Rule color="Symbol">
                    (?&lt;!\w):[a-zA-Z_][a-zA-Z0-9_]*[?!]?|[a-zA-Z_][a-zA-Z0-9_]*:(?=\s)
                </Rule>

                <!-- Instance Variables (@var) and Class Variables (@@var) -->
                <Rule color="Variable">
                    @@?[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Global Variables ($var) -->
                <Rule color="Variable">
                    \$[a-zA-Z_0-9]+
                </Rule>

                <!-- Keywords -->
                <Keywords color="Keywords">
                    <Word>alias</Word>
                    <Word>and</Word>
                    <Word>begin</Word>
                    <Word>break</Word>
                    <Word>case</Word>
                    <Word>class</Word>
                    <Word>def</Word>
                    <Word>defined?</Word>
                    <Word>do</Word>
                    <Word>else</Word>
                    <Word>elsif</Word>
                    <Word>end</Word>
                    <Word>ensure</Word>
                    <Word>false</Word>
                    <Word>for</Word>
                    <Word>if</Word>
                    <Word>in</Word>
                    <Word>module</Word>
                    <Word>next</Word>
                    <Word>nil</Word>
                    <Word>not</Word>
                    <Word>or</Word>
                    <Word>redo</Word>
                    <Word>rescue</Word>
                    <Word>retry</Word>
                    <Word>return</Word>
                    <Word>self</Word>
                    <Word>super</Word>
                    <Word>then</Word>
                    <Word>true</Word>
                    <Word>undef</Word>
                    <Word>unless</Word>
                    <Word>until</Word>
                    <Word>when</Word>
                    <Word>while</Word>
                    <Word>yield</Word>
                    <Word>attr_accessor</Word>
                    <Word>attr_reader</Word>
                    <Word>attr_writer</Word>
                    <Word>include</Word>
                    <Word>extend</Word>
                    <Word>prepend</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>public</Word>
                    <Word>require</Word>
                    <Word>require_relative</Word>
                    <Word>raise</Word>
                    <Word>fail</Word>
                </Keywords>

                <!-- Core Ruby & Rails Types -->
                <Keywords color="Types">
                    <Word>Object</Word>
                    <Word>Class</Word>
                    <Word>Module</Word>
                    <Word>String</Word>
                    <Word>Integer</Word>
                    <Word>Float</Word>
                    <Word>Array</Word>
                    <Word>Hash</Word>
                    <Word>Symbol</Word>
                    <Word>Range</Word>
                    <Word>Regexp</Word>
                    <Word>Proc</Word>
                    <Word>Lambda</Word>
                    <Word>Time</Word>
                    <Word>Date</Word>
                    <Word>DateTime</Word>
                    <Word>StandardError</Word>
                    <Word>Exception</Word>
                    <Word>ApplicationRecord</Word>
                    <Word>ApplicationController</Word>
                    <Word>ActiveRecord</Word>
                    <Word>ActionController</Word>
                </Keywords>

                <!-- Digits -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+|\b0[bB][01]+|(\b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?)
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [?,.;:()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Ruby syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(RubyXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

