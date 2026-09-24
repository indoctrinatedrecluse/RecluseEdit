using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Remote;

public class RemoteExtension : IExtension
{
    public string Id => "recluse.remote";
    public string Name => "Remote & SSH Explorer";
    public string Version => "1.0.0";
    public string Description => "Provides MobaXterm-style SSH session management, SFTP browser with two-way sync, port forwarding tunnels, and network diagnostic tools.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken ct = default)
    {
        // 1. Register Remote & SSH Side Panel
        host.RegisterSidePanel(new RemoteSidePanelProvider());

        // 2. Register Status Bar item
        host.RegisterStatusBarItem(new StatusBarItem("remote.status", "🌐 SSH: Ready", StatusBarAlignment.Right, priority: 16)
        {
            Tooltip = "Remote & SSH Explorer - Ready. Click to open Remote panel.",
            OnClick = () => host.ShowSidePanel("recluse.remote")
        });

        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}

