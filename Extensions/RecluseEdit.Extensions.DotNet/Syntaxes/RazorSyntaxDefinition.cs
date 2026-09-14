using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.DotNet.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for Blazor (.razor) and Razor Pages (.cshtml)
/// in Dark+ styling, highlighting Razor directives, HTML elements, and C# expressions.
/// </summary>
public static class RazorSyntaxDefinition
{
    private const string RazorXshd = """"
        <?xml version="1.0"?>
        <SyntaxDefinition name="Razor" extensions=".razor;.cshtml" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="RazorDirective" foreground="#C586C0" fontWeight="bold" />
            <Color name="RazorEvent" foreground="#DCDCAA" />
            <Color name="HtmlTag" foreground="#569CD6" />
            <Color name="HtmlAttribute" foreground="#9CDCFE" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Digits" foreground="#B5CEA8" />

            <RuleSet ignoreCase="false">
                <!-- Razor Comments: @* ... *@ -->
                <Span color="Comment" multiline="true">
                    <Begin>@\*</Begin>
                    <End>\*@</End>
                </Span>

                <!-- HTML Comments -->
                <Span color="Comment" multiline="true">
                    <Begin>&lt;!--</Begin>
                    <End>--&gt;</End>
                </Span>

                <!-- Strings -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Razor Directives -->
                <Rule color="RazorDirective">@(page|model|inject|code|functions|using|namespace|implements|inherits|attribute|typeparam|layout|section|rendermode|ref|key)\b</Rule>

                <!-- Razor Event Handlers & Bindings -->
                <Rule color="RazorEvent">@(bind(-[a-zA-Z0-9_:]+)?|on[a-zA-Z0-9_]+)\b</Rule>

                <!-- Razor Control Flow -->
                <Rule color="ControlFlow">@(if|else|switch|case|for|foreach|while|do|try|catch|finally|await)\b</Rule>

                <!-- Digits -->
                <Rule color="Digits">\b\d+(\.[0-9]+)?\b</Rule>

                <!-- HTML / XML Tags -->
                <Rule color="HtmlTag">&lt;/?([a-zA-Z0-9_\-\.]+)</Rule>
                <Rule color="HtmlTag">/?&gt;</Rule>

                <!-- C# Keywords in Code Blocks -->
                <Keywords color="Keywords">
                    <Word>public</Word>
                    <Word>private</Word>
                    <Word>protected</Word>
                    <Word>internal</Word>
                    <Word>static</Word>
                    <Word>readonly</Word>
                    <Word>override</Word>
                    <Word>virtual</Word>
                    <Word>async</Word>
                    <Word>await</Word>
                    <Word>class</Word>
                    <Word>record</Word>
                    <Word>struct</Word>
                    <Word>interface</Word>
                    <Word>new</Word>
                    <Word>return</Word>
                    <Word>var</Word>
                    <Word>get</Word>
                    <Word>set</Word>
                    <Word>init</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>null</Word>
                </Keywords>

                <!-- Types -->
                <Keywords color="Types">
                    <Word>Task</Word>
                    <Word>ValueTask</Word>
                    <Word>EventCallback</Word>
                    <Word>RenderFragment</Word>
                    <Word>Parameter</Word>
                    <Word>Inject</Word>
                    <Word>CascadingParameter</Word>
                    <Word>NavigationManager</Word>
                    <Word>HttpClient</Word>
                    <Word>IJSRuntime</Word>
                    <Word>string</Word>
                    <Word>int</Word>
                    <Word>bool</Word>
                    <Word>double</Word>
                    <Word>decimal</Word>
                    <Word>DateTime</Word>
                    <Word>Guid</Word>
                    <Word>List</Word>
                    <Word>IEnumerable</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """";

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(RazorXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
