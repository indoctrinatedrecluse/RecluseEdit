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
}
