using RecluseEdit.Extensions.NodeBackend.Providers;
using RecluseEdit.Extensions.NodeBackend.Toolchains;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.NodeBackend;

/// <summary>
/// RecluseEdit extension providing language support, snippets, completions, and compiler checks
/// for modern and enterprise Node.js backend frameworks: NestJS, Fastify, Koa, and Socket.io.
/// Meticulously excludes pre-existing frameworks (Flask, FastAPI, Django, Express) to avoid collisions.
/// </summary>
public class NodeBackendExtension : IExtension
{
    public string Id => "recluse.nodebackend";
    public string Name => "Node Backend & Microservices Pack";
    public string Version => "1.0.0";
    public string Description => "Enterprise NestJS, Fastify, Koa, and Socket.io backend support with CLI diagnostics.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Inline Autocomplete Providers
        host.RegisterInlineCompletion(new NestJsCompletionProvider());
        host.RegisterInlineCompletion(new FastifyCompletionProvider());
        host.RegisterInlineCompletion(new KoaCompletionProvider());
        host.RegisterInlineCompletion(new SocketIoCompletionProvider());

        // 2. Register Toolchain Checks
        host.RegisterToolchainCheck(new NestCliToolchainCheck());
        host.RegisterToolchainCheck(new Pm2ToolchainCheck());
        host.RegisterToolchainCheck(new FastifyCliToolchainCheck());

        host.Log("Node Backend & Microservices Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

