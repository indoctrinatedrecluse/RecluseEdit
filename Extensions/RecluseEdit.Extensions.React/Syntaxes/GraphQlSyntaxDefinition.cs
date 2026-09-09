using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.React.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for GraphQL queries, mutations, and schemas,
/// including operations, directives, variables, types, and block strings.
/// </summary>
public static class GraphQlSyntaxDefinition
{
    private const string GraphQlXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="GraphQL" extensions=".graphql;.gql" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="Operations" foreground="#C586C0" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Directive" foreground="#DCDCAA" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Single Line Comment (#) -->
                <Span color="Comment">
                    <Begin>\#</Begin>
                </Span>

                <!-- Block Strings -->
                <Span color="String" multiline="true">
                    <Begin>&quot;&quot;&quot;</Begin>
                    <End>&quot;&quot;&quot;</End>
                </Span>

                <!-- Standard String ("...") -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Directives (@include, @skip, @deprecated, etc.) -->
                <Rule color="Directive">
                    @[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Variables ($variableName) -->
                <Rule color="Variable">
                    \$[a-zA-Z_][a-zA-Z0-9_]*
                </Rule>

                <!-- Operations & Framework Keywords (Magenta #C586C0) -->
                <Keywords color="Operations">
                    <Word>query</Word>
                    <Word>mutation</Word>
                    <Word>subscription</Word>
                    <Word>fragment</Word>
                </Keywords>

                <!-- Schema Definition Keywords (Blue #569CD6) -->
                <Keywords color="Keywords">
                    <Word>schema</Word>
                    <Word>type</Word>
                    <Word>input</Word>
                    <Word>interface</Word>
                    <Word>union</Word>
                    <Word>enum</Word>
                    <Word>scalar</Word>
                    <Word>directive</Word>
                    <Word>extend</Word>
                    <Word>implements</Word>
                    <Word>on</Word>
                    <Word>repeatable</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Built-in GraphQL Types (Teal #4EC9B0) -->
                <Keywords color="Types">
                    <Word>String</Word>
                    <Word>Int</Word>
                    <Word>Float</Word>
                    <Word>Boolean</Word>
                    <Word>ID</Word>
                </Keywords>

                <!-- Numeric Values -->
                <Rule color="Digits">
                    \b\d+(\.[0-9]+)?([eE][+-]?[0-9]+)?\b
                </Rule>

                <!-- Punctuation (!, :, {, }, (, ), [, ]) -->
                <Rule color="Punctuation">
                    [!?:()\[\]{}|=]|\.\.\.
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled GraphQL syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(GraphQlXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
