using System.IO;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.DeepSeek;
using RecluseEdit.Extensions.DeepSeek.Models;
using RecluseEdit.Extensions.DeepSeek.Services;
using RecluseEdit.Extensions.DeepSeek.Views;
using RecluseEdit.Sdk;

namespace RecluseEdit.Tests;

[TestClass]
public class DeepSeekExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<ISidePanelProvider> SidePanels { get; } = [];
        public List<string> Logs { get; } = [];

        public void RegisterSidePanel(ISidePanelProvider panelProvider) => SidePanels.Add(panelProvider);
        public IReadOnlyList<ISidePanelProvider> RegisteredSidePanels => SidePanels;
        public IWorkspaceContext WorkspaceContext => null!;
        public void Log(string message) => Logs.Add(message);

        public void RegisterLanguage(RecluseEdit.Sdk.Models.LanguageDefinition language) {}
        public void RegisterInlineCompletion(RecluseEdit.Sdk.Providers.IInlineCompletionProvider provider) {}
        public void RegisterIntelliSense(RecluseEdit.Sdk.Providers.IIntelliSenseProvider provider) {}
        public void RegisterToolchainCheck(RecluseEdit.Sdk.Providers.IToolchainCheck toolchainCheck) {}
        public void RegisterSyntaxHighlighting(string languageId, ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition definition) {}
        public IReadOnlyList<RecluseEdit.Sdk.Models.LanguageDefinition> GetRegisteredLanguages() => [];
    }

    private class MockApprovalWorkspaceContext : IWorkspaceContext
    {
        public bool ConfirmationResult { get; set; } = true;
        public string TempRoot { get; }

        public MockApprovalWorkspaceContext(string root)
        {
            TempRoot = root;
        }

        public string? WorkspaceRoot => TempRoot;
        public string? ActiveFilePath => Path.Combine(TempRoot, "test.txt");
        public string? ActiveDocumentContent => "Sample active content";
        public string? SelectedText => null;

        public void OpenFile(string path) {}

        public Task<string> ReadFileAsync(string path, CancellationToken ct = default)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(TempRoot, path);
            return File.ReadAllTextAsync(full, ct);
        }

        public Task WriteFileAsync(string path, string content, CancellationToken ct = default)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(TempRoot, path);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return File.WriteAllTextAsync(full, content, ct);
        }

        public Task<IReadOnlyList<string>> ListFilesAsync(string path = "", CancellationToken ct = default)
        {
            var full = string.IsNullOrEmpty(path) ? TempRoot : Path.Combine(TempRoot, path);
            if (!Directory.Exists(full)) return Task.FromResult<IReadOnlyList<string>>([]);
            var files = Directory.GetFileSystemEntries(full).Select(Path.GetFileName).ToList();
            return Task.FromResult<IReadOnlyList<string>>(files!);
        }

        public Task<CommandExecutionResult> ExecuteCommandAsync(string command, string? workingDirectory = null, CancellationToken ct = default)
        {
            if (!ConfirmationResult)
            {
                return Task.FromResult(new CommandExecutionResult
                {
                    ExitCode = -1,
                    StandardOutput = "",
                    StandardError = "Denied by user",
                    UserApproved = false
                });
            }

            return Task.FromResult(new CommandExecutionResult
            {
                ExitCode = 0,
                StandardOutput = "Command executed successfully",
                StandardError = "",
                UserApproved = true
            });
        }

        public Task<bool> RequestUserConfirmationAsync(string title, string prompt)
        {
            return Task.FromResult(ConfirmationResult);
        }
    }

    [TestMethod]
    public void TestDeepSeekMetadata()
    {
        var ext = new DeepSeekExtension();
        Assert.AreEqual("recluse.deepseek", ext.Id);
        Assert.AreEqual("DeepSeek AI Chat", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "DeepSeek AI");
    }

    [TestMethod]
    public async Task TestDeepSeekRegistration()
    {
        var ext = new DeepSeekExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        Assert.HasCount(1, host.SidePanels);
        var panel = host.SidePanels[0];
        Assert.AreEqual("deepseek.chat", panel.Id);
        Assert.AreEqual("DeepSeek AI Chat", panel.Title);
        Assert.AreEqual("🤖", panel.Icon);
    }

    [TestMethod]
    public void TestDeepSeekSettingsServicePersistence()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"deepseek_test_{Guid.NewGuid():N}.json");
        try
        {
            var svc = new DeepSeekSettingsService(tempFile);
            Assert.AreEqual("https://api.deepseek.com/chat/completions", svc.CurrentSettings.ApiEndpoint);
            Assert.AreEqual("deepseek-chat", svc.CurrentSettings.Model);

            svc.SaveSettings(new DeepSeekSettings
            {
                ApiEndpoint = "https://custom.ai/v1",
                ApiKey = "sk-test-12345",
                Model = "deepseek-coder",
                Temperature = 0.5
            });

            // Reload from same file
            var svcReloaded = new DeepSeekSettingsService(tempFile);
            Assert.AreEqual("https://custom.ai/v1", svcReloaded.CurrentSettings.ApiEndpoint);
            Assert.AreEqual("sk-test-12345", svcReloaded.CurrentSettings.ApiKey);
            Assert.AreEqual("deepseek-coder", svcReloaded.CurrentSettings.Model);
            Assert.AreEqual(0.5, svcReloaded.CurrentSettings.Temperature);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TestNormalizeEndpoint()
    {
        var client = new DeepSeekApiClient();
        Assert.AreEqual("https://api.deepseek.com/chat/completions", client.NormalizeEndpoint(""));
        Assert.AreEqual("https://api.deepseek.com/chat/completions", client.NormalizeEndpoint("https://api.deepseek.com"));
        Assert.AreEqual("https://api.deepseek.com/chat/completions", client.NormalizeEndpoint("https://api.deepseek.com/"));
        Assert.AreEqual("https://api.deepseek.com/chat/completions", client.NormalizeEndpoint("https://api.deepseek.com/chat/completions"));
        Assert.AreEqual("https://api.deepseek.com/v1/chat/completions", client.NormalizeEndpoint("https://api.deepseek.com/v1"));
        Assert.AreEqual("http://localhost:11434/v1/chat/completions", client.NormalizeEndpoint("http://localhost:11434/v1"));
    }

    [TestMethod]
    public void TestToolsDefinitionSchema()
    {
        var tools = DeepSeekApiClient.AvailableTools;
        Assert.HasCount(4, tools);
        Assert.IsTrue(tools.Any(t => t.Function.Name == "read_file"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "write_file"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "list_files"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "execute_command"));
    }

    [TestMethod]
    public async Task TestToolExecution_FileOperations()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"deepseek_workspace_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir);
            var client = new DeepSeekApiClient();

            // 1. Write file
            var writeResult = await client.ExecuteToolAsync("write_file", "{\"path\":\"hello.txt\",\"content\":\"Hello DeepSeek!\"}", context, CancellationToken.None);
            StringAssert.Contains(writeResult, "written successfully");
            Assert.IsTrue(File.Exists(Path.Combine(tempDir, "hello.txt")));

            // 2. Read file
            var readResult = await client.ExecuteToolAsync("read_file", "{\"path\":\"hello.txt\"}", context, CancellationToken.None);
            Assert.AreEqual("Hello DeepSeek!", readResult);

            // 3. List files
            var listResult = await client.ExecuteToolAsync("list_files", "{}", context, CancellationToken.None);
            StringAssert.Contains(listResult, "hello.txt");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task TestToolExecution_ExecuteCommand_WithApproval()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"deepseek_cmd_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir) { ConfirmationResult = true };
            var client = new DeepSeekApiClient();

            var result = await client.ExecuteToolAsync("execute_command", "{\"command\":\"echo test\"}", context, CancellationToken.None);
            StringAssert.Contains(result, "Exit Code: 0");
            StringAssert.Contains(result, "Command executed successfully");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task TestToolExecution_ExecuteCommand_WithDenial()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"deepseek_cmd_denial_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir) { ConfirmationResult = false };
            var client = new DeepSeekApiClient();

            var result = await client.ExecuteToolAsync("execute_command", "{\"command\":\"rm -rf /\"}", context, CancellationToken.None);
            StringAssert.Contains(result, "User confirmation denied");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [STATestMethod]
    public void TestDeepSeekSidePanelProviderCreatesView()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"deepseek_view_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir);
            var provider = new DeepSeekSidePanelProvider();
            var view = provider.CreateView(context);

            Assert.IsNotNull(view);
            Assert.IsInstanceOfType<DeepSeekChatView>(view);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void TestExtensionManagerSidePanelRegistration()
    {
        var syntaxManager = new SyntaxManager();
        var autocompleteManager = new AutocompleteManager();
        var toolchainManager = new ToolchainManager();
        var workspaceManager = new WorkspaceManager();
        var documentManager = new DocumentManager(syntaxManager);
        var workspaceContext = new WorkspaceContext(workspaceManager, documentManager);

        var extensionManager = new ExtensionManager(syntaxManager, autocompleteManager, toolchainManager, workspaceContext);

        ISidePanelProvider? registered = null;
        extensionManager.SidePanelRegistered += panel => registered = panel;

        var provider = new DeepSeekSidePanelProvider();
        extensionManager.RegisterSidePanel(provider);

        Assert.IsNotNull(registered);
        Assert.AreEqual("deepseek.chat", registered.Id);
        Assert.HasCount(1, extensionManager.RegisteredSidePanels);
        Assert.AreSame(provider, extensionManager.RegisteredSidePanels[0]);
    }
}
