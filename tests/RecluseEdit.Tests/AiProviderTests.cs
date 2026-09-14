using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions.AiChat;
using RecluseEdit.Extensions.AiChat.Models;
using RecluseEdit.Extensions.AiChat.Services;
using RecluseEdit.Extensions.AiChat.Views;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public class AiProviderTests
{
    private class TestHost : IExtensionHost
    {
        public List<IAiProvider> Providers { get; } = [];
        public List<ISidePanelProvider> SidePanels { get; } = [];
        public List<string> Logs { get; } = [];

        public void RegisterAiProvider(IAiProvider provider) => Providers.Add(provider);
        public IReadOnlyList<IAiProvider> RegisteredAiProviders => Providers;

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

    [TestMethod]
    public void AiProviderRegistry_ContainsAllRequiredProviders()
    {
        var providers = AiProviderRegistry.Providers;
        Assert.IsGreaterThanOrEqualTo(providers.Count, 6, "Expected at least 6 standard AI providers");

        var deepseek = providers.FirstOrDefault(p => p.Id == "deepseek");
        Assert.IsNotNull(deepseek);
        Assert.AreEqual(AiProviderType.DeepSeek, deepseek.ProviderType);
        CollectionAssert.Contains(deepseek.RecommendedModels.ToList(), "deepseek-chat");
        CollectionAssert.Contains(deepseek.RecommendedModels.ToList(), "deepseek-reasoner");

        var openai = providers.FirstOrDefault(p => p.Id == "openai");
        Assert.IsNotNull(openai);
        Assert.AreEqual(AiProviderType.OpenAi, openai.ProviderType);
        CollectionAssert.Contains(openai.RecommendedModels.ToList(), "gpt-4o");
        CollectionAssert.Contains(openai.RecommendedModels.ToList(), "o3-mini");

        var google = providers.FirstOrDefault(p => p.Id == "google_antigravity");
        Assert.IsNotNull(google);
        Assert.AreEqual(AiProviderType.GoogleAntigravity, google.ProviderType);
        CollectionAssert.Contains(google.RecommendedModels.ToList(), "gemini-2.5-flash");
        CollectionAssert.Contains(google.RecommendedModels.ToList(), "gemini-2.5-pro");

        var anthropic = providers.FirstOrDefault(p => p.Id == "anthropic");
        Assert.IsNotNull(anthropic);
        Assert.AreEqual(AiProviderType.Anthropic, anthropic.ProviderType);
        CollectionAssert.Contains(anthropic.RecommendedModels.ToList(), "claude-3-7-sonnet-20250219");

        var ollama = providers.FirstOrDefault(p => p.Id == "ollama");
        Assert.IsNotNull(ollama);
        Assert.AreEqual(AiProviderType.Ollama, ollama.ProviderType);
        Assert.AreEqual(AiAuthMode.LocalNoAuth, ollama.DefaultAuthMode);
        Assert.IsFalse(ollama.RequiresAuthentication);
        CollectionAssert.Contains(ollama.RecommendedModels.ToList(), "llama3.3");

        var custom = providers.FirstOrDefault(p => p.Id == "custom");
        Assert.IsNotNull(custom);
        Assert.AreEqual(AiProviderType.Custom, custom.ProviderType);
    }

    [TestMethod]
    public void AiProviderRegistry_GetProvider_HandlesCaseInsensitiveAndFallback()
    {
        var p1 = AiProviderRegistry.GetProvider("GOOGLE_ANTIGRAVITY");
        Assert.AreEqual("google_antigravity", p1.Id);

        var p2 = AiProviderRegistry.GetProvider("OpEnAi");
        Assert.AreEqual("openai", p2.Id);

        var fallback = AiProviderRegistry.GetProvider("non_existent_unknown_xyz");
        Assert.AreEqual("deepseek", fallback.Id);

        var fallbackNull = AiProviderRegistry.GetProvider(null);
        Assert.AreEqual("deepseek", fallbackNull.Id);
    }

    [TestMethod]
    public void AiProviderRegistry_NormalizeEndpoint_CorrectlyFormatsPerProvider()
    {
        var deepseek = AiProviderRegistry.GetProvider("deepseek");
        Assert.AreEqual("https://api.deepseek.com/chat/completions", deepseek.NormalizeEndpoint(""));
        Assert.AreEqual("https://api.deepseek.com/chat/completions", deepseek.NormalizeEndpoint("https://api.deepseek.com"));
        Assert.AreEqual("https://api.deepseek.com/v1/chat/completions", deepseek.NormalizeEndpoint("https://api.deepseek.com/v1"));

        var anthropic = AiProviderRegistry.GetProvider("anthropic");
        Assert.AreEqual("https://api.anthropic.com/v1/messages", anthropic.NormalizeEndpoint("https://api.anthropic.com"));
        Assert.AreEqual("https://api.anthropic.com/v1/messages", anthropic.NormalizeEndpoint("https://api.anthropic.com/v1/messages"));

        var ollama = AiProviderRegistry.GetProvider("ollama");
        Assert.AreEqual("http://localhost:11434/v1/chat/completions", ollama.NormalizeEndpoint("http://localhost:11434/v1"));
    }

    [TestMethod]
    public void DeepSeekSettings_EffectiveTokenAndAuthModes()
    {
        var settings = new DeepSeekSettings
        {
            Provider = "google_antigravity",
            AuthMode = "api_key",
            ApiKey = "test-api-key-123",
            AccountToken = "bearer-token-456"
        };

        Assert.AreEqual("test-api-key-123", settings.GetEffectiveToken());
        Assert.IsFalse(settings.IsLocalNoAuth);

        // Switch to account_token
        settings.AuthMode = "account_token";
        Assert.AreEqual("bearer-token-456", settings.GetEffectiveToken());

        // Switch to local_no_auth / ollama
        settings.Provider = "ollama";
        Assert.IsTrue(settings.IsLocalNoAuth);

        settings.Provider = "custom";
        settings.AuthMode = "local_no_auth";
        Assert.IsTrue(settings.IsLocalNoAuth);
    }

    [TestMethod]
    public void DeepSeekSettingsService_EncryptsAndPersistsSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"recluse_test_settings_{Guid.NewGuid():N}.json");
        try
        {
            var service = new DeepSeekSettingsService(tempFile);
            var settings = new DeepSeekSettings
            {
                Provider = "openai",
                AuthMode = "account_token",
                ApiKey = "secret-api-key",
                AccountToken = "secret-session-token",
                Model = "gpt-4o"
            };

            service.SaveSettings(settings);

            // Read raw JSON on disk
            var rawJson = File.ReadAllText(tempFile);
            StringAssert.DoesNotMatch(rawJson, new System.Text.RegularExpressions.Regex("secret-api-key"));

            // Load via service
            var loadedService = new DeepSeekSettingsService(tempFile);
            var loaded = loadedService.CurrentSettings;
            Assert.AreEqual("openai", loaded.Provider);
            Assert.AreEqual("account_token", loaded.AuthMode);
            Assert.AreEqual("secret-api-key", loaded.ApiKey);
            Assert.AreEqual("gpt-4o", loaded.Model);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task AiChatExtension_RegistersAllAiProvidersWithHost()
    {
        var host = new TestHost();
        var extension = new AiChatExtension();

        await extension.InitializeAsync(host);

        Assert.IsTrue(host.SidePanels.Any(p => p.Id == "aichat.panel"));
        Assert.IsGreaterThanOrEqualTo(host.Providers.Count, 6, "All AI providers should be registered with host");
        Assert.IsTrue(host.Providers.Any(p => p.Id == "google_antigravity"));
        Assert.IsTrue(host.Providers.Any(p => p.Id == "openai"));
        Assert.IsTrue(host.Providers.Any(p => p.Id == "ollama"));
    }

    [TestMethod]
    public async Task AiProviderRegistry_AutoDetectCredentials_EnvVarDetection()
    {
        var originalKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", "test-auto-detected-key");
            var res = await AiProviderRegistry.AutoDetectCredentialsAsync("deepseek");
            Assert.IsTrue(res.Found);
            Assert.AreEqual("test-auto-detected-key", res.Token);
            Assert.IsNotNull(res.SuggestedModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", originalKey);
        }
    }

    [TestMethod]
    public async Task AiProviderRegistry_AutoDetectCredentials_HandlesMissingGracefully()
    {
        var res = await AiProviderRegistry.AutoDetectCredentialsAsync("custom");
        Assert.IsFalse(res.Found);
        Assert.IsNotNull(res.Info);
    }

    [TestMethod]
    public async Task AiProviderRegistry_AutoDetectCredentials_OpenAi_ApiKey_And_ChatGPTToken()
    {
        var origOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var origChatGpt = Environment.GetEnvironmentVariable("CHATGPT_ACCESS_TOKEN");
        try
        {
            // 1. OPENAI_API_KEY
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-openai-env");
            Environment.SetEnvironmentVariable("CHATGPT_ACCESS_TOKEN", null);
            var res1 = await AiProviderRegistry.AutoDetectCredentialsAsync("openai");
            Assert.IsTrue(res1.Found);
            Assert.AreEqual("sk-test-openai-env", res1.Token);
            Assert.AreEqual("gpt-4o", res1.SuggestedModel);

            // 2. CHATGPT_ACCESS_TOKEN fallback
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("CHATGPT_ACCESS_TOKEN", "sess-chatgpt-token-123");
            var res2 = await AiProviderRegistry.AutoDetectCredentialsAsync("openai");
            Assert.IsTrue(res2.Found);
            Assert.AreEqual("sess-chatgpt-token-123", res2.Token);
            Assert.AreEqual("chatgpt-4o-latest", res2.SuggestedModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", origOpenAi);
            Environment.SetEnvironmentVariable("CHATGPT_ACCESS_TOKEN", origChatGpt);
        }
    }

    [TestMethod]
    public async Task AiProviderRegistry_AutoDetectCredentials_GoogleAntigravity_Keys()
    {
        var origGemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var origAntigravity = Environment.GetEnvironmentVariable("ANTIGRAVITY_API_KEY");
        try
        {
            // 1. GEMINI_API_KEY
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-gemini-key");
            Environment.SetEnvironmentVariable("ANTIGRAVITY_API_KEY", null);
            var res1 = await AiProviderRegistry.AutoDetectCredentialsAsync("google_antigravity");
            Assert.IsTrue(res1.Found);
            Assert.AreEqual("test-gemini-key", res1.Token);
            Assert.AreEqual("gemini-2.5-flash", res1.SuggestedModel);

            // 2. ANTIGRAVITY_API_KEY fallback
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_API_KEY", "test-antigravity-token");
            var res2 = await AiProviderRegistry.AutoDetectCredentialsAsync("google_antigravity");
            Assert.IsTrue(res2.Found);
            Assert.AreEqual("test-antigravity-token", res2.Token);
            Assert.AreEqual("gemini-2.5-flash", res2.SuggestedModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", origGemini);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_API_KEY", origAntigravity);
        }
    }

    [TestMethod]
    public async Task AiProviderRegistry_AutoDetectCredentials_Anthropic_Keys()
    {
        var origAnthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        var origClaude = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        try
        {
            // 1. ANTHROPIC_API_KEY
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-test-key");
            Environment.SetEnvironmentVariable("CLAUDE_API_KEY", null);
            var res1 = await AiProviderRegistry.AutoDetectCredentialsAsync("anthropic");
            Assert.IsTrue(res1.Found);
            Assert.AreEqual("sk-ant-test-key", res1.Token);

            // 2. CLAUDE_API_KEY fallback
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
            Environment.SetEnvironmentVariable("CLAUDE_API_KEY", "sk-claude-test-fallback");
            var res2 = await AiProviderRegistry.AutoDetectCredentialsAsync("anthropic");
            Assert.IsTrue(res2.Found);
            Assert.AreEqual("sk-claude-test-fallback", res2.Token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", origAnthropic);
            Environment.SetEnvironmentVariable("CLAUDE_API_KEY", origClaude);
        }
    }

    [TestMethod]
    public void ExtensionManager_AiProviderRegistrationAndEvents()
    {
        var syntax = new SyntaxManager();
        var auto = new AutocompleteManager();
        var toolchain = new ToolchainManager();
        var manager = new ExtensionManager(syntax, auto, toolchain);

        IAiProvider? registeredProvider = null;
        manager.AiProviderRegistered += p => registeredProvider = p;

        var descriptor = AiProviderRegistry.GetProvider("google_antigravity");
        manager.RegisterAiProvider(descriptor);

        Assert.HasCount(1, manager.RegisteredAiProviders);
        Assert.AreEqual("google_antigravity", manager.RegisteredAiProviders[0].Id);
        Assert.IsNotNull(registeredProvider);
        Assert.AreEqual("google_antigravity", registeredProvider.Id);
    }

    #region Mock Classes for API Client Testing

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public List<HttpRequestMessage> CapturedRequests { get; } = [];

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);
            return Task.FromResult(_handler(request));
        }
    }

    private class TestWorkspaceContext : IWorkspaceContext
    {
        public string? WorkspaceRoot { get; set; } = "C:\\test\\workspace";
        public string? ActiveFilePath { get; set; } = "C:\\test\\workspace\\Main.cs";
        public string? ActiveDocumentContent { get; set; } = "public class Main {}";
        public string? SelectedText { get; set; } = "Main";
        public Dictionary<string, string> VirtualFiles { get; } = [];
        public bool CommandApproved { get; set; } = true;
        public int CommandExitCode { get; set; } = 0;
        public string CommandStdout { get; set; } = "Done.";
        public string CommandStderr { get; set; } = "";

        public void OpenFile(string path) { }

        public Task<string> ReadFileAsync(string path, CancellationToken ct = default)
        {
            if (VirtualFiles.TryGetValue(path, out var content))
                return Task.FromResult(content);
            return Task.FromResult($"Content of {path}");
        }

        public Task WriteFileAsync(string path, string content, CancellationToken ct = default)
        {
            VirtualFiles[path] = content;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ListFilesAsync(string path = "", CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<string>>(VirtualFiles.Keys.ToList());
        }

        public Task<CommandExecutionResult> ExecuteCommandAsync(string command, string? workingDirectory = null, CancellationToken ct = default)
        {
            return Task.FromResult(new CommandExecutionResult
            {
                ExitCode = CommandExitCode,
                StandardOutput = CommandStdout,
                StandardError = CommandStderr,
                UserApproved = CommandApproved
            });
        }

        public Task<bool> RequestUserConfirmationAsync(string title, string prompt)
        {
            return Task.FromResult(true);
        }
    }

    #endregion

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_OpenAiProtocol_ParsesDeltasAndHeaders()
    {
        var sseResponse = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello \"}}]}\n\n" +
                          "data: {\"choices\":[{\"delta\":{\"content\":\"World!\"}}]}\n\n" +
                          "data: [DONE]\n\n";

        var handler = new MockHttpMessageHandler(req =>
        {
            var res = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(sseResponse, Encoding.UTF8, "text/event-stream")
            };
            return res;
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "openai",
            AuthMode = "api_key",
            ApiKey = "sk-test-token-123",
            Model = "gpt-4o"
        };

        var deltas = new List<string>();
        var result = await client.SendChatStreamAsync(
            settings,
            [new ChatMessage { Role = "user", Content = "Say hello" }],
            null,
            d => deltas.Add(d));

        Assert.AreEqual("Hello World!", result);
        Assert.HasCount(2, deltas);
        Assert.AreEqual("Hello ", deltas[0]);
        Assert.AreEqual("World!", deltas[1]);

        Assert.HasCount(1, handler.CapturedRequests);
        var req = handler.CapturedRequests[0];
        Assert.AreEqual("Bearer", req.Headers.Authorization?.Scheme);
        Assert.AreEqual("sk-test-token-123", req.Headers.Authorization?.Parameter);
    }

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_AnthropicProtocol_ParsesDeltasAndHeaders()
    {
        var sseResponse = "data: {\"type\":\"content_block_delta\",\"delta\":{\"type\":\"text_delta\",\"text\":\"Claude answer\"}}\n\n" +
                          "data: [DONE]\n\n";

        var handler = new MockHttpMessageHandler(req =>
        {
            var res = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(sseResponse, Encoding.UTF8, "text/event-stream")
            };
            return res;
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "anthropic",
            AuthMode = "api_key",
            ApiKey = "sk-ant-test-token",
            Model = "claude-3-7-sonnet-20250219"
        };

        var deltas = new List<string>();
        var result = await client.SendChatStreamAsync(
            settings,
            [new ChatMessage { Role = "user", Content = "Hello" }],
            null,
            d => deltas.Add(d));

        Assert.AreEqual("Claude answer", result);
        Assert.HasCount(1, handler.CapturedRequests);
        var req = handler.CapturedRequests[0];
        Assert.AreEqual("sk-ant-test-token", req.Headers.GetValues("x-api-key").FirstOrDefault());
        Assert.AreEqual("2023-06-01", req.Headers.GetValues("anthropic-version").FirstOrDefault());
    }

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_OllamaNoAuth_DoesNotSendAuthHeader()
    {
        var sseResponse = "data: {\"choices\":[{\"delta\":{\"content\":\"Local Ollama reply\"}}]}\n\n" +
                          "data: [DONE]\n\n";

        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(sseResponse, Encoding.UTF8, "text/event-stream")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "ollama",
            AuthMode = "local_no_auth",
            ApiKey = "",
            Model = "llama3.3"
        };

        var result = await client.SendChatStreamAsync(
            settings,
            [new ChatMessage { Role = "user", Content = "Test offline" }],
            null,
            _ => { });

        Assert.AreEqual("Local Ollama reply", result);
        Assert.HasCount(1, handler.CapturedRequests);
        var req = handler.CapturedRequests[0];
        Assert.IsNull(req.Headers.Authorization);
    }

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_ThrowsWhenCredentialsMissing()
    {
        var client = new DeepSeekApiClient();
        var settings = new DeepSeekSettings
        {
            Provider = "deepseek",
            AuthMode = "api_key",
            ApiKey = "",
            AccountToken = ""
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await client.SendChatStreamAsync(
                settings,
                [new ChatMessage { Role = "user", Content = "Hi" }],
                null,
                _ => { });
        });
    }

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"Invalid token\"}", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "openai",
            AuthMode = "api_key",
            ApiKey = "invalid-token",
            Model = "gpt-4o"
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await client.SendChatStreamAsync(
                settings,
                [new ChatMessage { Role = "user", Content = "Hi" }],
                null,
                _ => { });
        });

        StringAssert.Contains(ex.Message, "401");
        StringAssert.Contains(ex.Message, "Invalid token");
    }

    [TestMethod]
    public async Task DeepSeekApiClient_ExecuteToolAsync_FileOperations()
    {
        var client = new DeepSeekApiClient();
        var ctx = new TestWorkspaceContext();
        ctx.VirtualFiles["test.txt"] = "Hello world from virtual file";

        // Read file success
        var readSuccess = await client.ExecuteToolAsync("read_file", "{\"path\":\"test.txt\"}", ctx, CancellationToken.None);
        Assert.AreEqual("Hello world from virtual file", readSuccess);

        // Read file missing path
        var readMissing = await client.ExecuteToolAsync("read_file", "{}", ctx, CancellationToken.None);
        StringAssert.Contains(readMissing, "required");

        // Write file success
        var writeSuccess = await client.ExecuteToolAsync("write_file", "{\"path\":\"new.txt\",\"content\":\"Brand new file content\"}", ctx, CancellationToken.None);
        StringAssert.Contains(writeSuccess, "successfully");
        Assert.AreEqual("Brand new file content", ctx.VirtualFiles["new.txt"]);

        // Write file missing path
        var writeMissing = await client.ExecuteToolAsync("write_file", "{\"content\":\"test\"}", ctx, CancellationToken.None);
        StringAssert.Contains(writeMissing, "required");

        // List files non-empty
        var listRes = await client.ExecuteToolAsync("list_files", "{}", ctx, CancellationToken.None);
        StringAssert.Contains(listRes, "test.txt");
        StringAssert.Contains(listRes, "new.txt");

        // Unknown tool
        var unknown = await client.ExecuteToolAsync("unknown_tool_xyz", "{}", ctx, CancellationToken.None);
        StringAssert.Contains(unknown, "Unknown tool");
    }

    [TestMethod]
    public async Task DeepSeekApiClient_ExecuteToolAsync_ExecuteCommand_UserApproval()
    {
        var client = new DeepSeekApiClient();
        var ctx = new TestWorkspaceContext();

        // 1. User approves command
        ctx.CommandApproved = true;
        ctx.CommandExitCode = 0;
        ctx.CommandStdout = "All unit tests passed.";
        var cmdSuccess = await client.ExecuteToolAsync("execute_command", "{\"command\":\"dotnet test\"}", ctx, CancellationToken.None);
        StringAssert.Contains(cmdSuccess, "Exit Code: 0");
        StringAssert.Contains(cmdSuccess, "All unit tests passed.");

        // 2. User denies command
        ctx.CommandApproved = false;
        var cmdDenied = await client.ExecuteToolAsync("execute_command", "{\"command\":\"rm -rf /\"}", ctx, CancellationToken.None);
        StringAssert.Contains(cmdDenied, "User confirmation denied");
    }

    [TestMethod]
    public async Task DeepSeekApiClient_SendChatStreamAsync_ToolCalling_RecursesAndResolves()
    {
        int requestCount = 0;
        var sseToolCallResponse = "data: {\"choices\":[{\"delta\":{\"tool_calls\":[{\"index\":0,\"id\":\"call_001\",\"function\":{\"name\":\"read_file\",\"arguments\":\"{\\\"path\\\":\\\"notes.txt\\\"}\"}}]}}]}\n\n" +
                                  "data: [DONE]\n\n";

        var sseFinalResponse = "data: {\"choices\":[{\"delta\":{\"content\":\"Notes contain secret recipe\"}}]}\n\n" +
                               "data: [DONE]\n\n";

        var handler = new MockHttpMessageHandler(req =>
        {
            requestCount++;
            var body = requestCount == 1 ? sseToolCallResponse : sseFinalResponse;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "openai",
            AuthMode = "api_key",
            ApiKey = "sk-valid-key",
            Model = "gpt-4o"
        };

        var ctx = new TestWorkspaceContext();
        ctx.VirtualFiles["notes.txt"] = "secret recipe";

        var conversation = new List<ChatMessage> { new() { Role = "user", Content = "Read my notes" } };
        var result = await client.SendChatStreamAsync(
            settings,
            conversation,
            ctx,
            _ => { });

        Assert.AreEqual("Notes contain secret recipe", result);
        Assert.AreEqual(2, requestCount, "Expected 2 HTTP calls: 1st for tool call, 2nd after tool execution");
        Assert.IsTrue(conversation.Any(m => m.Role == "tool" && m.Content == "secret recipe"));
    }

    [TestMethod]
    public async Task DeepSeekApiClient_GenerateCompletionDirectAsync_BuildsChatPayload()
    {
        var sseResponse = "data: {\"choices\":[{\"delta\":{\"content\":\"Refactored code snippet\"}}]}\n\n" +
                          "data: [DONE]\n\n";

        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(sseResponse, Encoding.UTF8, "text/event-stream")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekApiClient(httpClient);
        var settings = new DeepSeekSettings
        {
            Provider = "google_antigravity",
            AuthMode = "api_key",
            ApiKey = "gemini-api-key-test",
            Model = "gemini-2.5-flash"
        };

        var descriptor = AiProviderRegistry.GetProvider("google_antigravity");
        var result = await client.GenerateCompletionDirectAsync(
            settings,
            descriptor,
            "Refactor this method",
            "You are an expert coder",
            _ => { });

        Assert.AreEqual("Refactored code snippet", result);
        Assert.HasCount(1, handler.CapturedRequests);
        var req = handler.CapturedRequests[0];
        Assert.AreEqual("Bearer", req.Headers.Authorization?.Scheme);
        Assert.AreEqual("gemini-api-key-test", req.Headers.Authorization?.Parameter);
    }
}
