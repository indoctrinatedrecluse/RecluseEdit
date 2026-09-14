using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class SmartTypingAndFoldingTests
{
    private static TextEditor CreateTestEditor(string text = "")
    {
        var editor = new TextEditor();
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = 4;
        editor.Document.Text = text;
        return editor;
    }

    [STATestMethod]
    public void TestSmartEnter_BraceSplit()
    {
        var editor = CreateTestEditor("void Foo() {}");
        // Caret between { and }
        editor.CaretOffset = 12;

        EditorOperations.HandleSmartEnter(editor, "csharp");

        var text = editor.Document.Text;
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        Assert.HasCount(3, lines, "Should split into 3 lines");
        Assert.AreEqual("void Foo() {", lines[0]);
        Assert.AreEqual("    ", lines[1], "Middle line should have 4 spaces indent");
        Assert.AreEqual("}", lines[2]);
        Assert.AreEqual(editor.Document.GetLineByNumber(2).Offset + 4, editor.CaretOffset, "Caret should be at the indented position on line 2");
    }

    [STATestMethod]
    public void TestSmartEnter_BlockOpener_CSharp()
    {
        var editor = CreateTestEditor("if (x > 0) {");
        editor.CaretOffset = editor.Document.TextLength;

        EditorOperations.HandleSmartEnter(editor, "csharp");

        var text = editor.Document.Text;
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        Assert.HasCount(2, lines);
        Assert.AreEqual("    ", lines[1]);
    }

    [STATestMethod]
    public void TestSmartEnter_BlockOpener_Python()
    {
        var editor = CreateTestEditor("def greet(name):");
        editor.CaretOffset = editor.Document.TextLength;

        EditorOperations.HandleSmartEnter(editor, "python");

        var text = editor.Document.Text;
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        Assert.HasCount(2, lines);
        Assert.AreEqual("    ", lines[1], "Python block should indent by 4 spaces");
    }

    [STATestMethod]
    public void TestSmartEnter_PreserveLeadingWhitespace()
    {
        var editor = CreateTestEditor("    var message = \"hello\";");
        editor.CaretOffset = editor.Document.TextLength;

        EditorOperations.HandleSmartEnter(editor, "csharp");

        var text = editor.Document.Text;
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        Assert.HasCount(2, lines);
        Assert.AreEqual("    ", lines[1], "Should preserve existing 4 space indentation");
    }

    [STATestMethod]
    public void TestSmartBackspace_RemovesDelimiters()
    {
        string[] pairs = { "()", "{}", "[]", "\"\"", "''", "``" };

        foreach (var pair in pairs)
        {
            var editor = CreateTestEditor(pair);
            editor.CaretOffset = 1;

            bool handled = EditorOperations.HandleSmartBackspace(editor);

            Assert.IsTrue(handled, $"Smart backspace should handle {pair}");
            Assert.AreEqual(string.Empty, editor.Document.Text, $"Pair {pair} should be completely removed");
            Assert.AreEqual(0, editor.CaretOffset);
        }
    }

    [STATestMethod]
    public void TestSmartBackspace_NormalCharacter_NotHandled()
    {
        var editor = CreateTestEditor("abc");
        editor.CaretOffset = 2;

        bool handled = EditorOperations.HandleSmartBackspace(editor);

        Assert.IsFalse(handled, "Smart backspace should not handle non-delimiter characters");
        Assert.AreEqual("abc", editor.Document.Text);
    }

    [TestMethod]
    public void TestUniversalFolding_Braces()
    {
        var doc = new TextDocument
        {
            Text = "class MyClass\n{\n    void Method()\n    {\n        int x = 1;\n    }\n}\n"
        };

        var strategy = new UniversalFoldingStrategy();
        var foldings = strategy.CreateNewFoldings(doc, "csharp", out int firstError).ToList();

        Assert.AreEqual(-1, firstError);
        Assert.IsGreaterThanOrEqualTo(2, foldings.Count, $"Expected at least 2 foldings for class and method, got {foldings.Count}");
    }

    [TestMethod]
    public void TestUniversalFolding_PythonIndentation()
    {
        var doc = new TextDocument
        {
            Text = "def calculate_sum(a, b):\n    result = a + b\n    return result\n\ndef main():\n    pass\n"
        };

        var strategy = new UniversalFoldingStrategy();
        var foldings = strategy.CreateNewFoldings(doc, "python", out int firstError).ToList();

        Assert.AreEqual(-1, firstError);
        Assert.IsGreaterThanOrEqualTo(1, foldings.Count, $"Expected at least 1 folding for python function, got {foldings.Count}");
    }

    [TestMethod]
    public void TestUniversalFolding_Regions()
    {
        var doc = new TextDocument
        {
            Text = "#region Utilities\nvoid Test() {}\n#endregion\n"
        };

        var strategy = new UniversalFoldingStrategy();
        var foldings = strategy.CreateNewFoldings(doc, "csharp", out int firstError).ToList();

        Assert.AreEqual(-1, firstError);
        Assert.HasCount(1, foldings);
        Assert.AreEqual("Utilities", foldings[0].Name);
    }

    [TestMethod]
    public void TestUniversalFolding_Comments()
    {
        var doc = new TextDocument
        {
            Text = "/*\n * Header Comment\n * Multi-line\n */\nint a = 1;\n"
        };

        var strategy = new UniversalFoldingStrategy();
        var foldings = strategy.CreateNewFoldings(doc, "csharp", out int firstError).ToList();

        Assert.AreEqual(-1, firstError);
        Assert.IsGreaterThanOrEqualTo(1, foldings.Count, "Should fold multi-line comments");
    }
}
