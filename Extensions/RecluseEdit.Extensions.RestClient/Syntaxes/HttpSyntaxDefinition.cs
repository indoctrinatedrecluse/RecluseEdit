using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.RestClient.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for .http and .rest files in Dark+ styling.
/// Supports HTTP methods, request boundaries (###), headers, variables ({{...}}), and inline JSON/body content.
/// </summary>
public static class HttpSyntaxDefinition
{
    private const string HttpXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="HTTP" extensions=".http;.rest" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Method" foreground="#569CD6" fontWeight="bold" />
            <Color name="Boundary" foreground="#C586C0" fontWeight="bold" />
            <Color name="Variable" foreground="#4EC9B0" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="HeaderName" foreground="#9CDCFE" />
            <Color name="Url" foreground="#DCDCAA" />

            <RuleSet ignoreCase="true">
                <!-- Request boundary separator: ### -->
                <Span color="Boundary">
                    <Begin>###</Begin>
                </Span>

                <!-- Single line comments: # and // -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- Variables: {{varName}} -->
                <Span color="Variable">
                    <Begin>\{\{</Begin>
                    <End>\}\}</End>
                </Span>

                <!-- Strings: "..." -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                </Span>

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F]+\b|\b\d+(\.[0-9]+)?\b</Rule>

                <!-- HTTP Methods -->
                <Keywords color="Method">
                    <Word>GET</Word>
                    <Word>POST</Word>
                    <Word>PUT</Word>
                    <Word>DELETE</Word>
                    <Word>PATCH</Word>
                    <Word>HEAD</Word>
                    <Word>OPTIONS</Word>
                    <Word>TRACE</Word>
                    <Word>CONNECT</Word>
                </Keywords>

                <!-- Common HTTP Headers -->
                <Keywords color="HeaderName">
                    <Word>Content-Type</Word>
                    <Word>Accept</Word>
                    <Word>Authorization</Word>
                    <Word>User-Agent</Word>
                    <Word>Host</Word>
                    <Word>Cache-Control</Word>
                    <Word>Cookie</Word>
                    <Word>Set-Cookie</Word>
                    <Word>Origin</Word>
                    <Word>Referer</Word>
                    <Word>X-Requested-With</Word>
                    <Word>X-API-Key</Word>
                    <Word>Bearer</Word>
                </Keywords>

                <!-- URLs -->
                <Rule color="Url">https?://[a-zA-Z0-9\-\._~:/\?#\[\]@!$&amp;'\(\)\*\+,;=%]+</Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(HttpXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
