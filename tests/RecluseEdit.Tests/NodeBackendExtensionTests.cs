using ICSharpCode.AvalonEdit.Highlighting;
using RecluseEdit.Extensions.NodeBackend;
using RecluseEdit.Extensions.NodeBackend.Providers;
using RecluseEdit.Extensions.NodeBackend.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class NodeBackendExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public Dictionary<string, IHighlightingDefinition> Syntaxes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) => Syntaxes[languageId] = definition;
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    [TestMethod]
    public void TestNodeBackendMetadata()
    {
        var ext = new NodeBackendExtension();
        Assert.AreEqual("recluse.nodebackend", ext.Id);
        Assert.AreEqual("Node Backend & Microservices Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        StringAssert.Contains(ext.Description, "NestJS", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task TestNodeBackendRegistration()
    {
        var ext = new NodeBackendExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // Inline providers (Nest, Fastify, Koa, Socket.io)
        Assert.HasCount(4, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "nodebackend.nestjs.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "nodebackend.fastify.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "nodebackend.koa.inline"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "nodebackend.socketio.inline"));

        // Toolchains (nest, pm2, fastify)
        Assert.HasCount(3, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "nest"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "pm2"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "fastify"));
    }

    [TestMethod]
    public async Task TestNestJsCompletionProvider()
    {
        var provider = new NestJsCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("typescript"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "@Controller('",
            TextBeforeCaret = "@Controller('",
            CaretOffset = 13,
            LineNumber = 1,
            ColumnNumber = 14,
            LanguageId = "typescript",
            FullText = "@Controller('"
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "ItemsController");

        var ctxModule = new InlineCompletionContext
        {
            CurrentLineText = "@Module({",
            TextBeforeCaret = "@Module({",
            CaretOffset = 9,
            LineNumber = 1,
            ColumnNumber = 10,
            LanguageId = "typescript",
            FullText = "@Module({"
        };
        var resModule = await provider.GetInlineSuggestionAsync(ctxModule);
        Assert.IsNotNull(resModule);
        StringAssert.Contains(resModule, "controllers:");
    }

    [TestMethod]
    public async Task TestFastifyCompletionProvider()
    {
        var provider = new FastifyCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("javascript"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "const fastify = require('fastify')(",
            TextBeforeCaret = "const fastify = require('fastify')(",
            CaretOffset = 35,
            LineNumber = 1,
            ColumnNumber = 36,
            LanguageId = "javascript",
            FullText = "const fastify = require('fastify')("
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        Assert.AreEqual("{ logger: true });", suggestion);
    }

    [TestMethod]
    public async Task TestKoaCompletionProvider()
    {
        var provider = new KoaCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("javascript"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "const Koa = require('koa');",
            TextBeforeCaret = "const Koa = require('koa');",
            CaretOffset = 27,
            LineNumber = 1,
            ColumnNumber = 28,
            LanguageId = "javascript",
            FullText = "const Koa = require('koa');"
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "new Koa()");
    }

    [TestMethod]
    public async Task TestSocketIoCompletionProvider()
    {
        var provider = new SocketIoCompletionProvider();
        Assert.IsTrue(provider.SupportedLanguages.Contains("typescript"));

        var ctx = new InlineCompletionContext
        {
            CurrentLineText = "const io = new Server(",
            TextBeforeCaret = "const io = new Server(",
            CaretOffset = 22,
            LineNumber = 1,
            ColumnNumber = 23,
            LanguageId = "typescript",
            FullText = "const io = new Server("
        };
        var suggestion = await provider.GetInlineSuggestionAsync(ctx);
        Assert.IsNotNull(suggestion);
        StringAssert.Contains(suggestion, "httpServer");
    }

    [TestMethod]
    public async Task TestNoCollisionWithExpressOrPython()
    {
        // Verify that NodeBackend providers do not trigger on Express triggers
        var nest = new NestJsCompletionProvider();
        var fastify = new FastifyCompletionProvider();
        var koa = new KoaCompletionProvider();
        var socketIo = new SocketIoCompletionProvider();

        var ctxExpress = new InlineCompletionContext
        {
            CurrentLineText = "const express = ",
            TextBeforeCaret = "const express = ",
            CaretOffset = 16,
            LineNumber = 1,
            ColumnNumber = 17,
            LanguageId = "javascript",
            FullText = "const express = "
        };

        Assert.IsNull(await nest.GetInlineSuggestionAsync(ctxExpress));
        Assert.IsNull(await fastify.GetInlineSuggestionAsync(ctxExpress));
        Assert.IsNull(await koa.GetInlineSuggestionAsync(ctxExpress));
        Assert.IsNull(await socketIo.GetInlineSuggestionAsync(ctxExpress));
    }

    [TestMethod]
    public async Task TestNodeBackendToolchains()
    {
        var nestCheck = new NestCliToolchainCheck();
        Assert.AreEqual("nest", nestCheck.Command);
        var nestReport = await nestCheck.CheckAsync();
        Assert.IsNotNull(nestReport);

        var pm2Check = new Pm2ToolchainCheck();
        Assert.AreEqual("pm2", pm2Check.Command);
        var pm2Report = await pm2Check.CheckAsync();
        Assert.IsNotNull(pm2Report);

        var fastifyCheck = new FastifyCliToolchainCheck();
        Assert.AreEqual("fastify", fastifyCheck.Command);
        var fastifyReport = await fastifyCheck.CheckAsync();
        Assert.IsNotNull(fastifyReport);
    }
}

