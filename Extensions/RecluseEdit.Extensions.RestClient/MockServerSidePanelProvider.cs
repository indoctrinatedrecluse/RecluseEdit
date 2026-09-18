using System.Windows;
using RecluseEdit.Extensions.RestClient.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.RestClient;

public class MockServerSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.mockserver";
    public string Title => "Mock Server";
    public string Icon => "🌐";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new MockServerView(workspaceContext);
    }
}

