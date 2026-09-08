using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.Angular;
using RecluseEdit.Extensions.Flutter;
using RecluseEdit.Extensions.Php;
using RecluseEdit.Extensions.React;

namespace RecluseEdit.Tests;

[TestClass]
public class ExtensionManagerIntegrationTests
{
    [TestMethod]
    public async Task ExtensionManager_CanLoadAllExtensionsManually()
    {
        var syntaxManager = new SyntaxManager();
        var autocompleteManager = new AutocompleteManager();
        var toolchainManager = new ToolchainManager();

        var extensionManager = new ExtensionManager(syntaxManager, autocompleteManager, toolchainManager);

        // Load all extensions
        await extensionManager.LoadExtensionAsync(new ReactExtension());
        await extensionManager.LoadExtensionAsync(new AngularExtension());
        await extensionManager.LoadExtensionAsync(new FlutterExtension());
        await extensionManager.LoadExtensionAsync(new PhpExtension());

        // Verify loaded count (4 external)
        Assert.HasCount(4, extensionManager.LoadedExtensions);

        // Verify languages registered
        var languages = syntaxManager.SupportedLanguages;
        Assert.IsTrue(languages.Any(l => l.Id == "jsx"));
        Assert.IsTrue(languages.Any(l => l.Id == "tsx"));
        Assert.IsTrue(languages.Any(l => l.Id == "angular-html"));
        Assert.IsTrue(languages.Any(l => l.Id == "angular-ts"));
        Assert.IsTrue(languages.Any(l => l.Id == "dart"));
        Assert.IsTrue(languages.Any(l => l.Id == "php"));

        // Verify toolchains registered
        var checks = toolchainManager.RegisteredChecks;
        Assert.IsTrue(checks.Any(c => c.Command == "node"));
        Assert.IsTrue(checks.Any(c => c.Command == "npm"));
        Assert.IsTrue(checks.Any(c => c.Command == "tsc"));
        Assert.IsTrue(checks.Any(c => c.Command == "ng"));
        Assert.IsTrue(checks.Any(c => c.Command == "flutter"));
        Assert.IsTrue(checks.Any(c => c.Command == "dart"));
        Assert.IsTrue(checks.Any(c => c.Command == "php"));
        Assert.IsTrue(checks.Any(c => c.Command == "composer"));

        // Verify completions available across all languages
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "react.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "angular.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "flutter.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "php.inline.completion"));
    }

    [TestMethod]
    public async Task ToolchainManager_RunsAllChecksWithoutException()
    {
        var toolchainManager = new ToolchainManager();

        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Angular.Toolchains.AngularCliToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Flutter.Toolchains.FlutterToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Flutter.Toolchains.DartToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Php.Toolchains.PhpToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Php.Toolchains.ComposerToolchainCheck());

        await toolchainManager.RunAllChecksAsync();
        var reports = toolchainManager.Reports;

        Assert.HasCount(5, reports);
        foreach (var report in reports)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.ToolName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.Command));
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.Description));
        }
    }
}
