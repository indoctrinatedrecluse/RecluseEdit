using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.RestClient.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for GraphQL (.graphql, .gql) files in Dark+ styling.
/// Supports operations, SDL declarations, directives, variables, and scalar types.
/// </summary>
public static class GraphQLSyntaxDefinition
{
    private const string GraphQLXshd = """"
        <?xml version="1.0"?>
        <SyntaxDefinition name="GraphQL" extensions=".graphql;.gql" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="BuiltInTypes" foreground="#4EC9B0" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="Directive" foreground="#DCDCAA" />
            <Color name="Digits" foreground="#B5CEA8" />

            <RuleSet ignoreCase="false">
                <!-- Single line comments: # -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Block string: """...""" -->
                <Span color="String" multiline="true">
                    <Begin>"""</Begin>
                    <End>"""</End>
                </Span>

                <!-- Regular string: "..." -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Variables: $variableName -->
                <Rule color="Variable">\$[a-zA-Z0-9_]+</Rule>

                <!-- Directives: @directiveName -->
                <Rule color="Directive">@[a-zA-Z0-9_]+</Rule>

                <!-- Digits -->
                <Rule color="Digits">\b\d+(\.[0-9]+)?\b</Rule>

                <!-- Operation & SDL Keywords -->
                <Keywords color="Keywords">
                    <Word>query</Word>
                    <Word>mutation</Word>
                    <Word>subscription</Word>
                    <Word>fragment</Word>
                    <Word>on</Word>
                    <Word>type</Word>
                    <Word>interface</Word>
                    <Word>union</Word>
                    <Word>enum</Word>
                    <Word>input</Word>
                    <Word>implements</Word>
                    <Word>directive</Word>
                    <Word>schema</Word>
                    <Word>extend</Word>
                    <Word>scalar</Word>
                    <Word>repeatable</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Built-In Scalars -->
                <Keywords color="BuiltInTypes">
                    <Word>String</Word>
                    <Word>Int</Word>
                    <Word>Float</Word>
                    <Word>Boolean</Word>
                    <Word>ID</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """";

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(GraphQLXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
