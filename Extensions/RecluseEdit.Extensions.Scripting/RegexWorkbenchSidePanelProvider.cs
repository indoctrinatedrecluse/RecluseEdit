using System.Windows;
using RecluseEdit.Extensions.Scripting.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Scripting;

public class RegexWorkbenchSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.regex";
    public string Title => "Regex";
    public string Icon => "🧪";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new RegexWorkbenchView(workspaceContext);
    }
}

