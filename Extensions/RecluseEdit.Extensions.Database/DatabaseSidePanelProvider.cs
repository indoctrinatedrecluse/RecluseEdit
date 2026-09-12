using System.Windows;
using RecluseEdit.Extensions.Database.UI;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Database;

public class DatabaseSidePanelProvider : ISidePanelProvider
{
    public string Id => "recluse.database";
    public string Title => "Database";
    public string Icon => "🗄️";

    public FrameworkElement CreateView(IWorkspaceContext workspaceContext)
    {
        return new DatabasePanelView();
    }
}

