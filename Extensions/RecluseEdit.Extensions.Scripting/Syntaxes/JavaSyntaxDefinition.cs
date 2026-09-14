using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for Java (.java) files in Dark+ styling,
/// including Spring Boot annotations and standard Java keywords.
/// </summary>
public static class JavaSyntaxDefinition
{
    private const string JavaXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Java" extensions=".java" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Char" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
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

                <!-- Strings -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
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

                <!-- Annotations (including Spring Boot) -->
                <Rule color="Annotation">@[a-zA-Z0-9_]+(\.[a-zA-Z0-9_]+)*</Rule>

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F_]+[lL]?\b|\b\d+[lLfFdD]?\b|\b\d+\.\d+([eE][+-]?\d+)?[fFdD]?\b</Rule>

                <!-- Control Flow -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>else</Word>
                    <Word>switch</Word>
                    <Word>case</Word>
                    <Word>default</Word>
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
                    <Word>yield</Word>
                </Keywords>

                <!-- Java Keywords -->
                <Keywords color="Keywords">
                    <Word>public</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>static</Word>
                    <Word>final</Word>
                    <Word>abstract</Word>
                    <Word>synchronized</Word>
                    <Word>volatile</Word>
                    <Word>transient</Word>
                    <Word>native</Word>
                    <Word>class</Word>
                    <Word>interface</Word>
                    <Word>enum</Word>
                    <Word>record</Word>
                    <Word>extends</Word>
                    <Word>implements</Word>
                    <Word>new</Word>
                    <Word>this</Word>
                    <Word>super</Word>
                    <Word>package</Word>
                    <Word>import</Word>
                    <Word>instanceof</Word>
                    <Word>var</Word>
                    <Word>sealed</Word>
                    <Word>permits</Word>
                    <Word>non-sealed</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Standard Types -->
                <Keywords color="Types">
                    <Word>void</Word>
                    <Word>boolean</Word>
                    <Word>byte</Word>
                    <Word>char</Word>
                    <Word>short</Word>
                    <Word>int</Word>
                    <Word>long</Word>
                    <Word>float</Word>
                    <Word>double</Word>
                    <Word>String</Word>
                    <Word>Object</Word>
                    <Word>List</Word>
                    <Word>Map</Word>
                    <Word>Set</Word>
                    <Word>Optional</Word>
                    <Word>ResponseEntity</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(JavaXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

