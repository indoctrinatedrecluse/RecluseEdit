using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;
using RecluseEdit.Core.Services.Formatters;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class CliDocumentFormatterTests
{
    private class TestCliFormatter : CliDocumentFormatter
    {
        private readonly bool _forceAvailable;

        public override string FormatterId => "cli.test";
        public override string DisplayName => "Test Formatter (CLI)";
        public override string ExecutableName => "test_cli";
        public override System.Collections.Generic.IReadOnlyList<string> SupportedLanguages => ["custom", ".custom"];

        public TestCliFormatter(bool forceAvailable = true)
        {
            _forceAvailable = forceAvailable;
        }

        public override bool IsAvailable() => _forceAvailable;
    }

    [TestMethod]
    public void TestCliFormatterAvailabilityAndLanguageMatching()
    {
        var availableFormatter = new TestCliFormatter(forceAvailable: true);
        var unavailableFormatter = new TestCliFormatter(forceAvailable: false);

        Assert.IsTrue(availableFormatter.CanFormat("custom", "file.custom"));
        Assert.IsTrue(availableFormatter.CanFormat("other", "file.custom"));
        Assert.IsFalse(availableFormatter.CanFormat("unsupported", "file.txt"));

        Assert.IsFalse(unavailableFormatter.CanFormat("custom", "file.custom"));
    }

    [TestMethod]
    public void TestBuiltInCliFormattersProperties()
    {
        var prettier = new PrettierCliFormatter();
        Assert.AreEqual("cli.prettier", prettier.FormatterId);
        Assert.AreEqual("prettier", prettier.ExecutableName);
        Assert.IsTrue(prettier.SupportedLanguages.Contains("javascript"));
        Assert.IsTrue(prettier.SupportedLanguages.Contains("typescript"));
        Assert.IsTrue(prettier.SupportedLanguages.Contains("json"));
        Assert.IsTrue(prettier.SupportedLanguages.Contains("markdown"));

        var black = new BlackCliFormatter();
        Assert.AreEqual("cli.black", black.FormatterId);
        Assert.AreEqual("black", black.ExecutableName);
        Assert.IsTrue(black.SupportedLanguages.Contains("python"));

        var ruff = new RuffCliFormatter();
        Assert.AreEqual("cli.ruff", ruff.FormatterId);
        Assert.AreEqual("ruff", ruff.ExecutableName);
        Assert.IsTrue(ruff.SupportedLanguages.Contains("python"));

        var gofmt = new GoFmtCliFormatter();
        Assert.AreEqual("cli.gofmt", gofmt.FormatterId);
        Assert.AreEqual("gofmt", gofmt.ExecutableName);
        Assert.IsTrue(gofmt.SupportedLanguages.Contains("go"));

        var rustfmt = new RustFmtCliFormatter();
        Assert.AreEqual("cli.rustfmt", rustfmt.FormatterId);
        Assert.AreEqual("rustfmt", rustfmt.ExecutableName);
        Assert.IsTrue(rustfmt.SupportedLanguages.Contains("rust"));

        var dartfmt = new DartFormatCliFormatter();
        Assert.AreEqual("cli.dartformat", dartfmt.FormatterId);
        Assert.AreEqual("dart", dartfmt.ExecutableName);
        Assert.IsTrue(dartfmt.SupportedLanguages.Contains("dart"));
    }

    [TestMethod]
    public void TestDocumentFormattingServiceFallsBackWhenCliUnavailable()
    {
        var service = new DocumentFormattingService();

        // Even if external CLI tools like prettier or black are not in PATH,
        // DocumentFormattingService smoothly falls back to built-in pure C# formatters!
        string unformattedJson = "{\"name\":\"RecluseEdit\",\"version\":4}";
        string formattedJson = service.FormatDocument(unformattedJson, "json", "app.json");

        Assert.Contains(Environment.NewLine, formattedJson);
        Assert.Contains("  \"name\": \"RecluseEdit\"", formattedJson);
    }

    [TestMethod]
    public void TestFindExecutableInPathSafety()
    {
        // Non-existent executable returns null
        var result = CliDocumentFormatter.FindExecutableInPath("non_existent_binary_xyz_12345");
        Assert.IsNull(result);

        // Common system command (e.g. cmd or powershell or dotnet)
        var dotnetPath = CliDocumentFormatter.FindExecutableInPath("dotnet");
        Assert.IsNotNull(dotnetPath);
        Assert.IsTrue(File.Exists(dotnetPath));
    }
}
