using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Frontend.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Astro components (.astro),
/// supporting code fences (---), component script, template markup, client directives, and scoped styles.
/// </summary>
public static class AstroSyntaxDefinition
{
    private const string AstroXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Astro" extensions=".astro" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="FrontmatterFence" foreground="#FF5D01" fontWeight="bold" />
            <Color name="AstroDirectives" foreground="#C586C0" fontWeight="bold" />
            <Color name="AstroGlobals" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Tags" foreground="#569CD6" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Interpolation" foreground="#DCDCAA" />

            <RuleSet ignoreCase="false">
                <!-- Frontmatter Code Fence -->
                <Span color="FrontmatterFence">
                    <Begin>^---</Begin>
                    <End>^---</End>
                </Span>

                <!-- HTML comments -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- JS comments -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Strings -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                </Span>
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                </Span>
                <Span color="String" multiline="true">
                    <Begin>`</Begin>
                    <End>`</End>
                </Span>

                <!-- Astro Directives: client:..., is:..., set:... -->
                <Rule color="AstroDirectives">\b(client:(load|idle|visible|media|only)|is:(raw|inline)|set:(html|text))\b</Rule>

                <!-- Astro Globals -->
                <Keywords color="AstroGlobals">
                    <Word>Astro</Word>
                    <Word>props</Word>
                    <Word>params</Word>
                    <Word>request</Word>
                    <Word>response</Word>
                    <Word>redirect</Word>
                    <Word>cookies</Word>
                    <Word>slots</Word>
                    <Word>site</Word>
                    <Word>generator</Word>
                </Keywords>

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F]+\b|\b\d+(\.[0-9]+)?\b</Rule>

                <!-- Standard JS/TS Keywords -->
                <Keywords color="Keywords">
                    <Word>import</Word>
                    <Word>export</Word>
                    <Word>from</Word>
                    <Word>default</Word>
                    <Word>const</Word>
                    <Word>let</Word>
                    <Word>var</Word>
                    <Word>function</Word>
                    <Word>return</Word>
                    <Word>async</Word>
                    <Word>await</Word>
                    <Word>if</Word>
                    <Word>else</Word>
                    <Word>for</Word>
                    <Word>while</Word>
                    <Word>switch</Word>
                    <Word>case</Word>
                    <Word>break</Word>
                    <Word>try</Word>
                    <Word>catch</Word>
                    <Word>finally</Word>
                    <Word>throw</Word>
                    <Word>new</Word>
                    <Word>class</Word>
                    <Word>interface</Word>
                    <Word>type</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                    <Word>undefined</Word>
                </Keywords>

                <!-- Tags -->
                <Rule color="Tags">&lt;/?(slot|style|script|[A-Z][a-zA-Z0-9_]*|[a-z0-9_\-]+)&gt;?</Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(AstroXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

