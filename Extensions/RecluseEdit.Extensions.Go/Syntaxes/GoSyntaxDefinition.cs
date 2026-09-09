using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Go.Syntaxes;

/// <summary>
/// Provides a comprehensive AvalonEdit XML Syntax Highlighting Definition (XSHD) for modern Go (1.23+),
/// including raw backtick strings, struct tags, goroutines/channels, built-in functions, and Dark+ theme colorization.
/// </summary>
public static class GoSyntaxDefinition
{
    private const string GoXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Go" extensions=".go" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Rune" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Builtins" foreground="#DCDCAA" />
            <Color name="Constants" foreground="#569CD6" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Punctuation" foreground="#D4D4D4" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="StructTagKey" foreground="#9CDCFE" fontWeight="bold" />
            <Color name="StructTagVal" foreground="#CE9178" />

            <RuleSet>
                <!-- Single Line Comment -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Multi-Line Comment -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Raw String Literals (backtick multiline strings with struct tag highlighting) -->
                <Span color="String" multiline="true">
                    <Begin>`</Begin>
                    <End>`</End>
                    <RuleSet>
                        <!-- Struct Tag Key-Value pairs e.g. json:"id" db:"user_id" binding:"required" -->
                        <Rule color="StructTagKey">
                            [a-zA-Z0-9_\-]+(?=:)
                        </Rule>
                        <Span color="StructTagVal">
                            <Begin>&quot;</Begin>
                            <End>&quot;</End>
                        </Span>
                    </RuleSet>
                </Span>

                <!-- Interpreted String Literals ("...") -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Rune Literals ('...') -->
                <Span color="Rune">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>break</Word>
                    <Word>case</Word>
                    <Word>continue</Word>
                    <Word>default</Word>
                    <Word>else</Word>
                    <Word>fallthrough</Word>
                    <Word>for</Word>
                    <Word>goto</Word>
                    <Word>if</Word>
                    <Word>range</Word>
                    <Word>return</Word>
                    <Word>select</Word>
                    <Word>switch</Word>
                </Keywords>

                <!-- Declaration & Concurrency Keywords (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>chan</Word>
                    <Word>const</Word>
                    <Word>defer</Word>
                    <Word>func</Word>
                    <Word>go</Word>
                    <Word>import</Word>
                    <Word>interface</Word>
                    <Word>map</Word>
                    <Word>package</Word>
                    <Word>struct</Word>
                    <Word>type</Word>
                    <Word>var</Word>
                </Keywords>

                <!-- Builtin Primitive & Generic Types (#4EC9B0) -->
                <Keywords color="Types">
                    <Word>any</Word>
                    <Word>bool</Word>
                    <Word>byte</Word>
                    <Word>comparable</Word>
                    <Word>complex64</Word>
                    <Word>complex128</Word>
                    <Word>error</Word>
                    <Word>float32</Word>
                    <Word>float64</Word>
                    <Word>int</Word>
                    <Word>int8</Word>
                    <Word>int16</Word>
                    <Word>int32</Word>
                    <Word>int64</Word>
                    <Word>rune</Word>
                    <Word>string</Word>
                    <Word>uint</Word>
                    <Word>uint8</Word>
                    <Word>uint16</Word>
                    <Word>uint32</Word>
                    <Word>uint64</Word>
                    <Word>uintptr</Word>
                </Keywords>

                <!-- Builtin Functions (#DCDCAA) -->
                <Keywords color="Builtins">
                    <Word>append</Word>
                    <Word>cap</Word>
                    <Word>clear</Word>
                    <Word>close</Word>
                    <Word>complex</Word>
                    <Word>copy</Word>
                    <Word>delete</Word>
                    <Word>imag</Word>
                    <Word>len</Word>
                    <Word>make</Word>
                    <Word>max</Word>
                    <Word>min</Word>
                    <Word>new</Word>
                    <Word>panic</Word>
                    <Word>print</Word>
                    <Word>println</Word>
                    <Word>real</Word>
                    <Word>recover</Word>
                </Keywords>

                <!-- Constants & Identifiers (#569CD6) -->
                <Keywords color="Constants">
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>iota</Word>
                    <Word>nil</Word>
                </Keywords>

                <!-- Go Special Operators (:=, <-, ...) -->
                <Rule color="Operators">
                    (:=|&lt;-|\.{3})
                </Rule>

                <!-- Digits -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+(_[0-9a-fA-F]+)*\b |
                    \b0[bB][01]+(_[01]+)*\b |
                    \b0[oO]?[0-7]+(_[0-7]+)*\b |
                    \b\d+(_\d+)*(\.\d+(_\d+)*)?([eE][+-]?\d+(_\d+)*)?i?\b
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [{}\(\)\[\];,]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = new StringReader(GoXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
