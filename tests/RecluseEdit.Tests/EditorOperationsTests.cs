using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class EditorOperationsTests
{
    [STATestMethod]
    public void TestToggleLineComment_CStyle()
    {
        var editor = new TextEditor();
        editor.Document.Text = "line1\nline2\nline3";
        editor.TextArea.Caret.Line = 1;

        // Comment line 1
        EditorOperations.ToggleLineComment(editor, "csharp");
        Assert.AreEqual("// line1\nline2\nline3", editor.Document.Text);

        // Uncomment line 1
        EditorOperations.ToggleLineComment(editor, "csharp");
        Assert.AreEqual("line1\nline2\nline3", editor.Document.Text);
    }

    [STATestMethod]
    public void TestToggleLineComment_HashStyle()
    {
        var editor = new TextEditor();
        editor.Document.Text = "def foo():\n    pass";
        editor.TextArea.Caret.Line = 2;

        // Comment python line
        EditorOperations.ToggleLineComment(editor, "python");
        Assert.AreEqual("def foo():\n    # pass", editor.Document.Text);

        // Uncomment python line
        EditorOperations.ToggleLineComment(editor, "python");
        Assert.AreEqual("def foo():\n    pass", editor.Document.Text);
    }

    [STATestMethod]
    public void TestMoveLinesUpAndDown()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Line A\nLine B\nLine C";
        editor.TextArea.Caret.Line = 2;

        // Move Line B up -> Line B, Line A, Line C
        EditorOperations.MoveLinesUp(editor);
        Assert.StartsWith("Line B", editor.Document.Text);

        // Move Line B down -> Line A, Line B, Line C
        EditorOperations.MoveLinesDown(editor);
        Assert.StartsWith("Line A", editor.Document.Text);
    }

    [STATestMethod]
    public void TestDuplicateLinesDown()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Alpha";
        editor.TextArea.Caret.Line = 1;

        EditorOperations.DuplicateLinesDown(editor);
        Assert.AreEqual(2, editor.Document.LineCount);
        Assert.AreEqual("Alpha", editor.Document.GetLineByNumber(1) != null ? editor.Document.GetText(editor.Document.GetLineByNumber(1)) : "");
        Assert.AreEqual("Alpha", editor.Document.GetLineByNumber(2) != null ? editor.Document.GetText(editor.Document.GetLineByNumber(2)) : "");
    }

    [STATestMethod]
    public void TestDuplicateLinesUp()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Beta";
        editor.TextArea.Caret.Line = 1;

        EditorOperations.DuplicateLinesUp(editor);
        Assert.AreEqual(2, editor.Document.LineCount);
    }

    [STATestMethod]
    public void TestDeleteLines()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Line 1\nLine 2\nLine 3";
        editor.TextArea.Caret.Line = 2;

        EditorOperations.DeleteLines(editor);
        Assert.AreEqual(2, editor.Document.LineCount);
        Assert.DoesNotContain("Line 2", editor.Document.Text);
    }

    [STATestMethod]
    public void TestJoinLines()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Hello\n    World";
        editor.TextArea.Caret.Line = 1;

        EditorOperations.JoinLines(editor);
        Assert.AreEqual("Hello World", editor.Document.Text);
    }

    [STATestMethod]
    public void TestTransformCase()
    {
        var editor = new TextEditor();
        editor.Document.Text = "hello world";
        editor.TextArea.Selection = Selection.Create(editor.TextArea, 0, 5);

        EditorOperations.TransformToUppercase(editor);
        Assert.AreEqual("HELLO world", editor.Document.Text);

        EditorOperations.TransformToLowercase(editor);
        Assert.AreEqual("hello world", editor.Document.Text);
    }

    [STATestMethod]
    public void TestSortLines()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Zebra\nApple\nMango";

        EditorOperations.SortLines(editor);
        var lines = editor.Document.Text.Split(["\r\n", "\n"], StringSplitOptions.None);
        Assert.AreEqual("Apple", lines[0]);
        Assert.AreEqual("Mango", lines[1]);
        Assert.AreEqual("Zebra", lines[2]);
    }

    [STATestMethod]
    public void TestTrimTrailingWhitespace()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Line 1   \nLine 2\t\t\nLine 3";

        EditorOperations.TrimTrailingWhitespace(editor);
        Assert.AreEqual("Line 1\nLine 2\nLine 3", editor.Document.Text);
    }

    [STATestMethod]
    public void TestGoToLine()
    {
        var editor = new TextEditor();
        editor.Document.Text = "First\nSecond\nThird\nFourth";

        EditorOperations.GoToLine(editor, 3, 2);
        Assert.AreEqual(3, editor.TextArea.Caret.Line);
        Assert.AreEqual(2, editor.TextArea.Caret.Column);
    }

    [STATestMethod]
    public void TestAddColumnCursorDownAndUp()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Row 1: AAA\nRow 2: BBB\nRow 3: CCC";
        editor.TextArea.Caret.Line = 1;
        editor.TextArea.Caret.Column = 8;

        EditorOperations.AddColumnCursorDown(editor);
        Assert.IsTrue(editor.TextArea.Selection is RectangleSelection);

        var rectSel = (RectangleSelection)editor.TextArea.Selection;
        Assert.AreEqual(2, rectSel.Segments.Count());

        // Now extend down again
        EditorOperations.AddColumnCursorDown(editor);
        Assert.AreEqual(3, ((RectangleSelection)editor.TextArea.Selection).Segments.Count());
    }
}
