using System;
using System.Text.Json.Serialization;

namespace RecluseEdit.Extensions.Remote.Models;

/// <summary>
/// Configuration for a local SSH port forwarding tunnel (MobaXterm style).
/// Forwards traffic on LocalPort to RemoteHost:RemotePort through an SSH connection.
/// </summary>
public class SshTunnelConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Port Tunnel";
    public string SshHost { get; set; } = "127.0.0.1";
    public int SshPort { get; set; } = 22;
    public string SshUser { get; set; } = "root";
    public SshAuthType AuthType { get; set; } = SshAuthType.Password;
    public string? SshPassword { get; set; }
    public string? SshKeyPath { get; set; }
    public uint LocalPort { get; set; } = 8080;
    public string RemoteHost { get; set; } = "127.0.0.1";
    public uint RemotePort { get; set; } = 80;

    [JsonIgnore]
    public bool IsActive { get; set; }

    [JsonIgnore]
    public string Description => $"127.0.0.1:{LocalPort} ➔ {RemoteHost}:{RemotePort} (via {SshUser}@{SshHost}:{SshPort})";
}

