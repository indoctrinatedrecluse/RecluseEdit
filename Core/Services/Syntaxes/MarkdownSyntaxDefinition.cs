using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Core.Services.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Markdown documents,
/// including headings, bold/italic, code spans, fenced code blocks, blockquotes, lists, and links.
/// </summary>
public static class MarkdownSyntaxDefinition
{
    private const string MarkdownXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Markdown" extensions=".md;.markdown" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Heading1" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Heading2" foreground="#569CD6" fontWeight="bold" />
            <Color name="Heading3" foreground="#9CDCFE" fontWeight="bold" />
            <Color name="Heading4" foreground="#DCDCAA" fontWeight="bold" />
            <Color name="Heading5" foreground="#C586C0" fontWeight="bold" />
            <Color name="Heading6" foreground="#CE9178" fontWeight="bold" />
            <Color name="Bold" foreground="#FFFFFF" fontWeight="bold" />
            <Color name="Italic" foreground="#CE9178" fontStyle="italic" />
            <Color name="Code" foreground="#CE9178" />
            <Color name="CodeBlock" foreground="#D4D4D4" />
            <Color name="Blockquote" foreground="#6A9955" fontStyle="italic" />
            <Color name="Link" foreground="#569CD6" />
            <Color name="LinkUrl" foreground="#9CDCFE" />
            <Color name="ListBullet" foreground="#569CD6" fontWeight="bold" />
            <Color name="HorizontalRule" foreground="#6A9955" />
            <Color name="Table" foreground="#808080" />
            <Color name="HtmlTag" foreground="#569CD6" />

            <RuleSet>
                <!-- Fenced Code Blocks (``` ... ```) -->
                <Span color="CodeBlock" multiline="true">
                    <Begin>```</Begin>
                    <End>```</End>
                </Span>

                <!-- Inline Code (`code`) -->
                <Span color="Code">
                    <Begin>`</Begin>
                    <End>`</End>
                </Span>

                <!-- Headings (H1 to H6) -->
                <Rule color="Heading1">
                    ^#\s+.*$
                </Rule>
                <Rule color="Heading2">
                    ^##\s+.*$
                </Rule>
                <Rule color="Heading3">
                    ^###\s+.*$
                </Rule>
                <Rule color="Heading4">
                    ^####\s+.*$
                </Rule>
                <Rule color="Heading5">
                    ^#####\s+.*$
                </Rule>
                <Rule color="Heading6">
                    ^######\s+.*$
                </Rule>

                <!-- Horizontal Rules -->
                <Rule color="HorizontalRule">
                    ^\s*(\*\s*){3,}$|^\s*(-\s*){3,}$|^\s*(_\s*){3,}$
                </Rule>

                <!-- Blockquotes (> quote) -->
                <Span color="Blockquote">
                    <Begin>^&gt;</Begin>
                </Span>

                <!-- Bold & Italic (***text***) -->
                <Span color="Bold" fontWeight="bold" fontStyle="italic">
                    <Begin>\*\*\*</Begin>
                    <End>\*\*\*</End>
                </Span>

                <!-- Bold (**text** or __text__) -->
                <Span color="Bold">
                    <Begin>\*\*</Begin>
                    <End>\*\*</End>
                </Span>
                <Span color="Bold">
                    <Begin>(?&lt;!\w)__(?!\s)</Begin>
                    <End>(?&lt;!\s)__(?!\w)</End>
                </Span>

                <!-- Italic (*text* or _text_) -->
                <Span color="Italic">
                    <Begin>\*</Begin>
                    <End>\*</End>
                </Span>
                <Span color="Italic">
                    <Begin>(?&lt;!\w)_(?!\s)</Begin>
                    <End>(?&lt;!\s)_(?!\w)</End>
                </Span>

                <!-- Links and Images ([text](url) or ![alt](url)) -->
                <Rule color="Link">
                    !?\[[^\]]*\](?=\([^\)]+\))
                </Rule>
                <Rule color="LinkUrl">
                    (?&lt;=\[[^\]]*\])\([^\)]+\)
                </Rule>

                <!-- Unordered Lists (*, -, +) -->
                <Rule color="ListBullet">
                    ^\s*[\*\-\+]\s+
                </Rule>

                <!-- Ordered Lists (1., 2., etc.) -->
                <Rule color="ListBullet">
                    ^\s*\d+\.\s+
                </Rule>

                <!-- Table Dividers -->
                <Rule color="Table">
                    ^\s*\|[-:\s|]+\|\s*$
                </Rule>

                <!-- Inline HTML Tags -->
                <Span color="HtmlTag">
                    <Begin>&lt;[a-zA-Z0-9_\-]+</Begin>
                    <End>&gt;</End>
                </Span>
                <Span color="HtmlTag">
                    <Begin>&lt;/[a-zA-Z0-9_\-]+&gt;</Begin>
                </Span>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Markdown syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(MarkdownXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}

