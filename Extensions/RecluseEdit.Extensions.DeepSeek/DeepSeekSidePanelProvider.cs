using System.Windows;
using RecluseEdit.Extensions.DeepSeek.Views;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.DeepSeek;

public class DeepSeekSidePanelProvider : ISidePanelProvider
{
    public string Id => "deepseek.chat";
    public string Title => "DeepSeek AI Chat";
    public string Icon => "🤖";

    public FrameworkElement CreateView(IWorkspaceContext context)
    {
        return new DeepSeekChatView(context);
    }
}
