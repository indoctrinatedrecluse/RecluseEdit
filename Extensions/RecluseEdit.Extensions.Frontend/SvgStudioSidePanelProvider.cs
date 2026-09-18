using System.Windows;
using RecluseEdit.Extensions.Frontend.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Frontend;

public class SvgStudioSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.svgstudio";
    public string Title => "SVG Studio";
    public string Icon => "📐";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new SvgStudioView(workspaceContext);
    }
}

