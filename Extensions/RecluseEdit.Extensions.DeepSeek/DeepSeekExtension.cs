using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.DeepSeek;

/// <summary>
/// DeepSeek AI Chat Extension for RecluseEdit.
/// Provides a right-side panel conversational assistant with tool calling for reading, writing files, and secure command execution.
/// </summary>
public class DeepSeekExtension : IExtension
{
    public string Id => "recluse.deepseek";
    public string Name => "DeepSeek AI Chat";
    public string Version => "1.0.0";
    public string Description => "DeepSeek AI chat assistant with workspace file operations and permission-governed shell execution.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        host.Log("Initializing DeepSeek AI Chat extension...");

        var sidePanelProvider = new DeepSeekSidePanelProvider();
        host.RegisterSidePanel(sidePanelProvider);

        host.Log("DeepSeek AI Chat extension initialized successfully.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

