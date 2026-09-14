using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.AiChat;

/// <summary>
/// AI Chat Extension for RecluseEdit.
/// Provides a universal multi-model conversational assistant (DeepSeek, OpenAI, Antigravity/Gemini, Claude, Ollama)
/// with workspace file operations, inline authentication, in-chat model switching, and permission-governed shell execution.
/// </summary>
public class AiChatExtension : IExtension
{
    public string Id => "recluse.aichat";
    public string Name => "AI Chat";
    public string Version => "5.1.0";
    public string Description => "Universal AI chat assistant with multi-model switching, workspace file operations, and secure shell execution.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        host.Log("Initializing AI Chat extension...");

        var sidePanelProvider = new AiChatSidePanelProvider();
        host.RegisterSidePanel(sidePanelProvider);

        foreach (var provider in Services.AiProviderRegistry.Providers)
        {
            host.RegisterAiProvider(provider);
        }

        host.Log("AI Chat & Multi-Model Providers initialized successfully.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

