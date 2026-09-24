using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Extensions.Remote.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace RecluseEdit.Extensions.Remote.Services;

/// <summary>
/// Provides remote file browsing, upload, download, and two-way file synchronization over SFTP.
/// </summary>
public class SftpBrowserService : IDisposable
{
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _activeWatchers = new();
    private readonly string _cacheRoot;

    public event Action<string, string>? FileSynchronized; // remotePath, localPath

    public SftpBrowserService(string? customCacheRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(customCacheRoot))
        {
            _cacheRoot = customCacheRoot;
        }
        else
        {
            _cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RecluseEdit", "remote_cache");
        }
        Directory.CreateDirectory(_cacheRoot);
    }

    public static ConnectionInfo CreateConnectionInfo(SshSessionProfile profile)
    {
        var authMethods = new List<AuthenticationMethod>();

        if (profile.AuthType == SshAuthType.Password && !string.IsNullOrEmpty(profile.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(profile.Username, profile.Password));
        }
        else if (profile.AuthType == SshAuthType.PrivateKey && !string.IsNullOrEmpty(profile.PrivateKeyPath) && File.Exists(profile.PrivateKeyPath))
        {
            var keyFile = !string.IsNullOrEmpty(profile.Passphrase)
                ? new PrivateKeyFile(profile.PrivateKeyPath, profile.Passphrase)
                : new PrivateKeyFile(profile.PrivateKeyPath);
            authMethods.Add(new PrivateKeyAuthenticationMethod(profile.Username, keyFile));
        }
        else if (!string.IsNullOrEmpty(profile.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(profile.Username, profile.Password));
        }

        if (authMethods.Count == 0)
        {
            // Fallback to empty password if nothing specified
            authMethods.Add(new PasswordAuthenticationMethod(profile.Username, ""));
        }

        return new ConnectionInfo(profile.Host, profile.Port, profile.Username, authMethods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Connects to the remote server and lists the files and directories in the target path.
    /// </summary>
    public async Task<IReadOnlyList<RemoteFileItem>> ListDirectoryAsync(SshSessionProfile profile, string remotePath = ".")
    {
        return await Task.Run(() =>
        {
            var conn = CreateConnectionInfo(profile);
            using var client = new SftpClient(conn);
            client.Connect();

            var files = client.ListDirectory(remotePath);
            var results = new List<RemoteFileItem>();

            foreach (var f in files)
            {
                if (f.Name is "." or "..") continue;

                results.Add(new RemoteFileItem
                {
                    Name = f.Name,
                    FullPath = f.FullName,
                    IsDirectory = f.IsDirectory,
                    Length = f.Length,
                    LastWriteTime = f.LastWriteTimeUtc.ToLocalTime(),
                    Permissions = GetPermissionsString(f)
                });
            }

            client.Disconnect();

            return results
                .OrderByDescending(r => r.IsDirectory)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        });
    }

    /// <summary>
    /// Downloads a remote file to a local destination file path.
    /// </summary>
    public async Task DownloadFileAsync(SshSessionProfile profile, string remotePath, string localDestination)
    {
        await Task.Run(() =>
        {
            var dir = Path.GetDirectoryName(localDestination);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var conn = CreateConnectionInfo(profile);
            using var client = new SftpClient(conn);
            client.Connect();

            using var fileStream = File.Create(localDestination);
            client.DownloadFile(remotePath, fileStream);

            client.Disconnect();
        });
    }

    /// <summary>
    /// Uploads a local file to a remote destination file path.
    /// </summary>
    public async Task UploadFileAsync(SshSessionProfile profile, string localSource, string remoteDestination)
    {
        await Task.Run(() =>
        {
            if (!File.Exists(localSource)) throw new FileNotFoundException("Local source file does not exist", localSource);

            var conn = CreateConnectionInfo(profile);
            using var client = new SftpClient(conn);
            client.Connect();

            using var fileStream = File.OpenRead(localSource);
            client.UploadFile(fileStream, remoteDestination, canOverride: true);

            client.Disconnect();
        });
    }

    /// <summary>
    /// Deletes a file or directory on the remote server.
    /// </summary>
    public async Task DeleteAsync(SshSessionProfile profile, string remotePath, bool isDirectory)
    {
        await Task.Run(() =>
        {
            var conn = CreateConnectionInfo(profile);
            using var client = new SftpClient(conn);
            client.Connect();

            if (isDirectory)
            {
                client.DeleteDirectory(remotePath);
            }
            else
            {
                client.DeleteFile(remotePath);
            }

            client.Disconnect();
        });
    }

    /// <summary>
    /// Creates a directory on the remote server.
    /// </summary>
    public async Task CreateDirectoryAsync(SshSessionProfile profile, string remotePath)
    {
        await Task.Run(() =>
        {
            var conn = CreateConnectionInfo(profile);
            using var client = new SftpClient(conn);
            client.Connect();

            client.CreateDirectory(remotePath);

            client.Disconnect();
        });
    }

    /// <summary>
    /// Downloads a remote file to the local cache and sets up a background two-way file watcher.
    /// When the file is edited and saved locally, it is automatically re-uploaded to the remote host.
    /// Returns the local cached file path.
    /// </summary>
    public async Task<string> FetchForEditingAsync(SshSessionProfile profile, string remotePath)
    {
        var sanitizedRemote = remotePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var localTarget = Path.Combine(_cacheRoot, profile.Id, sanitizedRemote);

        await DownloadFileAsync(profile, remotePath, localTarget);

        SetupAutoSyncWatcher(profile, remotePath, localTarget);

        return localTarget;
    }

    private void SetupAutoSyncWatcher(SshSessionProfile profile, string remotePath, string localPath)
    {
        if (_activeWatchers.ContainsKey(localPath)) return;

        var directory = Path.GetDirectoryName(localPath);
        var filename = Path.GetFileName(localPath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(filename)) return;

        var watcher = new FileSystemWatcher(directory, filename)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        var lastSyncTime = DateTime.MinValue;

        watcher.Changed += async (s, e) =>
        {
            // Debounce rapid save triggers
            if ((DateTime.UtcNow - lastSyncTime).TotalMilliseconds < 800) return;
            lastSyncTime = DateTime.UtcNow;

            await Task.Delay(200); // Allow file flush
            try
            {
                await UploadFileAsync(profile, localPath, remotePath);
                FileSynchronized?.Invoke(remotePath, localPath);
            }
            catch
            {
                // Upload retry on save
            }
        };

        _activeWatchers[localPath] = watcher;
    }

    private static string GetPermissionsString(ISftpFile file)
    {
        try
        {
            var isDir = file.IsDirectory ? "d" : "-";
            var r1 = file.OwnerCanRead ? "r" : "-";
            var w1 = file.OwnerCanWrite ? "w" : "-";
            var x1 = file.OwnerCanExecute ? "x" : "-";
            var r2 = file.GroupCanRead ? "r" : "-";
            var w2 = file.GroupCanWrite ? "w" : "-";
            var x2 = file.GroupCanExecute ? "x" : "-";
            var r3 = file.OthersCanRead ? "r" : "-";
            var w3 = file.OthersCanWrite ? "w" : "-";
            var x3 = file.OthersCanExecute ? "x" : "-";
            return $"{isDir}{r1}{w1}{x1}{r2}{w2}{x2}{r3}{w3}{x3}";
        }
        catch
        {
            return file.IsDirectory ? "drwxr-xr-x" : "-rw-r--r--";
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _activeWatchers.Values)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch { }
        }
        _activeWatchers.Clear();
    }
}
