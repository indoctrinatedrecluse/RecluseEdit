using System.IO;
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

    [TestMethod]
    public void TestReadmeDiagnostics()
    {
        var service = new DiagnosticService();
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "README.md")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        Assert.IsNotNull(dir);
        string text = File.ReadAllText(Path.Combine(dir, "README.md"));

        // Verify with both CRLF and LF line endings
        var diagCrlf = service.AnalyzeDocument(text.Replace("\r\n", "\n").Replace("\n", "\r\n"), "markdown", "README.md");
        Assert.IsEmpty(diagCrlf);

        var diagLf = service.AnalyzeDocument(text.Replace("\r\n", "\n"), "markdown", "README.md");
        Assert.IsEmpty(diagLf);
    }

    [TestMethod]
    public void TestMarkdownValidLinksAndFormattingProducesNoDiagnostics()
    {
        var service = new DiagnosticService();
        string md = """
            # Heading with (details)

            Here is a [link](https://github.com/indoctrinatedrecluse/RecluseEdit) and another [link](http://example.com/test_(with_parens)).
            And an image [![Badge](https://img.shields.io/badge/v4.0.1-blue.svg)](https://github.com).

            - [ ] Todo item
            - [x] Completed item

            Numbered list without opening parens:
            1) First step
            2) Second step
            a) Substep a
            b) Substep b

            Prose smileys: :) and :( and :-) and ;-)

            Inline code with special characters: `//` and `array[0]` and `func()` and `[new]`.

            <!-- HTML comment with [unclosed bracket -->
            """;

        var diagnostics = service.AnalyzeDocument(md, "markdown", "doc.md");
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void TestMarkdownUnclosedLinkBracketReported()
    {
        var service = new DiagnosticService();
        string md = "Click [here to read the documentation(https://example.com)\n";
        var diagnostics = service.AnalyzeDocument(md, "markdown", "test.md");

        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.Any(d => d.Message.Contains("Unclosed bracket '['")));
    }

    [TestMethod]
    public void TestMarkdownUnclosedUrlParenthesisReported()
    {
        var service = new DiagnosticService();
        string md = "Click [here](https://example.com and keep reading\n";
        var diagnostics = service.AnalyzeDocument(md, "markdown", "test.md");

        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.Any(d => d.Message.Contains("Unclosed bracket '('")));
    }

    [TestMethod]
    public void TestMarkdownUnclosedFencedCodeBlockReported()
    {
        var service = new DiagnosticService();
        string md = "```csharp\npublic void Foo() { }\n";
        var diagnostics = service.AnalyzeDocument(md, "markdown", "test.md");

        Assert.IsNotEmpty(diagnostics);
        Assert.IsTrue(diagnostics.Any(d => d.Message.Contains("Unclosed fenced code block")));
    }

    [TestMethod]
    public void TestPythonFloorDivisionAndHashCommentsNotMisread()
    {
        var service = new DiagnosticService();
        string py = """
            def calculate(a, b):
                # This is a comment with (an unclosed parenthesis
                result = (a // b)
                return result
            """;

        var diagnostics = service.AnalyzeDocument(py, "python", "script.py");
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void TestCodeBlockCommentsAndUrlsNotTreatedAsLineComments()
    {
        var service = new DiagnosticService();
        string csharp = """
            public class ApiClient
            {
                /* block comment with (unclosed paren */
                public string Url = "https://api.example.com";

                public void Connect()
                {
                    if (Url.StartsWith("https://"))
                    {
                        System.Console.WriteLine("Secure");
                    }
                }
            }
            """;

        var diagnostics = service.AnalyzeDocument(csharp, "csharp", "ApiClient.cs");
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void TestPlainTextFilesProduceNoDiagnostics()
    {
        var service = new DiagnosticService();
        string notes = "1) Buy milk\n2) Walk dog :)\nNotes: (unclosed paren is fine in prose\n";
        var diagnostics = service.AnalyzeDocument(notes, "plaintext", "notes.txt");
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void TestMarkdownVariousCodeFencePatternsProduceNoDiagnostics()
    {
        var service = new DiagnosticService();
        string md = """
            # Code Blocks Test

            Single-line block:
            ``` npm run build ```

            Prose starting with backticks:
            ``` is how to write a code block.

            Indented inside a list with spaces:
            1. First item:
               ```bash
               npm test
               ```

            Indented with a tab:
            	```json
            	{"key": "value"}
            	```

            Blockquote code block:
            > ```csharp
            > Console.WriteLine("hi");
            > ```

            Opening with 4 backticks, closing with 3:
            ````python
            print("hello")
            ```

            Closing with trailing spaces:
            ```
            code
            ```   
            """;

        var diagnostics = service.AnalyzeDocument(md, "markdown", "fences.md");
        Assert.IsEmpty(diagnostics);
    }
}

