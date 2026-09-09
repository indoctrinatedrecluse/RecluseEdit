using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services.Syntaxes;
using RecluseEdit.Extensions.Angular.Syntaxes;
using RecluseEdit.Extensions.Flutter.Syntaxes;
using RecluseEdit.Extensions.React.Syntaxes;
using RecluseEdit.Extensions.Ruby.Syntaxes;
using RecluseEdit.UI.Controls;

namespace RecluseEdit.Tests;

[TestClass]
public class SyntaxDefinitionTests
{
    private static void AssertHasColors(IHighlightingDefinition def, params string[] expectedColorNames)
    {
        var names = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        foreach (var expected in expectedColorNames)
        {
            CollectionAssert.Contains(names, expected, $"Expected syntax '{def.Name}' to contain color '{expected}'");
        }
    }

    [TestMethod]
    public void MarkdownSyntaxDefinition_LoadsAndHasColors()
    {
        var def = MarkdownSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Markdown", def.Name);
        AssertHasColors(def, "Heading1", "Heading2", "Bold", "Italic", "Code", "CodeBlock", "Link", "Blockquote");
    }

    [TestMethod]
    public void JavaScriptSyntaxDefinition_LoadsAndHasColors()
    {
        var def = JavaScriptSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("JavaScriptModern", def.Name);
        AssertHasColors(def, "ControlFlow", "Functions", "Types", "Regex", "DocComment");
    }

    [TestMethod]
    public void CssSyntaxDefinition_LoadsAndHasColors()
    {
        var def = CssSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("CSSModern", def.Name);
        AssertHasColors(def, "Property", "Variable", "SelectorClass", "AtRule", "Function");
    }

    [TestMethod]
    public void ModernPhpSyntaxDefinition_LoadsAndHasColors()
    {
        var def = ModernPhpSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("PHPModern", def.Name);
        AssertHasColors(def, "Attribute", "ControlFlow", "Functions", "Types", "PhpTag");
    }

    [TestMethod]
    public void JsxSyntaxDefinition_LoadsAndHasColors()
    {
        var def = JsxSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("ReactJSX", def.Name);
        AssertHasColors(def, "JsxComponent", "JsxTag", "JsxAttribute", "Hooks");
    }

    [TestMethod]
    public void GraphQlSyntaxDefinition_LoadsAndHasColors()
    {
        var def = GraphQlSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("GraphQL", def.Name);
        AssertHasColors(def, "Operations", "Directive", "Variable", "Types");
    }

    [TestMethod]
    public void AngularHtmlSyntaxDefinition_LoadsAndHasColors()
    {
        var def = AngularHtmlSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("AngularHTML", def.Name);
        AssertHasColors(def, "ControlFlow", "StructuralDirective", "TwoWayBinding", "PropertyBinding", "EventBinding");
    }

    [TestMethod]
    public void AngularTsSyntaxDefinition_LoadsAndHasColors()
    {
        var def = AngularTsSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("AngularTS", def.Name);
        AssertHasColors(def, "Decorator", "Signals", "Types");
    }

    [TestMethod]
    public void DartSyntaxDefinition_LoadsAndHasColors()
    {
        var def = DartSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Dart", def.Name);
        AssertHasColors(def, "Functions", "Annotation", "DocComment", "Types");
    }

    [TestMethod]
    public void RubySyntaxDefinition_LoadsAndHasColors()
    {
        var def = RubySyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Ruby", def.Name);
        AssertHasColors(def, "RailsMacro", "Functions", "Symbol", "Variable");
    }

    [TestMethod]
    public void BracketHighlightRenderer_FindsMatchingBrackets()
    {
        var doc = new TextDocument("(hello)");
        var match = BracketHighlightRenderer.FindMatchingBrackets(doc, 0);
        Assert.IsTrue(match.HasValue);
        Assert.AreEqual(0, match.Value.OpenOffset);
        Assert.AreEqual(6, match.Value.CloseOffset);

        // Caret adjacent to close parenthesis
        var matchClose = BracketHighlightRenderer.FindMatchingBrackets(doc, 7);
        Assert.IsTrue(matchClose.HasValue);
        Assert.AreEqual(0, matchClose.Value.OpenOffset);
        Assert.AreEqual(6, matchClose.Value.CloseOffset);

        // Brackets
        var docBrackets = new TextDocument("[1, 2]");
        var matchBracket = BracketHighlightRenderer.FindMatchingBrackets(docBrackets, 0);
        Assert.IsTrue(matchBracket.HasValue);
        Assert.AreEqual(0, matchBracket.Value.OpenOffset);
        Assert.AreEqual(5, matchBracket.Value.CloseOffset);

        // Braces
        var docBraces = new TextDocument("{ key }");
        var matchBrace = BracketHighlightRenderer.FindMatchingBrackets(docBraces, 0);
        Assert.IsTrue(matchBrace.HasValue);
        Assert.AreEqual(0, matchBrace.Value.OpenOffset);
        Assert.AreEqual(6, matchBrace.Value.CloseOffset);

        // Unmatched bracket
        var docUnmatched = new TextDocument("(hello");
        var matchNone = BracketHighlightRenderer.FindMatchingBrackets(docUnmatched, 0);
        Assert.IsFalse(matchNone.HasValue);
    }
}

