using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Frontend.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Vue 3 Single File Components (.vue),
/// supporting templates, script setup with TypeScript, scoped styles, directives, and expressions.
/// </summary>
public static class VueSyntaxDefinition
{
    private const string VueXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Vue" extensions=".vue" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="VueDirectives" foreground="#C586C0" fontWeight="bold" />
            <Color name="VueReactivity" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Tags" foreground="#569CD6" />
            <Color name="Attributes" foreground="#9CDCFE" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Interpolation" foreground="#DCDCAA" />

            <RuleSet ignoreCase="false">
                <!-- HTML Comments -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- JS Single line comment -->
                <Span color="Comment">
                    <Begin>//</Begin>
                </Span>

                <!-- JS Multi-line comment -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Mustache interpolation: {{ ... }} -->
                <Span color="Interpolation">
                    <Begin>\{\{</Begin>
                    <End>\}\}</End>
                </Span>

                <!-- Strings: double, single, backtick -->
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

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F]+\b|\b\d+(\.[0-9]+)?\b</Rule>

                <!-- Vue Directives & Events: v-..., @..., :... -->
                <Rule color="VueDirectives">\bv-(if|else-if|else|for|model|show|bind|on|slot|html|text|pre|cloak|once|memo)\b|@[a-zA-Z0-9_\-\.]+|:[a-zA-Z0-9_\-\.]+|#[a-zA-Z0-9_\-\.]+</Rule>

                <!-- Vue Reactivity & Macros -->
                <Keywords color="VueReactivity">
                    <Word>ref</Word>
                    <Word>reactive</Word>
                    <Word>computed</Word>
                    <Word>watch</Word>
                    <Word>watchEffect</Word>
                    <Word>watchPostEffect</Word>
                    <Word>watchSyncEffect</Word>
                    <Word>onMounted</Word>
                    <Word>onUpdated</Word>
                    <Word>onUnmounted</Word>
                    <Word>onBeforeMount</Word>
                    <Word>onBeforeUpdate</Word>
                    <Word>onBeforeUnmount</Word>
                    <Word>onErrorCaptured</Word>
                    <Word>defineProps</Word>
                    <Word>defineEmits</Word>
                    <Word>defineExpose</Word>
                    <Word>defineOptions</Word>
                    <Word>defineSlots</Word>
                    <Word>defineModel</Word>
                    <Word>withDefaults</Word>
                    <Word>toRef</Word>
                    <Word>toRefs</Word>
                    <Word>isRef</Word>
                    <Word>unref</Word>
                    <Word>shallowRef</Word>
                    <Word>shallowReactive</Word>
                    <Word>nextTick</Word>
                    <Word>provide</Word>
                    <Word>inject</Word>
                </Keywords>

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
                    <Word>extends</Word>
                    <Word>implements</Word>
                    <Word>as</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                    <Word>undefined</Word>
                </Keywords>

                <!-- Tags -->
                <Rule color="Tags">&lt;/?(template|script|style|slot|component|transition|keep-alive|[a-zA-Z0-9_\-]+)&gt;?</Rule>

                <!-- Tag attributes -->
                <Rule color="Attributes">\b(setup|lang|scoped|src|class|id|name|type|placeholder|disabled|href|key)\b</Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(VueXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

