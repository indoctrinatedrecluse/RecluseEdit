using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class DocumentFormattingServiceTests
{
    [TestMethod]
    public void TestJsonFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "{\"name\":\"RecluseEdit\",\"version\":3,\"features\":[\"preview\",\"formatting\"]}";
        string formatted = service.FormatDocument(unformatted, "json", "config.json");

        Assert.Contains("\"name\": \"RecluseEdit\"", formatted);
        Assert.Contains("\"version\": 3", formatted);
    }

    [TestMethod]
    public void TestCssFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "body{margin:0;padding:0;}h1{color:#fff;}";
        string formatted = service.FormatDocument(unformatted, "css", "styles.css");

        Assert.Contains("body {", formatted);
        Assert.Contains("  margin: 0;", formatted);
        Assert.Contains("  padding: 0;", formatted);
        Assert.Contains("}", formatted);
        Assert.Contains("h1 {", formatted);
    }

    [TestMethod]
    public void TestHtmlFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "<div><header><h1>Title</h1></header><p>Content</p></div>";
        string formatted = service.FormatDocument(unformatted, "html", "index.html");

        Assert.Contains("<div>", formatted);
        Assert.Contains("<header>", formatted);
        Assert.Contains("<h1>", formatted);
        Assert.Contains("Title", formatted);
        Assert.Contains("</header>", formatted);
        Assert.Contains("<p>", formatted);
        Assert.Contains("Content", formatted);
        Assert.Contains("</div>", formatted);
    }

    [TestMethod]
    public void TestSqlFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "select id, name from users where active = 1 order by id desc";
        string formatted = service.FormatDocument(unformatted, "sql", "query.sql");

        Assert.Contains("SELECT", formatted);
        Assert.Contains("FROM users", formatted);
        Assert.Contains("WHERE active = 1", formatted);
        Assert.Contains("ORDER BY", formatted);
        Assert.Contains("id desc", formatted);
    }

    [TestMethod]
    public void TestJavaScriptFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "function greet(name) {\nif (name) {\nconsole.log('hello');\n}\n}";
        string formatted = service.FormatDocument(unformatted, "javascript", "app.js");

        Assert.Contains("function greet(name) {", formatted);
        Assert.Contains("  if (name) {", formatted);
        Assert.Contains("    console.log('hello');", formatted);
    }

    [TestMethod]
    public void TestMarkdownFormatting()
    {
        var service = new DocumentFormattingService();
        string unformatted = "# Heading 1\nSome text here\n## Heading 2\nMore text";
        string formatted = service.FormatDocument(unformatted, "markdown", "README.md");

        Assert.Contains("# Heading 1", formatted);
        Assert.Contains("Some text here", formatted);
        Assert.Contains("## Heading 2", formatted);
        Assert.Contains("More text", formatted);
    }

    private class CustomTestFormatter : IDocumentFormatter
    {
        public string FormatterId => "test.custom";
        public string DisplayName => "Custom Test Formatter";
        public IReadOnlyList<string> SupportedLanguages => ["customlang"];

        public bool CanFormat(string language, string filePath) => language == "customlang";

        public string Format(string text, string languageId, FormattingOptions options)
        {
            return "CUSTOM:" + text.Trim();
        }
    }

    [TestMethod]
    public void TestCustomExtensionFormatterDelegation()
    {
        var service = new DocumentFormattingService();
        service.RegisterFormatter(new CustomTestFormatter());

        string unformatted = "   hello world   ";
        string formatted = service.FormatDocument(unformatted, "customlang", "file.custom");

        Assert.AreEqual("CUSTOM:hello world", formatted);
    }
}
