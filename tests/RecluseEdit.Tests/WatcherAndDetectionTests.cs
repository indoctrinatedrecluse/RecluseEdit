using System;
using System.IO;
using ICSharpCode.AvalonEdit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;
using RecluseEdit.UI.Controls;

namespace RecluseEdit.Tests;

[TestClass]
public class WatcherAndDetectionTests
{
    [TestMethod]
    public void TestIndentationDetector_Tabs()
    {
        var text = "function test() {\n\tconst a = 1;\n\tconst b = 2;\n}";
        var info = IndentationDetector.Detect(text);

        Assert.IsTrue(info.UseTabs);
        Assert.AreEqual(4, info.IndentSize);
        Assert.AreEqual("Tab Size: 4", info.ToString());
    }

    [TestMethod]
    public void TestIndentationDetector_TwoSpaces()
    {
        var text = "{\n  \"name\": \"recluse\",\n  \"version\": \"1.0.0\",\n  \"scripts\": {\n    \"start\": \"node index.js\"\n  }\n}";
        var info = IndentationDetector.Detect(text);

        Assert.IsFalse(info.UseTabs);
        Assert.AreEqual(2, info.IndentSize);
        Assert.AreEqual("Spaces: 2", info.ToString());
    }

    [TestMethod]
    public void TestIndentationDetector_FourSpaces()
    {
        var text = "class Example:\n    def __init__(self):\n        self.x = 10\n";
        var info = IndentationDetector.Detect(text);

        Assert.IsFalse(info.UseTabs);
        Assert.AreEqual(4, info.IndentSize);
        Assert.AreEqual("Spaces: 4", info.ToString());
    }

    [STATestMethod]
    public void TestIndentationConversion_TabsToSpaces()
    {
        var editor = new TextEditor();
        editor.Document.Text = "\tvar x = 1;\n\t\tvar y = 2;";

        IndentationDetector.ConvertTabsToSpaces(editor, 4);

        Assert.AreEqual("    var x = 1;\n        var y = 2;", editor.Document.Text);
    }

    [STATestMethod]
    public void TestIndentationConversion_SpacesToTabs()
    {
        var editor = new TextEditor();
        editor.Document.Text = "    var x = 1;\n        var y = 2;";

        IndentationDetector.ConvertSpacesToTabs(editor, 4);

        Assert.AreEqual("\tvar x = 1;\n\t\tvar y = 2;", editor.Document.Text);
    }

    [TestMethod]
    public void TestLineEndingDetector_CrlfAndLf()
    {
        var crlfText = "Line 1\r\nLine 2\r\nLine 3";
        var lfText = "Line 1\nLine 2\nLine 3";

        Assert.AreEqual(DocumentLineEnding.Crlf, LineEndingDetector.Detect(crlfText));
        Assert.AreEqual(DocumentLineEnding.Lf, LineEndingDetector.Detect(lfText));
    }

    [STATestMethod]
    public void TestLineEndingConversion()
    {
        var editor = new TextEditor();
        editor.Document.Text = "Line 1\nLine 2\nLine 3";

        LineEndingDetector.ConvertLineEndings(editor, DocumentLineEnding.Crlf);
        Assert.AreEqual("Line 1\r\nLine 2\r\nLine 3", editor.Document.Text);

        LineEndingDetector.ConvertLineEndings(editor, DocumentLineEnding.Lf);
        Assert.AreEqual("Line 1\nLine 2\nLine 3", editor.Document.Text);
    }

    [TestMethod]
    public void TestFileWatcherService_AutoReloadCleanDocument()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Original Content");

            var syntaxManager = new SyntaxManager();
            var docManager = new DocumentManager(syntaxManager);
            var doc = docManager.OpenDocument(tempFile);

            bool reloadedEventFired = false;
            using var watcher = new FileWatcherService(() => docManager.Documents);
            watcher.FileReloaded += d =>
            {
                if (d == doc) reloadedEventFired = true;
            };

            // Modify file on disk
            File.WriteAllText(tempFile, "Updated Disk Content");

            watcher.ProcessFileChange(tempFile, WatcherChangeTypes.Changed);

            Assert.IsTrue(reloadedEventFired, "Clean document should trigger FileReloaded");
            Assert.AreEqual("Updated Disk Content", doc.Document.Text);
            Assert.IsFalse(doc.IsDirty);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TestFileWatcherService_DetectsConflictForDirtyDocument()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Original Content");

            var syntaxManager = new SyntaxManager();
            var docManager = new DocumentManager(syntaxManager);
            var doc = docManager.OpenDocument(tempFile);

            // Dirty the document with local unsaved changes
            doc.Document.Text = "Local Unsaved Edit";
            Assert.IsTrue(doc.IsDirty);

            bool conflictFired = false;
            string? receivedDiskContent = null;

            using var watcher = new FileWatcherService(() => docManager.Documents);
            watcher.FileConflictDetected += (d, content) =>
            {
                if (d == doc)
                {
                    conflictFired = true;
                    receivedDiskContent = content;
                }
            };

            // External change on disk
            File.WriteAllText(tempFile, "External Editor Edit");

            watcher.ProcessFileChange(tempFile, WatcherChangeTypes.Changed);

            Assert.IsTrue(conflictFired, "Dirty document should trigger FileConflictDetected");
            Assert.AreEqual("External Editor Edit", receivedDiskContent);
            Assert.AreEqual("Local Unsaved Edit", doc.Document.Text, "Local changes must NOT be overwritten on conflict");
            Assert.IsTrue(doc.IsDirty);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TestLargeFileMode_Detection()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Write 2.1 million characters to exceed the 2M character threshold
            File.WriteAllText(tempFile, new string('x', 2_100_000));

            var syntaxManager = new SyntaxManager();
            var docManager = new DocumentManager(syntaxManager);
            var doc = docManager.OpenDocument(tempFile);

            Assert.IsTrue(doc.IsLargeFile, "Large file (>2M chars or >10MB) must have IsLargeFile = true");

            var normalFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(normalFile, "small file content");
                var normalDoc = docManager.OpenDocument(normalFile);
                Assert.IsFalse(normalDoc.IsLargeFile, "Standard file must have IsLargeFile = false");
            }
            finally
            {
                if (File.Exists(normalFile)) File.Delete(normalFile);
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
