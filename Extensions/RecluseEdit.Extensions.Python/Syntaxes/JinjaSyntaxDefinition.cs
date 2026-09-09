using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Python.Syntaxes;

/// <summary>
/// Provides a modern AvalonEdit XML Syntax Highlighting Definition (XSHD) for Jinja2 and Django HTML templates,
/// supporting statements, expressions, filters, and embedded HTML structure.
/// </summary>
public static class JinjaSyntaxDefinition
{
    private const string JinjaXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Jinja" extensions=".jinja;.jinja2;.j2;.html.jinja;.djhtml" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" fontStyle="italic" />
            <Color name="String" foreground="#CE9178" />
            <Color name="TemplateTag" foreground="#C586C0" fontWeight="bold" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Filter" foreground="#DCDCAA" />
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="HtmlTag" foreground="#569CD6" />
            <Color name="HtmlAttribute" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Jinja / Django Comment: {# ... #} -->
                <Span color="Comment" multiline="true">
                    <Begin>\{#</Begin>
                    <End>#\}</End>
                </Span>

                <!-- HTML Comment -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- Jinja Statement: {% ... %} -->
                <Span multiline="true">
                    <Begin>\{%</Begin>
                    <End>%\}</End>
                    <RuleSet>
                        <Span color="String">
                            <Begin>&quot;</Begin>
                            <End>&quot;</End>
                        </Span>
                        <Span color="String">
                            <Begin>'</Begin>
                            <End>'</End>
                        </Span>
                        <Keywords color="Keywords">
                            <Word>for</Word>
                            <Word>endfor</Word>
                            <Word>in</Word>
                            <Word>if</Word>
                            <Word>elif</Word>
                            <Word>else</Word>
                            <Word>endif</Word>
                            <Word>block</Word>
                            <Word>endblock</Word>
                            <Word>extends</Word>
                            <Word>include</Word>
                            <Word>macro</Word>
                            <Word>endmacro</Word>
                            <Word>call</Word>
                            <Word>endcall</Word>
                            <Word>filter</Word>
                            <Word>endfilter</Word>
                            <Word>set</Word>
                            <Word>endset</Word>
                            <Word>raw</Word>
                            <Word>endraw</Word>
                            <Word>with</Word>
                            <Word>endwith</Word>
                            <Word>trans</Word>
                            <Word>endtrans</Word>
                            <Word>csrf_token</Word>
                            <Word>static</Word>
                            <Word>url</Word>
                            <Word>load</Word>
                            <Word>autoescape</Word>
                            <Word>endautoescape</Word>
                            <Word>cycle</Word>
                            <Word>empty</Word>
                            <Word>firstof</Word>
                            <Word>regroup</Word>
                            <Word>now</Word>
                            <Word>and</Word>
                            <Word>or</Word>
                            <Word>not</Word>
                            <Word>is</Word>
                            <Word>defined</Word>
                            <Word>undefined</Word>
                            <Word>true</Word>
                            <Word>false</Word>
                            <Word>none</Word>
                            <Word>True</Word>
                            <Word>False</Word>
                            <Word>None</Word>
                        </Keywords>
                        <!-- Filter pipe: |filter_name -->
                        <Rule color="Filter">
                            (?&lt;=\|)\s*[a-zA-Z_][a-zA-Z0-9_]*
                        </Rule>
                        <Rule color="Variable">
                            \b[a-zA-Z_][a-zA-Z0-9_\.]*\b
                        </Rule>
                        <Rule color="Digits">
                            \b\d+(\.[0-9]+)?\b
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Jinja Expression: {{ ... }} -->
                <Span multiline="true">
                    <Begin>\{\{</Begin>
                    <End>\}\}</End>
                    <RuleSet>
                        <Span color="String">
                            <Begin>&quot;</Begin>
                            <End>&quot;</End>
                        </Span>
                        <Span color="String">
                            <Begin>'</Begin>
                            <End>'</End>
                        </Span>
                        <!-- Filter pipe: |filter_name -->
                        <Rule color="Filter">
                            (?&lt;=\|)\s*[a-zA-Z_][a-zA-Z0-9_]*
                        </Rule>
                        <Rule color="Variable">
                            \b[a-zA-Z_][a-zA-Z0-9_\.]*\b
                        </Rule>
                        <Rule color="Digits">
                            \b\d+(\.[0-9]+)?\b
                        </Rule>
                        <Rule color="Punctuation">
                            [?:;.,()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Double Quoted Strings -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                </Span>

                <!-- Single Quoted Strings -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                </Span>

                <!-- Standard HTML Closing Tags (</tag>) -->
                <Rule color="HtmlTag">
                    (?&lt;=&lt;/)[a-zA-Z0-9_\-]+(?=&gt;)
                </Rule>

                <!-- Standard HTML Opening Tags (<tag) -->
                <Rule color="HtmlTag">
                    (?&lt;=&lt;)[a-zA-Z0-9_\-]+(?=[\s/&gt;])
                </Rule>

                <!-- Standard HTML Attributes (class, id, type, etc.) -->
                <Rule color="HtmlAttribute">
                    \b[a-zA-Z_:][-a-zA-Z0-9_:.]*(?=\s*=)
                </Rule>

                <!-- Tag Brackets & Punctuation -->
                <Rule color="Punctuation">
                    &lt;/|/&gt;|&lt;|&gt;|[?:;=]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Jinja/Django template syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(JinjaXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

