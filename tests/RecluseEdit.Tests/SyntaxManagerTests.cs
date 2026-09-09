using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class SyntaxManagerTests
{
    [TestMethod]
    public void SyntaxManager_RegistersDefaultLanguages()
    {
        var manager = new SyntaxManager();
        Assert.IsNotNull(manager.GetLanguageById("html"));
        Assert.IsNotNull(manager.GetLanguageById("css"));
        Assert.IsNotNull(manager.GetLanguageById("javascript"));
        Assert.IsNotNull(manager.GetLanguageById("typescript"));
        Assert.IsNotNull(manager.GetLanguageById("json"));
        Assert.IsNotNull(manager.GetLanguageById("xml"));
        Assert.IsNotNull(manager.GetLanguageById("csharp"));
        Assert.IsNotNull(manager.GetLanguageById("markdown"));
        Assert.IsNotNull(manager.GetLanguageById("php"));
    }

    [TestMethod]
    public void SyntaxManager_ResolvesFileExtensions()
    {
        var manager = new SyntaxManager();
        Assert.AreEqual("html", manager.GetLanguageForFile("index.html").Id);
        Assert.AreEqual("css", manager.GetLanguageForFile("styles.css").Id);
        Assert.AreEqual("javascript", manager.GetLanguageForFile("app.js").Id);
        Assert.AreEqual("typescript", manager.GetLanguageForFile("app.ts").Id);
        Assert.AreEqual("json", manager.GetLanguageForFile("package.json").Id);
        Assert.AreEqual("xml", manager.GetLanguageForFile("MainWindow.xaml").Id);
        Assert.AreEqual("xml", manager.GetLanguageForFile("App.config").Id);
        Assert.AreEqual("markdown", manager.GetLanguageForFile("README.md").Id);
        Assert.AreEqual("php", manager.GetLanguageForFile("index.php").Id);
    }

    [TestMethod]
    public void BuiltInDefinitions_CanBeRetrieved()
    {
        var manager = new SyntaxManager();
        var htmlDef = manager.GetHighlighting(manager.GetLanguageById("html")!);
        var cssDef = manager.GetHighlighting(manager.GetLanguageById("css")!);
        var jsDef = manager.GetHighlighting(manager.GetLanguageById("javascript")!);
        var tsDef = manager.GetHighlighting(manager.GetLanguageById("typescript")!);
        var xmlDef = manager.GetHighlighting(manager.GetLanguageById("xml")!);
        var jsonDef = manager.GetHighlighting(manager.GetLanguageById("json")!);
        var phpDef = manager.GetHighlighting(manager.GetLanguageById("php")!);
        var mdDef = manager.GetHighlighting(manager.GetLanguageById("markdown")!);

        Assert.IsNotNull(htmlDef);
        Assert.IsNotNull(cssDef);
        Assert.IsNotNull(jsDef);
        Assert.IsNotNull(tsDef);
        Assert.IsNotNull(xmlDef);
        Assert.IsNotNull(jsonDef);
        Assert.IsNotNull(phpDef);
        Assert.IsNotNull(mdDef);
        Assert.AreEqual("JSON", jsonDef.Name);
        Assert.AreEqual("Markdown", mdDef.Name);
        Assert.AreEqual("JavaScriptModern", jsDef.Name);
        Assert.AreEqual("CSSModern", cssDef.Name);
        Assert.AreEqual("PHPModern", phpDef.Name);
    }

    [TestMethod]
    public void JsonHighlighting_HighlightsJsonDocumentWithoutExceptions()
    {
        var manager = new SyntaxManager();
        var jsonDef = manager.GetHighlighting(manager.GetLanguageById("json")!);
        Assert.IsNotNull(jsonDef);

        var document = new ICSharpCode.AvalonEdit.Document.TextDocument("""
        {
          "compilerOptions": {
            "tsBuildInfoFile": "./node_modules/.tmp/tsconfig.app.tsbuildinfo",
            "target": "es2023",
            "lib": ["ES2023", "DOM"],
            "module": "esnext",
            "types": ["vite/client"],
            "skipLibCheck": true,

            /* Bundler mode */
            "moduleResolution": "bundler",
            "allowImportingTsExtensions": true,
            "verbatimModuleSyntax": true,
            "moduleDetection": "force",
            "noEmit": true,
            "jsx": "react-jsx",

            /* Linting */
            "noUnusedLocals": true,
            "noUnusedParameters": true,
            "erasableSyntaxOnly": true,
            "noFallthroughCasesInSwitch": true,
            "path": "C:\\Users\\test",
            "escaped": "Hello \"World\" \\\\ test"
          },
          "include": ["src"]
        }
        """);

        var highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(document, jsonDef);
        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = highlighter.HighlightLine(i);
            Assert.IsNotNull(line);
        }
    }

    [TestMethod]
    public void MarkdownHighlighting_HighlightsMarkdownDocumentWithoutExceptions()
    {
        var manager = new SyntaxManager();
        var mdDef = manager.GetHighlighting(manager.GetLanguageById("markdown")!);
        Assert.IsNotNull(mdDef);

        var document = new ICSharpCode.AvalonEdit.Document.TextDocument("""
        # Project Title
        This is a paragraph with **bold** and *italic* text.

        ## Features
        - Item 1
        - Item 2
        1. Numbered item

        ### Code Example
        ```javascript
        const message = "Hello, world!";
        console.log(message);
        ```

        > Blockquote example

        [Link to Google](https://google.com)
        """);

        var highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(document, mdDef);
        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = highlighter.HighlightLine(i);
            Assert.IsNotNull(line);
        }
    }

    [TestMethod]
    public void PhpHighlighting_HighlightsPhpWithHashCommentsAndAttributesWithoutExceptions()
    {
        var manager = new SyntaxManager();
        var phpDef = manager.GetHighlighting(manager.GetLanguageById("php")!);
        Assert.IsNotNull(phpDef);

        var document = new ICSharpCode.AvalonEdit.Document.TextDocument("""
        <?php
        // Standard slash comment
        # Standard hash comment
        #[Route('/api/v1', methods: ['GET'])]
        class UserController {
            public function index() {
                $msg = "Hello \"world\" and {$variable}";
                $single = 'Single \'quoted\' string';
                return $msg;
            }
        }
        ?>
        """);

        var highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(document, phpDef);
        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = highlighter.HighlightLine(i);
            Assert.IsNotNull(line);
        }
    }

    [TestMethod]
    public void AllSupportedLanguages_HighlightVariousCodeSnippetsWithoutExceptions()
    {
        var manager = new SyntaxManager();
        var testSnippets = new[]
        {
            "",
            "   \t  ",
            "Single line of plain text",
            "\"escaped \\\"quote\\\" and \\\\ slash\"",
            "'single \\'quote\\''",
            "`template ${value} literal`",
            "// comment line",
            "# hash comment line",
            "/* block comment */",
            "<!-- xml comment -->",
            "0 123 0xDEADBEEF 3.14159",
            "{ [ ( ) ] } ; : , .",
            "function test(arg1, arg2) { return true; }"
        };

        foreach (var lang in manager.SupportedLanguages)
        {
            var def = manager.GetHighlighting(lang);
            if (def == null) continue;

            foreach (var snippet in testSnippets)
            {
                var doc = new ICSharpCode.AvalonEdit.Document.TextDocument(snippet);
                var highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(doc, def);
                for (int i = 1; i <= doc.LineCount; i++)
                {
                    var line = highlighter.HighlightLine(i);
                    Assert.IsNotNull(line);
                }
            }
        }
    }

    [TestMethod]
    public void ExtensionSyntaxes_HighlightVariousCodeSnippetsWithoutExceptions()
    {
        var definitions = new Dictionary<string, IHighlightingDefinition>
        {
            ["python"] = RecluseEdit.Extensions.Python.Syntaxes.PythonSyntaxDefinition.CreateDefinition(),
            ["ruby"] = RecluseEdit.Extensions.Ruby.Syntaxes.RubySyntaxDefinition.CreateDefinition(),
            ["dart"] = RecluseEdit.Extensions.Flutter.Syntaxes.DartSyntaxDefinition.CreateDefinition(),
            ["go"] = RecluseEdit.Extensions.Go.Syntaxes.GoSyntaxDefinition.CreateDefinition(),
            ["gomod"] = RecluseEdit.Extensions.Go.Syntaxes.GoModSyntaxDefinition.CreateDefinition(),
            ["jsx"] = RecluseEdit.Extensions.React.Syntaxes.JsxSyntaxDefinition.CreateDefinition(),
            ["graphql"] = RecluseEdit.Extensions.React.Syntaxes.GraphQlSyntaxDefinition.CreateDefinition(),
            ["angular-ts"] = RecluseEdit.Extensions.Angular.Syntaxes.AngularTsSyntaxDefinition.CreateDefinition(),
            ["angular-html"] = RecluseEdit.Extensions.Angular.Syntaxes.AngularHtmlSyntaxDefinition.CreateDefinition(),
            ["blade"] = RecluseEdit.Extensions.Laravel.Syntaxes.BladeSyntaxDefinition.CreateDefinition()
        };

        var testSnippets = new[]
        {
            "",
            "   \t  ",
            "Single line of code",
            "\"escaped \\\"quote\\\" and \\\\ slash\"",
            "'single \\'quote\\''",
            "`template ${value} literal`",
            "// comment line",
            "# hash comment line",
            "/* block comment */",
            "<!-- xml comment -->",
            "0 123 0xDEADBEEF 3.14159",
            "{ [ ( ) ] } ; : , .",
            "func main() { fmt.Println(\"Hello \\\"World\\\"\") }",
            "def foo(bar):\n    # comment\n    return f\"hello {bar} \\\"world\\\"\"",
            "class User < ApplicationRecord\n  # comment\n  validates :name, presence: true\nend"
        };

        foreach (var (lang, def) in definitions)
        {
            foreach (var snippet in testSnippets)
            {
                var doc = new ICSharpCode.AvalonEdit.Document.TextDocument(snippet);
                var highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(doc, def);
                for (int i = 1; i <= doc.LineCount; i++)
                {
                    var line = highlighter.HighlightLine(i);
                    Assert.IsNotNull(line, $"Failed highlighting {lang} for snippet: {snippet}");
                }
            }
        }
    }

    [TestMethod]
    public void InvalidSyntax_FallsBackGracefullyWithoutThrowing()
    {
        var manager = new SyntaxManager();
        // Definition with zero-character rule
        const string badXshd = """
            <?xml version="1.0"?>
            <SyntaxDefinition name="BadGrammar" extensions=".bad" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                <RuleSet>
                    <Rule>
                        ^
                    </Rule>
                </RuleSet>
            </SyntaxDefinition>
            """;
        using var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(badXshd));
        var badDef = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);

        var badLang = new RecluseEdit.Sdk.Models.LanguageDefinition
        {
            Id = "bad",
            DisplayName = "Bad Grammar",
            Extensions = [".bad"]
        };
        manager.RegisterLanguage(badLang);
        manager.RegisterSyntaxDefinition("bad", badDef);

        // Pre-test validation in an editor scenario should catch the exception and fall back
        var doc = new ICSharpCode.AvalonEdit.Document.TextDocument("Some test content");
        IHighlightingDefinition? appliedHighlighting = null;
        try
        {
            var def = manager.GetHighlighting(badLang);
            if (def != null)
            {
                var testHighlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(doc, def);
                for (int i = 1; i <= doc.LineCount; i++)
                {
                    testHighlighter.HighlightLine(i);
                }
            }
            appliedHighlighting = def;
        }
        catch
        {
            // Defensive fallback as implemented in EditorControl
            appliedHighlighting = null;
        }

        Assert.IsNull(appliedHighlighting, "Should have fallen back to null (PlainText) without crashing");
    }
}
