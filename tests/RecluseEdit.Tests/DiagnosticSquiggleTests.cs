using System;
using System.Collections.Generic;
using System.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.UI.Controls;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class DiagnosticSquiggleTests
{
    [TestMethod]
    public void TestDiagnosticMarkerRecordProperties()
    {
        var diag = new DiagnosticItem
        {
            FilePath = "test.js",
            LineNumber = 2,
            ColumnNumber = 5,
            Message = "Unexpected token",
            Severity = DiagnosticSeverity.Error,
            Source = "TestLinter"
        };

        var marker = new DiagnosticMarker(diag, StartOffset: 15, Length: 4, DiagnosticSeverity.Error, "Unexpected token");

        Assert.AreEqual(15, marker.StartOffset);
        Assert.AreEqual(4, marker.Length);
        Assert.AreEqual(DiagnosticSeverity.Error, marker.Severity);
        Assert.AreEqual("Unexpected token", marker.Message);
        Assert.AreEqual(diag, marker.Diagnostic);
    }

    [TestMethod]
    public void TestDiagnosticSquiggleRendererOffsetMappingAndQueries()
    {
        // Run on STA thread because TextView is a WPF Visual
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var textView = new TextView();
                var renderer = new DiagnosticSquiggleRenderer(textView);

                var doc = new TextDocument("function hello() {\n    return x + \n}\n");

                var diagnostics = new List<DiagnosticItem>
                {
                    new()
                    {
                        FilePath = "test.js",
                        LineNumber = 2,
                        ColumnNumber = 12,
                        Message = "Expression expected",
                        Severity = DiagnosticSeverity.Error,
                        Source = "Linter"
                    },
                    new()
                    {
                        FilePath = "test.js",
                        LineNumber = 1,
                        ColumnNumber = 10,
                        Message = "Unused function",
                        Severity = DiagnosticSeverity.Warning,
                        Source = "Linter"
                    }
                };

                renderer.SetDiagnostics(doc, diagnostics);

                var markers = renderer.GetMarkers();
                Assert.HasCount(2, markers);

                // Line 2, Column 12 is at '+' in line 2
                var line2 = doc.GetLineByNumber(2);
                int expectedOffsetLine2 = line2.Offset + 11;
                var markerAtLine2 = renderer.GetMarkerAtOffset(expectedOffsetLine2);
                Assert.IsNotNull(markerAtLine2);
                Assert.AreEqual(DiagnosticSeverity.Error, markerAtLine2.Severity);
                Assert.AreEqual("Expression expected", markerAtLine2.Message);

                // Line 1, Column 10 is 'hello'
                var line1 = doc.GetLineByNumber(1);
                int expectedOffsetLine1 = line1.Offset + 9;
                var markerAtLine1 = renderer.GetMarkerAtOffset(expectedOffsetLine1);
                Assert.IsNotNull(markerAtLine1);
                Assert.AreEqual(DiagnosticSeverity.Warning, markerAtLine1.Severity);
                Assert.AreEqual(5, markerAtLine1.Length); // "hello" is 5 characters!

                // Query outside markers returns null
                var noMarker = renderer.GetMarkerAtOffset(0);
                Assert.IsNull(noMarker);

                // Clearing markers
                renderer.Clear();
                Assert.IsEmpty(renderer.GetMarkers());
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw threadEx;
        }
    }
}
