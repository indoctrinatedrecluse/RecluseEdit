using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for PowerShell,
/// featuring Verb-Noun cmdlets, variables ($env:...), parameters (-Name), type accelerators ([string]),
/// comparison operators (-eq, -match), and Dark+ theme styling.
/// </summary>
public static class PowerShellSyntaxDefinition
{
    private const string PowerShellXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="PowerShell" extensions=".ps1;.psm1;.psd1" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Variables" foreground="#9CDCFE" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Cmdlets" foreground="#DCDCAA" />
            <Color name="Parameters" foreground="#9CDCFE" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet ignoreCase="true">
                <!-- Multiline Block Comments (<# ... #>) -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;#</Begin>
                    <End>#&gt;</End>
                </Span>

                <!-- Single Line Comment (#) -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Double-quoted Strings with Variable Expansion -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="`" end="." />
                        <Rule color="Variables">
                            \$[a-zA-Z0-9_:]+ |
                            \$\{[^}]+\}
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Single-quoted Strings (Verbatim) -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="''" end="" />
                    </RuleSet>
                </Span>

                <!-- Type Accelerators e.g. [string], [int], [hashtable], [PSCustomObject] -->
                <Rule color="Types">
                    \[[a-zA-Z0-9_\.\[\]]+\]
                </Rule>

                <!-- Variables ($var, $env:PATH, $script:X, $true, $false, $null, $_) -->
                <Rule color="Variables">
                    \$[a-zA-Z0-9_:]+ |
                    \$\{[^}]+\} |
                    \$(?=[?_\^])
                </Rule>

                <!-- Parameters e.g. -Path, -Force, -Recurse -->
                <Rule color="Parameters">
                    -[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Standard Verb-Noun Cmdlets -->
                <Rule color="Cmdlets">
                    \b(Get|Set|New|Remove|Start|Stop|Restart|Invoke|Write|Read|Enable|Disable|Test|Clear|Find|Install|Update|Select|Where|Sort|Group|Measure|Out|Export|Import|ConvertFrom|ConvertTo|Wait|Add|Format|Show|Reset|Register|Unregister|Resolve|Join|Split|Expand|Measure|Compare|Push|Pop)-[a-zA-Z0-9_]+\b
                </Rule>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>break</Word>
                    <Word>catch</Word>
                    <Word>continue</Word>
                    <Word>do</Word>
                    <Word>else</Word>
                    <Word>elseif</Word>
                    <Word>finally</Word>
                    <Word>for</Word>
                    <Word>foreach</Word>
                    <Word>if</Word>
                    <Word>in</Word>
                    <Word>return</Word>
                    <Word>switch</Word>
                    <Word>throw</Word>
                    <Word>trap</Word>
                    <Word>try</Word>
                    <Word>until</Word>
                    <Word>while</Word>
                </Keywords>

                <!-- Core Keywords (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>class</Word>
                    <Word>configuration</Word>
                    <Word>data</Word>
                    <Word>dynamicparam</Word>
                    <Word>enum</Word>
                    <Word>filter</Word>
                    <Word>function</Word>
                    <Word>hidden</Word>
                    <Word>inlinescript</Word>
                    <Word>module</Word>
                    <Word>param</Word>
                    <Word>parallel</Word>
                    <Word>sequence</Word>
                    <Word>using</Word>
                    <Word>workflow</Word>
                </Keywords>

                <!-- Comparison & Logical Operators -->
                <Keywords color="Operators">
                    <Word>-and</Word>
                    <Word>-as</Word>
                    <Word>-band</Word>
                    <Word>-bnot</Word>
                    <Word>-bor</Word>
                    <Word>-bxor</Word>
                    <Word>-ccontains</Word>
                    <Word>-ceq</Word>
                    <Word>-cge</Word>
                    <Word>-cgt</Word>
                    <Word>-cin</Word>
                    <Word>-cle</Word>
                    <Word>-clike</Word>
                    <Word>-clt</Word>
                    <Word>-cmatch</Word>
                    <Word>-cne</Word>
                    <Word>-cnotcontains</Word>
                    <Word>-cnotin</Word>
                    <Word>-cnotlike</Word>
                    <Word>-cnotmatch</Word>
                    <Word>-contains</Word>
                    <Word>-creplace</Word>
                    <Word>-csplit</Word>
                    <Word>-eq</Word>
                    <Word>-ge</Word>
                    <Word>-gt</Word>
                    <Word>-icontains</Word>
                    <Word>-ieq</Word>
                    <Word>-ige</Word>
                    <Word>-igt</Word>
                    <Word>-iin</Word>
                    <Word>-ile</Word>
                    <Word>-ilike</Word>
                    <Word>-ilt</Word>
                    <Word>-imatch</Word>
                    <Word>-in</Word>
                    <Word>-ine</Word>
                    <Word>-inotcontains</Word>
                    <Word>-inotin</Word>
                    <Word>-inotlike</Word>
                    <Word>-inotmatch</Word>
                    <Word>-ireplace</Word>
                    <Word>-is</Word>
                    <Word>-isnot</Word>
                    <Word>-isplit</Word>
                    <Word>-join</Word>
                    <Word>-le</Word>
                    <Word>-like</Word>
                    <Word>-lt</Word>
                    <Word>-match</Word>
                    <Word>-ne</Word>
                    <Word>-not</Word>
                    <Word>-notcontains</Word>
                    <Word>-notin</Word>
                    <Word>-notlike</Word>
                    <Word>-notmatch</Word>
                    <Word>-or</Word>
                    <Word>-replace</Word>
                    <Word>-shl</Word>
                    <Word>-shr</Word>
                    <Word>-split</Word>
                    <Word>-xor</Word>
                </Keywords>

                <!-- Pipe & Redirection Operators -->
                <Rule color="Operators">
                    (\||\+=|-=|\*=|/=|%=|&amp;|&gt;&gt;|&gt;|&lt;)
                </Rule>

                <!-- Digits (hex, kb/mb/gb multipliers) -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+(kb|mb|gb|tb|pb)?\b |
                    \b\d+(\.\d+)?(kb|mb|gb|tb|pb)?\b
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [{}\(\)\[\];,:@]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = new StringReader(PowerShellXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

