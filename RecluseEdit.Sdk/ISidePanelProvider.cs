using System.Windows;

namespace RecluseEdit.Sdk;

/// <summary>
/// Defines a provider for an editor side panel (e.g. Right Pane tool window or AI Chat view).
/// </summary>
public interface ISidePanelProvider
{
    string Id { get; }
    string Title { get; }
    string Icon { get; }

    /// <summary>
    /// Creates and returns the WPF user interface element to host in the editor side panel.
    /// </summary>
    FrameworkElement CreateView(IWorkspaceContext workspaceContext);
}
