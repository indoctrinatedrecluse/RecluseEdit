using System.Windows;
using RecluseEdit.Extensions.AiChat.Views;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.AiChat;

/// <summary>
/// Side panel provider exposing the unified AI Chat interface.
/// </summary>
public class AiChatSidePanelProvider : ISidePanelProvider
{
    public string Id => "aichat.panel";
    public string Title => "AI Chat";
    public string Icon => "🤖";

    public FrameworkElement CreateView(IWorkspaceContext context)
    {
        return new AiChatView(context);
    }
}
