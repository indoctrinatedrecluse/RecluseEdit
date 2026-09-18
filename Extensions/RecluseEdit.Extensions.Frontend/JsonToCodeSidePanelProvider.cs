using System.Windows;
using RecluseEdit.Extensions.Frontend.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Frontend;

public class JsonToCodeSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.jsontocode";
    public string Title => "JSON to Code";
    public string Icon => "🧩";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new JsonToCodeView(workspaceContext);
    }
}

