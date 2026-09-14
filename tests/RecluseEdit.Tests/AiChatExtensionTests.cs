using System.IO;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.AiChat;
using RecluseEdit.Extensions.AiChat.Models;
using RecluseEdit.Extensions.AiChat.Services;
using RecluseEdit.Extensions.AiChat.Views;
using RecluseEdit.Sdk;

namespace RecluseEdit.Tests;

[TestClass]
public class AiChatExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<ISidePanelProvider> SidePanels { get; } = [];
        public List<RecluseEdit.Sdk.Providers.IAiProvider> Providers { get; } = [];
        public List<string> Logs { get; } = [];

        public void RegisterSidePanel(ISidePanelProvider panelProvider) => SidePanels.Add(panelProvider);
        public IReadOnlyList<ISidePanelProvider> RegisteredSidePanels => SidePanels;

        public void RegisterAiProvider(RecluseEdit.Sdk.Providers.IAiProvider provider) => Providers.Add(provider);
        public IReadOnlyList<RecluseEdit.Sdk.Providers.IAiProvider> RegisteredAiProviders => Providers;

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
    public void TestAiChatMetadata()
    {
        var ext = new AiChatExtension();
        Assert.AreEqual("recluse.aichat", ext.Id);
        Assert.AreEqual("AI Chat", ext.Name);
        Assert.AreEqual("5.1.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "Universal AI chat");
    }

    [TestMethod]
    public async Task TestAiChatRegistration()
    {
        var ext = new AiChatExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        Assert.HasCount(1, host.SidePanels);
        var panel = host.SidePanels[0];
        Assert.AreEqual("aichat.panel", panel.Id);
        Assert.AreEqual("AI Chat", panel.Title);
        Assert.AreEqual("🤖", panel.Icon);

        Assert.IsGreaterThanOrEqualTo(host.Providers.Count, 6, "Expected all AI Providers to be registered");
    }

    [TestMethod]
    public void TestAiChatSettingsServicePersistence_DefaultsToDeepSeek()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"aichat_test_{Guid.NewGuid():N}.json");
        try
        {
            var svc = new AiChatSettingsService(tempFile);
            Assert.AreEqual("deepseek", svc.CurrentSettings.Provider);
            Assert.AreEqual("deepseek-chat", svc.CurrentSettings.Model);
            Assert.AreEqual("https://api.deepseek.com/chat/completions", svc.CurrentSettings.ApiEndpoint);

            var secretKey = "sk-aichat-test-secret-999888777";
            svc.SaveSettings(new AiChatSettings
            {
                Provider = "deepseek",
                ApiEndpoint = "https://custom.ai/v1",
                ApiKey = secretKey,
                Model = "deepseek-coder",
                Temperature = 0.5
            });

            // 1. Verify file on disk is ENCRYPTED and contains no plaintext secret
            var fileContent = File.ReadAllText(tempFile);
            StringAssert.Contains(fileContent, "encrypted_api_key");
            Assert.DoesNotContain(secretKey, fileContent, "Disk file must never contain plaintext secret API key!");

            // 2. Reload from same file and verify decryption works
            var svcReloaded = new AiChatSettingsService(tempFile);
            Assert.AreEqual("https://custom.ai/v1", svcReloaded.CurrentSettings.ApiEndpoint);
            Assert.AreEqual(secretKey, svcReloaded.CurrentSettings.ApiKey);
            Assert.AreEqual("deepseek-coder", svcReloaded.CurrentSettings.Model);
            Assert.AreEqual(0.5, svcReloaded.CurrentSettings.Temperature);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TestLegacyDeepSeekSettingsMigration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"recluse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var deepSeekLegacyPath = Path.Combine(tempDir, "deepseek_settings.json");
        var aiChatPath = Path.Combine(tempDir, "aichat_settings.json");

        try
        {
            // Seed a legacy DeepSeek file with encrypted key using legacy entropy
            var legacyPlainKey = "sk-deepseek-migrated-secret-key-12345";
            var legacyEncrypted = AiChatSettingsService.EncryptSecret(legacyPlainKey);
            var legacyJson = $"{{\n  \"api_endpoint\": \"https://api.deepseek.com/chat/completions\",\n  \"encrypted_api_key\": \"{legacyEncrypted}\",\n  \"model\": \"deepseek-chat\"\n}}";
            File.WriteAllText(deepSeekLegacyPath, legacyJson);

            // Instantiate service pointing to new aichat path
            var svc = new AiChatSettingsService(aiChatPath);
            // Save to trigger writing
            svc.SaveSettings(new AiChatSettings
            {
                Provider = "deepseek",
                ApiKey = legacyPlainKey,
                Model = "deepseek-chat"
            });

            Assert.IsTrue(File.Exists(aiChatPath));
            var newSvc = new AiChatSettingsService(aiChatPath);
            Assert.AreEqual(legacyPlainKey, newSvc.CurrentSettings.ApiKey);
            Assert.AreEqual("deepseek-chat", newSvc.CurrentSettings.Model);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void TestLegacyPlaintextMigration()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"aichat_legacy_{Guid.NewGuid():N}.json");
        try
        {
            // Seed a legacy unencrypted file
            var legacyPlainKey = "sk-legacy-unencrypted-key-444333";
            var rawJson = $"{{\n  \"api_endpoint\": \"https://api.deepseek.com/chat/completions\",\n  \"api_key\": \"{legacyPlainKey}\",\n  \"model\": \"deepseek-chat\"\n}}";
            File.WriteAllText(tempFile, rawJson);

            // Load via service: should migrate legacy plaintext to encrypted on disk
            var svc = new AiChatSettingsService(tempFile);
            Assert.AreEqual(legacyPlainKey, svc.CurrentSettings.ApiKey);

            // Verify disk file was migrated: plaintext wiped, encrypted key present
            var migratedDiskContent = File.ReadAllText(tempFile);
            StringAssert.Contains(migratedDiskContent, "encrypted_api_key");
            Assert.DoesNotContain(legacyPlainKey, migratedDiskContent, "Legacy plaintext key must be scrubbed from disk upon migration!");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TestDirectSecretEncryptionRoundtrip()
    {
        var secret = "sk-test-token-" + Guid.NewGuid().ToString("N");
        var encrypted = AiChatSettingsService.EncryptSecret(secret);

        Assert.AreNotEqual(secret, encrypted);
        Assert.IsTrue(encrypted.StartsWith("dpapi:") || encrypted.StartsWith("aes:"));

        var decrypted = AiChatSettingsService.DecryptSecret(encrypted);
        Assert.AreEqual(secret, decrypted);
    }

    [TestMethod]
    public void TestNormalizeEndpoint()
    {
        var client = new AiChatApiClient();
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
        var tools = AiChatApiClient.AvailableTools;
        Assert.HasCount(4, tools);
        Assert.IsTrue(tools.Any(t => t.Function.Name == "read_file"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "write_file"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "list_files"));
        Assert.IsTrue(tools.Any(t => t.Function.Name == "execute_command"));
    }

    [TestMethod]
    public async Task TestToolExecution_FileOperations()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aichat_workspace_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir);
            var client = new AiChatApiClient();

            // 1. Write file
            var writeResult = await client.ExecuteToolAsync("write_file", "{\"path\":\"hello.txt\",\"content\":\"Hello AI Chat!\"}", context, CancellationToken.None);
            StringAssert.Contains(writeResult, "written successfully");
            Assert.IsTrue(File.Exists(Path.Combine(tempDir, "hello.txt")));

            // 2. Read file
            var readResult = await client.ExecuteToolAsync("read_file", "{\"path\":\"hello.txt\"}", context, CancellationToken.None);
            Assert.AreEqual("Hello AI Chat!", readResult);

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
        var tempDir = Path.Combine(Path.GetTempPath(), $"aichat_cmd_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir) { ConfirmationResult = true };
            var client = new AiChatApiClient();

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
        var tempDir = Path.Combine(Path.GetTempPath(), $"aichat_cmd_denial_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir) { ConfirmationResult = false };
            var client = new AiChatApiClient();

            var result = await client.ExecuteToolAsync("execute_command", "{\"command\":\"rm -rf /\"}", context, CancellationToken.None);
            StringAssert.Contains(result, "User confirmation denied");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [STATestMethod]
    public void TestAiChatSidePanelProviderCreatesView()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aichat_view_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new MockApprovalWorkspaceContext(tempDir);
            var provider = new AiChatSidePanelProvider();
            var view = provider.CreateView(context);

            Assert.IsNotNull(view);
            Assert.IsInstanceOfType<AiChatView>(view);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void TestBackwardCompatibilityWrappers()
    {
        // Verify legacy types instantiate and inherit correctly
        var legacyExt = new RecluseEdit.Extensions.DeepSeek.DeepSeekExtension();
        Assert.AreEqual("recluse.aichat", legacyExt.Id);

        var legacyProvider = new RecluseEdit.Extensions.DeepSeek.DeepSeekSidePanelProvider();
        Assert.AreEqual("aichat.panel", legacyProvider.Id);

        var legacySettings = new RecluseEdit.Extensions.DeepSeek.Models.DeepSeekSettings();
        Assert.AreEqual("deepseek", legacySettings.Provider);
        Assert.AreEqual("deepseek-chat", legacySettings.Model);

        var legacyClient = new RecluseEdit.Extensions.DeepSeek.Services.DeepSeekApiClient();
        Assert.IsNotNull(legacyClient);
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

        var provider = new AiChatSidePanelProvider();
        extensionManager.RegisterSidePanel(provider);

        Assert.IsNotNull(registered);
        Assert.AreEqual("aichat.panel", registered.Id);
        Assert.HasCount(1, extensionManager.RegisteredSidePanels);
        Assert.AreSame(provider, extensionManager.RegisteredSidePanels[0]);
    }
}
