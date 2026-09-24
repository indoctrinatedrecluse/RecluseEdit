using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RecluseEdit.Extensions.Remote.Models;

namespace RecluseEdit.Extensions.Remote.Services;

/// <summary>
/// Persists SSH connection profiles and tunnel definitions to local JSON storage.
/// </summary>
public class SshProfileStore
{
    private readonly string _storagePath;
    private readonly object _lock = new();

    public class StorageData
    {
        public List<SshSessionProfile> Profiles { get; set; } = [];
        public List<SshTunnelConfig> Tunnels { get; set; } = [];
    }

    private StorageData _data = new();

    public SshProfileStore(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            _storagePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RecluseEdit");
            Directory.CreateDirectory(dir);
            _storagePath = Path.Combine(dir, "remote_sessions.json");
        }

        Load();
    }

    public IReadOnlyList<SshSessionProfile> GetProfiles()
    {
        lock (_lock)
        {
            return _data.Profiles.ToList().AsReadOnly();
        }
    }

    public void SaveProfile(SshSessionProfile profile)
    {
        lock (_lock)
        {
            var existingIndex = _data.Profiles.FindIndex(p => p.Id == profile.Id);
            if (existingIndex >= 0)
            {
                _data.Profiles[existingIndex] = profile;
            }
            else
            {
                _data.Profiles.Add(profile);
            }
            Persist();
        }
    }

    public bool DeleteProfile(string profileId)
    {
        lock (_lock)
        {
            var removed = _data.Profiles.RemoveAll(p => p.Id == profileId) > 0;
            if (removed)
            {
                Persist();
            }
            return removed;
        }
    }

    public IReadOnlyList<SshTunnelConfig> GetTunnels()
    {
        lock (_lock)
        {
            return _data.Tunnels.ToList().AsReadOnly();
        }
    }

    public void SaveTunnel(SshTunnelConfig tunnel)
    {
        lock (_lock)
        {
            var existingIndex = _data.Tunnels.FindIndex(t => t.Id == tunnel.Id);
            if (existingIndex >= 0)
            {
                _data.Tunnels[existingIndex] = tunnel;
            }
            else
            {
                _data.Tunnels.Add(tunnel);
            }
            Persist();
        }
    }

    public bool DeleteTunnel(string tunnelId)
    {
        lock (_lock)
        {
            var removed = _data.Tunnels.RemoveAll(t => t.Id == tunnelId) > 0;
            if (removed)
            {
                Persist();
            }
            return removed;
        }
    }

    private void Load()
    {
        lock (_lock)
        {
            if (File.Exists(_storagePath))
            {
                try
                {
                    var json = File.ReadAllText(_storagePath);
                    var loaded = JsonSerializer.Deserialize<StorageData>(json);
                    if (loaded != null)
                    {
                        _data = loaded;
                        return;
                    }
                }
                catch
                {
                    // Fallback to empty defaults
                }
            }

            // Populate sample profiles if empty
            _data = new StorageData
            {
                Profiles =
                [
                    new SshSessionProfile
                    {
                        Name = "Localhost Development",
                        Group = "Local",
                        Host = "127.0.0.1",
                        Port = 22,
                        Username = Environment.UserName,
                        AuthType = SshAuthType.Password,
                        Notes = "Local SSH service running on workstation."
                    },
                    new SshSessionProfile
                    {
                        Name = "Ubuntu Cloud Server",
                        Group = "Production",
                        Host = "192.168.1.100",
                        Port = 22,
                        Username = "ubuntu",
                        AuthType = SshAuthType.PrivateKey,
                        Notes = "Cloud deployment VM."
                    }
                ],
                Tunnels =
                [
                    new SshTunnelConfig
                    {
                        Name = "PostgreSQL Forwarding",
                        SshHost = "192.168.1.100",
                        SshPort = 22,
                        SshUser = "ubuntu",
                        LocalPort = 5433,
                        RemoteHost = "127.0.0.1",
                        RemotePort = 5432
                    }
                ]
            };

            Persist();
        }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
            // Ignore disk write errors
        }
    }
}

