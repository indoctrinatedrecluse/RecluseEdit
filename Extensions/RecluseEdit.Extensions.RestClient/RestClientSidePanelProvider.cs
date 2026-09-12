using System.Windows;
using RecluseEdit.Extensions.RestClient.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.RestClient;

public class RestClientSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.restclient";
    public string Title => "REST Client";
    public string Icon => "⚡";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new RestClientPanelView();
    }
}
