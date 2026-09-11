using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Bash &amp; POSIX Shell,
/// featuring shebang lines, variable expansions ($VAR, ${VAR:-...}), shell builtins,
/// common Unix utilities, test brackets [[ ]], and Dark+ theme styling.
/// </summary>
public static class BashSyntaxDefinition
{
    private const string BashXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Bash" extensions=".sh;.bash;.zsh;.ksh;.command" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Variables" foreground="#9CDCFE" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Builtins" foreground="#DCDCAA" />
            <Color name="Commands" foreground="#4EC9B0" />
            <Color name="Shebang" foreground="#608B4E" fontWeight="bold" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Shebang Line (#!/bin/bash or #!/usr/bin/env ...) -->
                <Span color="Shebang">
                    <Begin>^#!</Begin>
                    <End>$</End>
                </Span>

                <!-- Single Line Comment -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Double-quoted Strings with Variable Expansion -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                        <Rule color="Variables">
                            \$[a-zA-Z_][a-zA-Z0-9_]* |
                            \$\{[^}]+\} |
                            \$[@*#?$!0-9]
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Single-quoted Strings (Literal / Verbatim) -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                </Span>

                <!-- Backtick Command Substitution (`...`) -->
                <Span color="Builtins">
                    <Begin>`</Begin>
                    <End>`</End>
                </Span>

                <!-- Variables ($VAR, ${VAR:-default}, $1, $@, $?, $$) -->
                <Rule color="Variables">
                    \$[a-zA-Z_][a-zA-Z0-9_]* |
                    \$\{[^}]+\} |
                    \$[@*#?$!0-9]
                </Rule>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>case</Word>
                    <Word>do</Word>
                    <Word>done</Word>
                    <Word>elif</Word>
                    <Word>else</Word>
                    <Word>esac</Word>
                    <Word>fi</Word>
                    <Word>for</Word>
                    <Word>if</Word>
                    <Word>in</Word>
                    <Word>select</Word>
                    <Word>then</Word>
                    <Word>time</Word>
                    <Word>until</Word>
                    <Word>while</Word>
                </Keywords>

                <!-- Declaration Keywords (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>function</Word>
                </Keywords>

                <!-- Shell Builtin Commands (#DCDCAA) -->
                <Keywords color="Builtins">
                    <Word>alias</Word>
                    <Word>bg</Word>
                    <Word>bind</Word>
                    <Word>break</Word>
                    <Word>builtin</Word>
                    <Word>cd</Word>
                    <Word>command</Word>
                    <Word>continue</Word>
                    <Word>declare</Word>
                    <Word>dirs</Word>
                    <Word>disown</Word>
                    <Word>echo</Word>
                    <Word>enable</Word>
                    <Word>eval</Word>
                    <Word>exec</Word>
                    <Word>exit</Word>
                    <Word>export</Word>
                    <Word>fc</Word>
                    <Word>fg</Word>
                    <Word>getopts</Word>
                    <Word>hash</Word>
                    <Word>help</Word>
                    <Word>history</Word>
                    <Word>jobs</Word>
                    <Word>kill</Word>
                    <Word>let</Word>
                    <Word>local</Word>
                    <Word>popd</Word>
                    <Word>printf</Word>
                    <Word>pushd</Word>
                    <Word>pwd</Word>
                    <Word>read</Word>
                    <Word>readonly</Word>
                    <Word>return</Word>
                    <Word>set</Word>
                    <Word>shift</Word>
                    <Word>shopt</Word>
                    <Word>source</Word>
                    <Word>suspend</Word>
                    <Word>test</Word>
                    <Word>times</Word>
                    <Word>trap</Word>
                    <Word>type</Word>
                    <Word>typeset</Word>
                    <Word>ulimit</Word>
                    <Word>umask</Word>
                    <Word>unalias</Word>
                    <Word>unset</Word>
                    <Word>wait</Word>
                </Keywords>

                <!-- Common Unix CLI Utilities (#4EC9B0) -->
                <Keywords color="Commands">
                    <Word>awk</Word>
                    <Word>basename</Word>
                    <Word>cat</Word>
                    <Word>chmod</Word>
                    <Word>chown</Word>
                    <Word>cp</Word>
                    <Word>curl</Word>
                    <Word>cut</Word>
                    <Word>diff</Word>
                    <Word>dirname</Word>
                    <Word>env</Word>
                    <Word>find</Word>
                    <Word>git</Word>
                    <Word>grep</Word>
                    <Word>gzip</Word>
                    <Word>head</Word>
                    <Word>ln</Word>
                    <Word>ls</Word>
                    <Word>mkdir</Word>
                    <Word>mv</Word>
                    <Word>patch</Word>
                    <Word>rm</Word>
                    <Word>sed</Word>
                    <Word>sort</Word>
                    <Word>ssh</Word>
                    <Word>sudo</Word>
                    <Word>tail</Word>
                    <Word>tar</Word>
                    <Word>tee</Word>
                    <Word>touch</Word>
                    <Word>uniq</Word>
                    <Word>wc</Word>
                    <Word>wget</Word>
                    <Word>which</Word>
                    <Word>xargs</Word>
                </Keywords>

                <!-- Shell Operators & Redirection -->
                <Rule color="Operators">
                    (&amp;&amp;|\|\||&gt;&gt;|&gt;|&lt;&lt;&lt;|&lt;&lt;|&lt;|2&gt;&amp;1|2&gt;|&amp;&gt;|;;|;|\||==|!=|=~|=)
                </Rule>

                <!-- Test Conditional Brackets -->
                <Rule color="ControlFlow">
                    (\[\[|\]\]|\[|\])
                </Rule>

                <!-- Digits -->
                <Rule color="Digits">
                    \b\d+\b
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [{}\(\);,]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = new StringReader(BashXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

