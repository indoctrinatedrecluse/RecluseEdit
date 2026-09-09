using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Angular.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Angular HTML Templates,
/// including modern control flow (@if, @for, @switch, @defer), structural directives (*ngIf, *ngFor),
/// property/event bindings ([prop], (event), [(ngModel)]), and interpolation expressions ({{ ... }}).
/// </summary>
public static class AngularHtmlSyntaxDefinition
{
    private const string AngularHtmlXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="AngularHTML" extensions=".component.html" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="StructuralDirective" foreground="#C586C0" fontWeight="bold" />
            <Color name="TwoWayBinding" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="PropertyBinding" foreground="#9CDCFE" />
            <Color name="EventBinding" foreground="#DCDCAA" />
            <Color name="TemplateRef" foreground="#4EC9B0" />
            <Color name="Interpolation" foreground="#9CDCFE" />
            <Color name="Tag" foreground="#569CD6" />
            <Color name="TagBracket" foreground="#808080" />
            <Color name="Attribute" foreground="#9CDCFE" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- HTML Comment -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- Angular Interpolation ({{ ... }}) -->
                <Span color="Interpolation">
                    <Begin>\{\{</Begin>
                    <End>\}\}</End>
                    <RuleSet>
                        <Keywords color="ControlFlow">
                            <Word>as</Word>
                        </Keywords>
                        <Rule color="EventBinding">
                            (?&lt;=\|\s*)[a-zA-Z_][a-zA-Z0-9_]*
                        </Rule>
                        <Rule color="Punctuation">
                            [|?:.]
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Modern Angular Control Flow Blocks (@if, @for, @switch, @defer, etc.) -->
                <Rule color="ControlFlow">
                    @(if|else\s+if|else|for|empty|switch|case|default|defer|placeholder|loading|error)\b
                </Rule>

                <!-- Double Quoted Strings (Attribute values) -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                </Span>

                <!-- Single Quoted Strings -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                </Span>

                <!-- Angular Two-Way Bindings ([(ngModel)]) -->
                <Rule color="TwoWayBinding">
                    \[\([a-zA-Z0-9_\.\-]+\)\]
                </Rule>

                <!-- Angular Property Bindings ([prop], [class.active], [style.width.px]) -->
                <Rule color="PropertyBinding">
                    \[[a-zA-Z0-9_\.\-]+\]
                </Rule>

                <!-- Angular Event Bindings ((click), (submit), (keydown.enter)) -->
                <Rule color="EventBinding">
                    \([a-zA-Z0-9_\.\-]+\)
                </Rule>

                <!-- Structural Directives (*ngIf, *ngFor, *ngSwitchCase, etc.) -->
                <Rule color="StructuralDirective">
                    \*[a-zA-Z0-9_\-]+
                </Rule>

                <!-- Template Reference Variables (#myInput, #ref) -->
                <Rule color="TemplateRef">
                    \#[a-zA-Z_][a-zA-Z0-9_\-]*
                </Rule>

                <!-- Standard HTML Closing Tags (</tag>) -->
                <Rule color="Tag">
                    (?&lt;=&lt;/)[a-zA-Z0-9_\-]+(?=&gt;)
                </Rule>

                <!-- Standard HTML Opening Tags (<tag) -->
                <Rule color="Tag">
                    (?&lt;=&lt;)[a-zA-Z0-9_\-]+(?=[\s/&gt;])
                </Rule>

                <!-- Standard HTML Attributes (class, id, type, etc.) -->
                <Rule color="Attribute">
                    \b[a-zA-Z_][a-zA-Z0-9_\-]*(?=\s*=)
                </Rule>

                <!-- Tag Brackets (<, >, </, />) -->
                <Rule color="TagBracket">
                    &lt;/|/&gt;|&lt;|&gt;
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [?:;=]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    /// <summary>
    /// Loads and returns the compiled Angular HTML syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(AngularHtmlXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
