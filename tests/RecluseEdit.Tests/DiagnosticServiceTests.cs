using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class DiagnosticServiceTests
{
    [TestMethod]
    public void TestValidJsonProducesNoDiagnostics()
    {
        var service = new DiagnosticService();
        var diagnostics = service.AnalyzeDocument("{\"valid\": true, \"count\": 42}", "json", "test.json");
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void TestMalformedJsonReportsSyntaxError()
    {
        var service = new DiagnosticService();
        string malformed = "{\n  \"key\": \"value\",\n  \"trailing\":\n}";
        var diagnostics = service.AnalyzeDocument(malformed, "json", "test.json");

        Assert.IsNotEmpty(diagnostics);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.AreEqual("JSON Parser", diagnostics[0].Source);
        Assert.IsGreaterThanOrEqualTo(1, diagnostics[0].LineNumber);
    }

    [TestMethod]
    public void TestUnclosedBracketsReported()
    {
        var service = new DiagnosticService();
        string code = "function test() {\n    const x = [1, 2, 3;\n";
        var diagnostics = service.AnalyzeDocument(code, "javascript", "test.js");

        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.Any(d => d.Message.Contains("Unmatched closing") || d.Message.Contains("Unclosed")));
    }

    [TestMethod]
    public void TestHtmlUnclosedTagReported()
    {
        var service = new DiagnosticService();
        string html = "<div>\n  <span>Hello\n</div>";
        var diagnostics = service.AnalyzeDocument(html, "html", "index.html");

        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.Any(d => d.Message.Contains("span")));
    }

    [TestMethod]
    public void TestValidHtmlProducesNoDiagnostics()
    {
        var service = new DiagnosticService();
        string html = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><br><hr></head><body><h1>Title</h1></body></html>";
        var diagnostics = service.AnalyzeDocument(html, "html", "index.html");

        Assert.IsEmpty(diagnostics);
    }
}
