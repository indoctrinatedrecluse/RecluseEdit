using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Scripting.Syntaxes;

/// <summary>
/// Provides AvalonEdit syntax highlighting definition (XSHD) for Elixir (.ex, .exs, .heex, .eex) files in Dark+ styling,
/// including Phoenix LiveView and pipe operators.
/// </summary>
public static class ElixirSyntaxDefinition
{
    private const string ElixirXshd = """"
        <?xml version="1.0"?>
        <SyntaxDefinition name="Elixir" extensions=".ex;.exs;.heex;.eex" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="#6A9955" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Char" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="ControlFlow" foreground="#C586C0" fontWeight="bold" />
            <Color name="Atoms" foreground="#4EC9B0" />
            <Color name="Module" foreground="#4EC9B0" fontWeight="bold" />
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="Operators" foreground="#D4D4D4" />

            <RuleSet ignoreCase="false">
                <!-- Single-line comments: # -->
                <Span color="Comment">
                    <Begin>#</Begin>
                </Span>

                <!-- Triple quoted strings / sigils: ~H"""...""" or """...""" -->
                <Span color="String" multiline="true">
                    <Begin>"""</Begin>
                    <End>"""</End>
                </Span>
                <Span color="String" multiline="true">
                    <Begin>~[a-zA-Z]"""</Begin>
                    <End>"""</End>
                </Span>

                <!-- Regular strings: "..." -->
                <Span color="String">
                    <Begin>"</Begin>
                    <End>"</End>
                    <RuleSet>
                        <Span begin="\\#" end="." />
                        <Span begin="\\&quot;" end="." />
                        <Span color="Atoms">
                            <Begin>#\{</Begin>
                            <End>\}</End>
                        </Span>
                    </RuleSet>
                </Span>

                <!-- Atoms: :atom or atom: -->
                <Rule color="Atoms">:[a-zA-Z_][a-zA-Z0-9_]*|\b[a-zA-Z_][a-zA-Z0-9_]*:(?=\s)</Rule>

                <!-- Module names (Capitalized identifiers) -->
                <Rule color="Module">\b[A-Z][a-zA-Z0-9_]*\b</Rule>

                <!-- Digits -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F_]+\b|\b0[bB][01_]+\b|\b0[oO][0-7_]+\b|\b\d[0-9_]*(\.[0-9_]+)?([eE][+-]?[0-9_]+)?\b</Rule>

                <!-- Operators (e.g. pipe |>) -->
                <Rule color="Operators">\|&gt;|\=\&gt;|\-\&gt;|\+\+|\-\-|\=\=|\!\=|\&lt;\=|\&gt;\=|\&amp;\&amp;|\|\|</Rule>

                <!-- Control Flow -->
                <Keywords color="ControlFlow">
                    <Word>if</Word>
                    <Word>unless</Word>
                    <Word>case</Word>
                    <Word>cond</Word>
                    <Word>with</Word>
                    <Word>for</Word>
                    <Word>try</Word>
                    <Word>rescue</Word>
                    <Word>catch</Word>
                    <Word>after</Word>
                    <Word>raise</Word>
                    <Word>throw</Word>
                </Keywords>

                <!-- Elixir Definition Keywords -->
                <Keywords color="Keywords">
                    <Word>defmodule</Word>
                    <Word>def</Word>
                    <Word>defp</Word>
                    <Word>defmacro</Word>
                    <Word>defmacrop</Word>
                    <Word>defstruct</Word>
                    <Word>defguard</Word>
                    <Word>defimpl</Word>
                    <Word>defprotocol</Word>
                    <Word>do</Word>
                    <Word>end</Word>
                    <Word>use</Word>
                    <Word>import</Word>
                    <Word>require</Word>
                    <Word>alias</Word>
                    <Word>fn</Word>
                    <Word>when</Word>
                    <Word>and</Word>
                    <Word>or</Word>
                    <Word>not</Word>
                    <Word>in</Word>
                    <Word>true</Word>
                    <Word>false</Word>
                    <Word>nil</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """";

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(ElixirXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}
