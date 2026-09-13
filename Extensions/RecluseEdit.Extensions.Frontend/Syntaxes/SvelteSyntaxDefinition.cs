using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Frontend.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Svelte components (.svelte),
/// featuring Svelte 5 runes ($state, $derived, $effect, $props), template blocks, bindings, and events.
/// </summary>
public static class SvelteSyntaxDefinition
{
    private const string SvelteXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Svelte" extensions=".svelte" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="SvelteRunes" foreground="#FF3E00" fontWeight="bold" />
            <Color name="SvelteBlocks" foreground="#C586C0" fontWeight="bold" />
            <Color name="SvelteBindings" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Tags" foreground="#569CD6" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Interpolation" foreground="#DCDCAA" />

            <RuleSet ignoreCase="false">
                <!-- HTML Comments -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- JS Comments -->
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

                <!-- Svelte Logic Blocks: {#if}, {:else}, {/if}, {#each}, etc. -->
                <Rule color="SvelteBlocks">\{#?(if|each|await|key|snippet)\b|\{:?(else|then|catch)\b|\{/?(if|each|await|key|snippet)\}|\{@(html|debug|const|render)\b</Rule>

                <!-- Svelte 5 Runes -->
                <Rule color="SvelteRunes">\$state(\.(raw|snapshot))?|\$derived(\.by)?|\$effect(\.(pre|root))?|\$props|\$bindable|\$inspect|\$host</Rule>

                <!-- Svelte Event Directives & Bindings: bind:..., on:..., use:..., class:... -->
                <Rule color="SvelteBindings">\b(bind|on|use|class|style|transition|in|out|animate):[a-zA-Z0-9_\-\.]+\b</Rule>

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F]+\b|\b\d+(\.[0-9]+)?\b</Rule>

                <!-- JS/TS Standard Keywords -->
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
                    <Word>type</Word>
                    <Word>interface</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                    <Word>undefined</Word>
                </Keywords>

                <!-- Tags -->
                <Rule color="Tags">&lt;/?(script|style|svelte:head|svelte:body|svelte:window|svelte:document|svelte:element|svelte:component|svelte:self|slot|[a-zA-Z0-9_\-]+)&gt;?</Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(SvelteXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

