using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.Angular;
using RecluseEdit.Extensions.Flutter;
using RecluseEdit.Extensions.Go;
using RecluseEdit.Extensions.Laravel;
using RecluseEdit.Extensions.Php;
using RecluseEdit.Extensions.Python;
using RecluseEdit.Extensions.React;
using RecluseEdit.Extensions.Ruby;

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

        // Load all 8 official extensions
        await extensionManager.LoadExtensionAsync(new ReactExtension());
        await extensionManager.LoadExtensionAsync(new AngularExtension());
        await extensionManager.LoadExtensionAsync(new FlutterExtension());
        await extensionManager.LoadExtensionAsync(new PhpExtension());
        await extensionManager.LoadExtensionAsync(new RubyExtension());
        await extensionManager.LoadExtensionAsync(new PythonExtension());
        await extensionManager.LoadExtensionAsync(new LaravelExtension());
        await extensionManager.LoadExtensionAsync(new GoExtension());

        // Verify loaded count (8 external)
        Assert.HasCount(8, extensionManager.LoadedExtensions);

        // Verify languages registered
        var languages = syntaxManager.SupportedLanguages;
        Assert.IsTrue(languages.Any(l => l.Id == "jsx"));
        Assert.IsTrue(languages.Any(l => l.Id == "tsx"));
        Assert.IsTrue(languages.Any(l => l.Id == "angular-html"));
        Assert.IsTrue(languages.Any(l => l.Id == "angular-ts"));
        Assert.IsTrue(languages.Any(l => l.Id == "dart"));
        Assert.IsTrue(languages.Any(l => l.Id == "php"));
        Assert.IsTrue(languages.Any(l => l.Id == "ruby"));
        Assert.IsTrue(languages.Any(l => l.Id == "erb"));
        Assert.IsTrue(languages.Any(l => l.Id == "python"));
        Assert.IsTrue(languages.Any(l => l.Id == "jinja"));
        Assert.IsTrue(languages.Any(l => l.Id == "blade"));
        Assert.IsTrue(languages.Any(l => l.Id == "laravel"));
        Assert.IsTrue(languages.Any(l => l.Id == "go"));
        Assert.IsTrue(languages.Any(l => l.Id == "gomod"));
        Assert.IsTrue(languages.Any(l => l.Id == "gotemplate"));

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
        Assert.IsTrue(checks.Any(c => c.Command == "ruby"));
        Assert.IsTrue(checks.Any(c => c.Command == "bundle"));
        Assert.IsTrue(checks.Any(c => c.Command == "rails"));
        Assert.IsTrue(checks.Any(c => c.Command == "python"));
        Assert.IsTrue(checks.Any(c => c.Command == "pip"));
        Assert.IsTrue(checks.Any(c => c.Command == "django-admin"));
        Assert.IsTrue(checks.Any(c => c.Command == "laravel"));
        Assert.IsTrue(checks.Any(c => c.Command == "php artisan"));
        Assert.IsTrue(checks.Any(c => c.Command == "go"));
        Assert.IsTrue(checks.Any(c => c.Command == "golangci-lint"));

        // Verify completions available across all languages
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "react.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "angular.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "flutter.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "php.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "ruby.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "rails.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "python.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "python.flask.django.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "python.frontends.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "express.inline.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "laravel.blade.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "laravel.eloquent.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "laravel.routes.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "go.core.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "go.backend.completion"));
        Assert.IsTrue(autocompleteManager.InlineProviders.Any(p => p.Id == "go.mod.completion"));
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
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Ruby.Toolchains.RubyToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Ruby.Toolchains.BundlerToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Ruby.Toolchains.RailsToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Python.Toolchains.PythonToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Python.Toolchains.PipToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Python.Toolchains.DjangoToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Laravel.Toolchains.LaravelCliToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Laravel.Toolchains.ArtisanToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Go.Toolchains.GoCompilerToolchainCheck());
        toolchainManager.RegisterCheck(new RecluseEdit.Extensions.Go.Toolchains.GolangciLintToolchainCheck());

        await toolchainManager.RunAllChecksAsync();
        var reports = toolchainManager.Reports;

        Assert.HasCount(15, reports);
        foreach (var report in reports)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.ToolName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.Command));
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.Description));
        }
    }
}
