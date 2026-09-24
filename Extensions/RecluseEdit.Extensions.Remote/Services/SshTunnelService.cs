using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using RecluseEdit.Extensions.Remote.Models;
using Renci.SshNet;

namespace RecluseEdit.Extensions.Remote.Services;

/// <summary>
/// Manages background SSH port forwarding tunnels (MobaXterm style) using SSH.NET.
/// </summary>
public class SshTunnelService : IDisposable
{
    private class ActiveTunnel
    {
        public required SshClient Client { get; init; }
        public required ForwardedPortLocal Port { get; init; }
        public required SshTunnelConfig Config { get; init; }
    }

    private readonly ConcurrentDictionary<string, ActiveTunnel> _tunnels = new();

    public event Action<SshTunnelConfig, bool>? TunnelStatusChanged; // config, isRunning

    public int ActiveTunnelCount => _tunnels.Count;

    public bool IsTunnelRunning(string tunnelId) => _tunnels.ContainsKey(tunnelId);

    public async Task StartTunnelAsync(SshTunnelConfig config)
    {
        if (_tunnels.ContainsKey(config.Id)) return;

        await Task.Run(() =>
        {
            var profile = new SshSessionProfile
            {
                Host = config.SshHost,
                Port = config.SshPort,
                Username = config.SshUser,
                AuthType = config.AuthType,
                Password = config.SshPassword,
                PrivateKeyPath = config.SshKeyPath
            };

            var conn = SftpBrowserService.CreateConnectionInfo(profile);
            var client = new SshClient(conn);
            client.Connect();

            var port = new ForwardedPortLocal("127.0.0.1", config.LocalPort, config.RemoteHost, config.RemotePort);
            client.AddForwardedPort(port);
            port.Start();

            var active = new ActiveTunnel
            {
                Client = client,
                Port = port,
                Config = config
            };

            _tunnels[config.Id] = active;
            config.IsActive = true;
            TunnelStatusChanged?.Invoke(config, true);
        });
    }

    public void StopTunnel(string tunnelId)
    {
        if (_tunnels.TryRemove(tunnelId, out var active))
        {
            try
            {
                if (active.Port.IsStarted)
                {
                    active.Port.Stop();
                }
            }
            catch { }

            try
            {
                if (active.Client.IsConnected)
                {
                    active.Client.Disconnect();
                }
                active.Client.Dispose();
            }
            catch { }

            active.Config.IsActive = false;
            TunnelStatusChanged?.Invoke(active.Config, false);
        }
    }

    public void Dispose()
    {
        foreach (var id in _tunnels.Keys)
        {
            StopTunnel(id);
        }
    }
}

