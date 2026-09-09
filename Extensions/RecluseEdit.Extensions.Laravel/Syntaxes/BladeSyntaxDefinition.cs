using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Laravel.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Laravel Blade templates (.blade.php),
/// supporting Blade directives (@extends, @section, @if, @foreach, @livewire, @vite, @csrf, etc.),
/// comments ({{-- ... --}}), expressions ({{ ... }} and {!! ... !!}), and embedded HTML markup.
/// </summary>
public static class BladeSyntaxDefinition
{
    private const string BladeXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Blade" extensions=".blade.php" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" fontStyle="italic" />
            <Color name="Directive" foreground="#C586C0" fontWeight="bold" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Variable" foreground="#9CDCFE" />
            <Color name="RawHtml" foreground="#DCDCAA" />
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="HtmlTag" foreground="#569CD6" />
            <Color name="HtmlAttribute" foreground="#9CDCFE" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Blade Comments -->
                <Span color="Comment" multiline="true">
                    <Begin>\{\{--</Begin>
                    <End>--\}\}</End>
                </Span>

                <!-- HTML Comment -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- Unescaped Raw Echo: {!! ... !!} -->
                <Span color="RawHtml" multiline="true">
                    <Begin>\{!!</Begin>
                    <End>!!\}</End>
                    <RuleSet>
                        <Span color="String">
                            <Begin>&quot;</Begin>
                            <End>&quot;</End>
                        </Span>
                        <Span color="String">
                            <Begin>'</Begin>
                            <End>'</End>
                        </Span>
                        <Rule color="Variable">
                            \$[a-zA-Z_\x7f-\xff][a-zA-Z0-9_\x7f-\xff]*
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Escaped Blade Echo: {{ ... }} -->
                <Span color="Variable" multiline="true">
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
                        <Rule color="Variable">
                            \$[a-zA-Z_\x7f-\xff][a-zA-Z0-9_\x7f-\xff]*
                        </Rule>
                        <Rule color="Digits">
                            \b\d+(\.[0-9]+)?\b
                        </Rule>
                        <Rule color="Punctuation">
                            [?:;.,()\[\]{}+\-*\/%=&lt;&gt;&amp;|^!~]
                        </Rule>
                    </RuleSet>
                </Span>

                <!-- Blade Directives (@extends, @section, @if, @foreach, @livewire, @vite, @csrf, etc.) -->
                <Rule color="Directive">
                    @(extends|section|endsection|yield|show|parent|stop|overwrite|component|endcomponent|slot|endslot|props|push|pushOnce|endPushOnce|endpush|prepend|endprepend|stack|once|endonce|if|elseif|else|endif|unless|endunless|isset|endisset|empty|endempty|auth|endauth|guest|endguest|production|endproduction|env|endenv|hasSection|sectionMissing|switch|case|default|endswitch|for|endfor|foreach|endforeach|forelse|endforelse|while|endwhile|continue|break|include|includeIf|includeWhen|includeUnless|includeFirst|each|csrf|method|error|enderror|livewire|vite|session|endsession|dd|dump|class|style|checked|selected|disabled|readonly|required|php|endphp)\b
                </Rule>

                <!-- Custom / Component Directives: @directiveName -->
                <Rule color="Directive">
                    @[a-zA-Z_][a-zA-Z0-9_]*\b
                </Rule>

                <!-- PHP Variables: $var -->
                <Rule color="Variable">
                    \$[a-zA-Z_\x7f-\xff][a-zA-Z0-9_\x7f-\xff]*
                </Rule>

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
    /// Loads and returns the compiled Blade syntax definition.
    /// </summary>
    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = XmlReader.Create(new StringReader(BladeXshd));
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
