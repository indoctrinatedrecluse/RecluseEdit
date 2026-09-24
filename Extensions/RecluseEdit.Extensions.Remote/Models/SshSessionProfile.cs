using System;
using System.Text.Json.Serialization;

namespace RecluseEdit.Extensions.Remote.Models;

public enum SshAuthType
{
    Password,
    PrivateKey
}

/// <summary>
/// Represents a saved SSH remote connection session profile.
/// </summary>
public class SshSessionProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Session";
    public string Group { get; set; } = "Default";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "root";
    public SshAuthType AuthType { get; set; } = SshAuthType.Password;
    public string? Password { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? Passphrase { get; set; }
    public string? InitialDirectory { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastConnected { get; set; }

    [JsonIgnore]
    public string DisplayTitle => $"{Name} ({Username}@{Host}:{Port})";

    [JsonIgnore]
    public string QuickUri => $"{Username}@{Host}:{Port}";
}

