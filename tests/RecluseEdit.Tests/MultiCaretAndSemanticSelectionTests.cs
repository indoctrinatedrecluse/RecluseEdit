using ICSharpCode.AvalonEdit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class MultiCaretAndSemanticSelectionTests
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
    public void TestAddNextOccurrence_SelectsWordInitially()
    {
        var editor = CreateTestEditor("apple banana apple orange apple");
        editor.CaretOffset = 2; // Inside first "apple"

        var manager = new MultiSelectionManager();
        manager.AddNextOccurrence(editor);

        Assert.AreEqual(0, editor.SelectionStart);
        Assert.AreEqual(5, editor.SelectionLength);
        Assert.AreEqual("apple", editor.SelectedText);
        Assert.IsFalse(manager.HasSecondarySelections);
    }

    [STATestMethod]
    public void TestAddNextOccurrence_AddsSecondOccurrence()
    {
        var editor = CreateTestEditor("apple banana apple orange apple");
        editor.CaretOffset = 2;

        var manager = new MultiSelectionManager();
        manager.AddNextOccurrence(editor); // First: selects word
        manager.AddNextOccurrence(editor); // Second: adds next occurrence

        Assert.AreEqual(0, editor.SelectionStart);
        Assert.AreEqual(5, editor.SelectionLength);
        Assert.IsTrue(manager.HasSecondarySelections);
        Assert.HasCount(1, manager.SecondarySelections);
        Assert.AreEqual(13, manager.SecondarySelections[0].StartOffset);
        Assert.AreEqual(5, manager.SecondarySelections[0].Length);
    }

    [STATestMethod]
    public void TestSelectAllOccurrences()
    {
        var editor = CreateTestEditor("apple banana apple orange apple");
        editor.CaretOffset = 2;

        var manager = new MultiSelectionManager();
        manager.SelectAllOccurrences(editor);

        Assert.AreEqual(0, editor.SelectionStart);
        Assert.AreEqual(5, editor.SelectionLength);
        Assert.HasCount(2, manager.SecondarySelections);
        Assert.AreEqual(13, manager.SecondarySelections[0].StartOffset);
        Assert.AreEqual(26, manager.SecondarySelections[1].StartOffset);
    }

    [STATestMethod]
    public void TestMultiCaret_SimultaneousTextInput()
    {
        var editor = CreateTestEditor("var apple = 1; var apple = 2;");
        editor.CaretOffset = 5;

        var manager = new MultiSelectionManager();
        manager.SelectAllOccurrences(editor);

        bool handled = manager.HandleTextInput(editor, "fruit");

        Assert.IsTrue(handled);
        Assert.AreEqual("var fruit = 1; var fruit = 2;", editor.Document.Text);
    }

    [STATestMethod]
    public void TestMultiCaret_SimultaneousBackspace()
    {
        var editor = CreateTestEditor("var countX = 1; var countX = 2;");
        // Select first 'X' (at offset 9, length 1)
        editor.Select(9, 1);

        var manager = new MultiSelectionManager();
        manager.AddNextOccurrence(editor); // Adds second 'X' (at offset 25)
        Assert.HasCount(1, manager.SecondarySelections);

        bool handled = manager.HandleBackspace(editor);
        Assert.IsTrue(handled);
        Assert.AreEqual("var count = 1; var count = 2;", editor.Document.Text);
    }

    [STATestMethod]
    public void TestSemanticSelection_ExpandWord()
    {
        var editor = CreateTestEditor("int totalCount = 100;");
        editor.CaretOffset = 6; // Inside "totalCount"

        var service = new SemanticSelectionService();
        service.ExpandSelection(editor);

        Assert.AreEqual(4, editor.SelectionStart);
        Assert.AreEqual(10, editor.SelectionLength);
        Assert.AreEqual("totalCount", editor.SelectedText);
    }

    [STATestMethod]
    public void TestSemanticSelection_ExpandQuotes()
    {
        var editor = CreateTestEditor("string msg = \"hello world\";");
        editor.CaretOffset = 16; // Inside "hello"

        var service = new SemanticSelectionService();

        // Level 1: Word "hello"
        service.ExpandSelection(editor);
        Assert.AreEqual("hello", editor.SelectedText);

        // Level 2: Inside quotes "hello world"
        service.ExpandSelection(editor);
        Assert.AreEqual("hello world", editor.SelectedText);

        // Level 3: Including quotes "\"hello world\""
        service.ExpandSelection(editor);
        Assert.AreEqual("\"hello world\"", editor.SelectedText);
    }

    [STATestMethod]
    public void TestSemanticSelection_ExpandBracketsAndShrink()
    {
        var editor = CreateTestEditor("if (alpha && beta) { DoSomething(); }");
        editor.CaretOffset = 5; // Inside "alpha"

        var service = new SemanticSelectionService();

        // Level 1: Word "alpha"
        service.ExpandSelection(editor);
        Assert.AreEqual("alpha", editor.SelectedText);

        // Level 2: Inside parens "alpha && beta"
        service.ExpandSelection(editor);
        Assert.AreEqual("alpha && beta", editor.SelectedText);

        // Level 3: Including parens "(alpha && beta)"
        service.ExpandSelection(editor);
        Assert.AreEqual("(alpha && beta)", editor.SelectedText);

        // Shrink back to inside parens
        service.ShrinkSelection(editor);
        Assert.AreEqual("alpha && beta", editor.SelectedText);

        // Shrink back to word "alpha"
        service.ShrinkSelection(editor);
        Assert.AreEqual("alpha", editor.SelectedText);
    }
}
