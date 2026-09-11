using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Rust,
/// featuring lifetimes, macros, attributes, raw strings, standard types, and Dark+ theme styling.
/// </summary>
public static class RustSyntaxDefinition
{
    private const string RustXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Rust" extensions=".rs" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Char" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Macros" foreground="#DCDCAA" />
            <Color name="Attributes" foreground="#9CDCFE" />
            <Color name="Lifetimes" foreground="#4EC9B0" fontStyle="italic" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="DocComment" foreground="#608B4E" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Doc Comments (/// or //!) -->
                <Span color="DocComment">
                    <Begin>///(?!/)</Begin>
                </Span>
                <Span color="DocComment">
                    <Begin>//!(?!/)</Begin>
                </Span>

                <!-- Single Line Comment -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Multi-Line Comment -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Attributes / Derives e.g. #[derive(Debug)] or #![no_std] -->
                <Span color="Attributes" multiline="true">
                    <Begin>#!?\[</Begin>
                    <End>\]</End>
                </Span>

                <!-- Raw Strings e.g. r#"hello"# or r"hello" -->
                <Span color="String" multiline="true">
                    <Begin>r#*&quot;</Begin>
                    <End>&quot;#*</End>
                </Span>

                <!-- Byte Strings e.g. b"..." -->
                <Span color="String">
                    <Begin>b&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Normal String Literals -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Character Literals -->
                <Span color="Char">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Macros e.g. println!, vec!, format!, panic! -->
                <Rule color="Macros">
                    \b[a-zA-Z_][a-zA-Z0-9_]*!
                </Rule>

                <!-- Lifetimes e.g. 'a, 'static, 'de -->
                <Rule color="Lifetimes">
                    '[a-zA-Z_][a-zA-Z0-9_]*\b
                </Rule>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>break</Word>
                    <Word>continue</Word>
                    <Word>else</Word>
                    <Word>for</Word>
                    <Word>if</Word>
                    <Word>in</Word>
                    <Word>loop</Word>
                    <Word>match</Word>
                    <Word>return</Word>
                    <Word>while</Word>
                    <Word>yield</Word>
                </Keywords>

                <!-- Core Keywords (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>as</Word>
                    <Word>async</Word>
                    <Word>await</Word>
                    <Word>const</Word>
                    <Word>crate</Word>
                    <Word>dyn</Word>
                    <Word>enum</Word>
                    <Word>extern</Word>
                    <Word>fn</Word>
                    <Word>impl</Word>
                    <Word>let</Word>
                    <Word>mod</Word>
                    <Word>move</Word>
                    <Word>mut</Word>
                    <Word>pub</Word>
                    <Word>ref</Word>
                    <Word>self</Word>
                    <Word>Self</Word>
                    <Word>static</Word>
                    <Word>struct</Word>
                    <Word>super</Word>
                    <Word>trait</Word>
                    <Word>type</Word>
                    <Word>union</Word>
                    <Word>unsafe</Word>
                    <Word>use</Word>
                    <Word>where</Word>
                </Keywords>

                <!-- Standard Primitive & Common Types (#4EC9B0) -->
                <Keywords color="Types">
                    <Word>bool</Word>
                    <Word>char</Word>
                    <Word>str</Word>
                    <Word>u8</Word>
                    <Word>u16</Word>
                    <Word>u32</Word>
                    <Word>u64</Word>
                    <Word>u128</Word>
                    <Word>usize</Word>
                    <Word>i8</Word>
                    <Word>i16</Word>
                    <Word>i32</Word>
                    <Word>i64</Word>
                    <Word>i128</Word>
                    <Word>isize</Word>
                    <Word>f32</Word>
                    <Word>f64</Word>
                    <Word>String</Word>
                    <Word>Option</Word>
                    <Word>Result</Word>
                    <Word>Some</Word>
                    <Word>None</Word>
                    <Word>Ok</Word>
                    <Word>Err</Word>
                    <Word>Vec</Word>
                    <Word>Box</Word>
                    <Word>Rc</Word>
                    <Word>Arc</Word>
                    <Word>RefCell</Word>
                    <Word>Cell</Word>
                    <Word>HashMap</Word>
                    <Word>HashSet</Word>
                    <Word>BTreeMap</Word>
                    <Word>BTreeSet</Word>
                    <Word>Default</Word>
                    <Word>Clone</Word>
                    <Word>Copy</Word>
                    <Word>Debug</Word>
                    <Word>Display</Word>
                    <Word>Send</Word>
                    <Word>Sync</Word>
                    <Word>From</Word>
                    <Word>Into</Word>
                </Keywords>

                <!-- Constants (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>true</Word>
                    <Word>false</Word>
                </Keywords>

                <!-- Rust Operators -->
                <Rule color="Operators">
                    (::|-&gt;|=&gt;|\.\.=?|\?|&amp;&amp;|\|\||!=|==|&lt;=|&gt;=)
                </Rule>

                <!-- Digits with optional type suffixes e.g. 100u32, 0.5f64 -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+(_[0-9a-fA-F]+)*([iu](8|16|32|64|128|size))?\b |
                    \b0[bB][01]+(_[01]+)*([iu](8|16|32|64|128|size))?\b |
                    \b0[oO][0-7]+(_[0-7]+)*([iu](8|16|32|64|128|size))?\b |
                    \b\d+(_\d+)*(\.\d+(_\d+)*)?([eE][+-]?\d+(_\d+)*)?(f32|f64|[iu](8|16|32|64|128|size))?\b
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
        using var reader = new StringReader(RustXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

