using System.Windows;
using RecluseEdit.Extensions.Remote.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Remote;

public class RemoteSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.remote";
    public string Title => "Remote & SSH";
    public string Icon => "🌐";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new RemotePanelView(workspaceContext);
    }
}

