using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for Lua,
/// featuring multiline comments/strings, standard libraries, metatables, and Dark+ theme styling.
/// </summary>
public static class LuaSyntaxDefinition
{
    private const string LuaXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="Lua" extensions=".lua" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Builtins" foreground="#DCDCAA" />
            <Color name="SpecialVars" foreground="#9CDCFE" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Punctuation" foreground="#D4D4D4" />

            <RuleSet>
                <!-- Multiline Block Comments [==[ ... ]==] -->
                <Span color="Comment" multiline="true">
                    <Begin>--\[\[</Begin>
                    <End>\]\]</End>
                </Span>

                <!-- Single Line Comment -->
                <Span color="Comment">
                    <Begin>--</Begin>
                </Span>

                <!-- Multiline Literal Strings ([[ ... ]]) -->
                <Span color="String" multiline="true">
                    <Begin>\[\[</Begin>
                    <End>\]\]</End>
                </Span>

                <!-- Double-quoted Strings -->
                <Span color="String">
                    <Begin>&quot;</Begin>
                    <End>&quot;</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Single-quoted Strings -->
                <Span color="String">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="\\" end="." />
                    </RuleSet>
                </Span>

                <!-- Control Flow Keywords (#C586C0) -->
                <Keywords color="ControlFlow">
                    <Word>break</Word>
                    <Word>do</Word>
                    <Word>else</Word>
                    <Word>elseif</Word>
                    <Word>end</Word>
                    <Word>for</Word>
                    <Word>goto</Word>
                    <Word>if</Word>
                    <Word>in</Word>
                    <Word>repeat</Word>
                    <Word>return</Word>
                    <Word>then</Word>
                    <Word>until</Word>
                    <Word>while</Word>
                </Keywords>

                <!-- Core Keywords (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>and</Word>
                    <Word>function</Word>
                    <Word>local</Word>
                    <Word>not</Word>
                    <Word>or</Word>
                </Keywords>

                <!-- Constants (#569CD6) -->
                <Keywords color="Keywords">
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>nil</Word>
                </Keywords>

                <!-- Special Variables (#9CDCFE) -->
                <Keywords color="SpecialVars">
                    <Word>_G</Word>
                    <Word>_ENV</Word>
                    <Word>_VERSION</Word>
                    <Word>self</Word>
                </Keywords>

                <!-- Standard Libraries & Builtin Functions (#DCDCAA) -->
                <Keywords color="Builtins">
                    <Word>assert</Word>
                    <Word>collectgarbage</Word>
                    <Word>coroutine</Word>
                    <Word>debug</Word>
                    <Word>dofile</Word>
                    <Word>error</Word>
                    <Word>getmetatable</Word>
                    <Word>io</Word>
                    <Word>ipairs</Word>
                    <Word>load</Word>
                    <Word>loadfile</Word>
                    <Word>math</Word>
                    <Word>next</Word>
                    <Word>os</Word>
                    <Word>package</Word>
                    <Word>pairs</Word>
                    <Word>pcall</Word>
                    <Word>print</Word>
                    <Word>rawequal</Word>
                    <Word>rawget</Word>
                    <Word>rawlen</Word>
                    <Word>rawset</Word>
                    <Word>require</Word>
                    <Word>select</Word>
                    <Word>setmetatable</Word>
                    <Word>string</Word>
                    <Word>table</Word>
                    <Word>tonumber</Word>
                    <Word>tostring</Word>
                    <Word>type</Word>
                    <Word>utf8</Word>
                    <Word>warn</Word>
                    <Word>xpcall</Word>
                </Keywords>

                <!-- Lua Operators (.., ~=, ==, <=, >=, //, #) -->
                <Rule color="Operators">
                    (\.\.|~=|==|&lt;=|&gt;=|//|[#])
                </Rule>

                <!-- Digits (hex 0x... and decimal/float) -->
                <Rule color="Digits">
                    \b0[xX][0-9a-fA-F]+(\.[0-9a-fA-F]+)?([pP][+-]?\d+)?\b |
                    \b\d+(\.\d+)?([eE][+-]?\d+)?\b
                </Rule>

                <!-- Punctuation -->
                <Rule color="Punctuation">
                    [{}\(\)\[\];,:]
                </Rule>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var reader = new StringReader(LuaXshd);
        using var xmlReader = XmlReader.Create(reader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
