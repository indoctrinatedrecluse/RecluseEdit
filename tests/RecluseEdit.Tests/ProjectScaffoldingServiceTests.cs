using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class ProjectScaffoldingServiceTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "RecluseEdit_ScaffoldTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                // Unset any read-only attributes (e.g. .git files)
                foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp
        }
    }

    [TestMethod]
    public void TestGetTemplatesReturnsSevenTemplates()
    {
        var service = new ProjectScaffoldingService();
        var templates = service.GetTemplates();

        Assert.HasCount(7, templates);
        Assert.IsTrue(templates.Any(t => t.Id == "static-web"));
        Assert.IsTrue(templates.Any(t => t.Id == "vite-react-ts"));
        Assert.IsTrue(templates.Any(t => t.Id == "vite-vue-ts"));
        Assert.IsTrue(templates.Any(t => t.Id == "vite-svelte-ts"));
        Assert.IsTrue(templates.Any(t => t.Id == "vite-solid-ts"));
        Assert.IsTrue(templates.Any(t => t.Id == "fastify-api"));
        Assert.IsTrue(templates.Any(t => t.Id == "markdown-docs"));
    }

    [TestMethod]
    public async Task TestScaffoldStaticWeb()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-web-app";
        string projectDir = await service.ScaffoldProjectAsync("static-web", _tempDir, projectName, initGit: true);
        string entry = Path.Combine(projectDir, "index.html");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "style.css")));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "app.js")));
        Assert.IsTrue(Directory.Exists(Path.Combine(projectDir, ".git")));

        string htmlContent = await File.ReadAllTextAsync(entry);
        Assert.Contains(projectName, htmlContent);
    }

    [TestMethod]
    public async Task TestScaffoldViteReactTs()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-react-app";
        string projectDir = await service.ScaffoldProjectAsync("vite-react-ts", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "src", "App.tsx");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "package.json")));

        string pkg = await File.ReadAllTextAsync(Path.Combine(projectDir, "package.json"));
        Assert.Contains("react", pkg);
    }

    [TestMethod]
    public async Task TestScaffoldViteVueTs()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-vue-app";
        string projectDir = await service.ScaffoldProjectAsync("vite-vue-ts", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "src", "App.vue");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));

        string vueSfc = await File.ReadAllTextAsync(entry);
        Assert.Contains(projectName, vueSfc);
        Assert.Contains("{{ count }}", vueSfc);
    }

    [TestMethod]
    public async Task TestScaffoldViteSvelteTs()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-svelte-app";
        string projectDir = await service.ScaffoldProjectAsync("vite-svelte-ts", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "src", "App.svelte");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));

        string svelteCode = await File.ReadAllTextAsync(entry);
        Assert.Contains(projectName, svelteCode);
    }

    [TestMethod]
    public async Task TestScaffoldViteSolidTs()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-solid-app";
        string projectDir = await service.ScaffoldProjectAsync("vite-solid-ts", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "src", "App.tsx");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));

        string solidCode = await File.ReadAllTextAsync(entry);
        Assert.Contains(projectName, solidCode);
    }

    [TestMethod]
    public async Task TestScaffoldFastifyApi()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-fastify-api";
        string projectDir = await service.ScaffoldProjectAsync("fastify-api", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "src", "server.ts");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "package.json")));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "src", "routes", "items.ts")));

        string serverCode = await File.ReadAllTextAsync(entry);
        Assert.Contains("Fastify", serverCode);
    }

    [TestMethod]
    public async Task TestScaffoldMarkdownDocs()
    {
        var service = new ProjectScaffoldingService();
        string projectName = "my-docs";
        string projectDir = await service.ScaffoldProjectAsync("markdown-docs", _tempDir, projectName, initGit: false);
        string entry = Path.Combine(projectDir, "README.md");

        Assert.IsTrue(Directory.Exists(projectDir));
        Assert.IsTrue(File.Exists(entry));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "docs", "getting-started.md")));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "docs", "architecture.md")));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "docs", "api-reference.md")));

        string readme = await File.ReadAllTextAsync(entry);
        Assert.Contains("my-docs Documentation", readme);
    }
}
