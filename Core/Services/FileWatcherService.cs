using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Monitors opened files on disk for external modifications or deletions,
/// triggering seamless auto-reload for clean documents or conflict notifications for dirty documents.
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly Dictionary<string, FileSystemWatcher> _directoryWatchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event Action<DocumentModel>? FileReloaded;
    public event Action<DocumentModel, string>? FileConflictDetected;
    public event Action<DocumentModel>? FileDeletedOnDisk;

    private readonly Func<IEnumerable<DocumentModel>> _getOpenDocuments;

    public FileWatcherService(Func<IEnumerable<DocumentModel>> getOpenDocuments)
    {
        _getOpenDocuments = getOpenDocuments;
    }

    /// <summary>
    /// Synchronizes the set of active directory watchers with the currently opened documents.
    /// </summary>
    public void SyncWatchedFiles()
    {
        lock (_lock)
        {
            var openDocs = _getOpenDocuments().Where(d => !string.IsNullOrEmpty(d.FilePath)).ToList();
            var requiredDirs = openDocs
                .Select(d => Path.GetDirectoryName(Path.GetFullPath(d.FilePath!)))
                .Where(dir => !string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Remove unused watchers
            var toRemove = _directoryWatchers.Keys.Where(k => !requiredDirs.Contains(k)).ToList();
            foreach (var dir in toRemove)
            {
                if (_directoryWatchers.Remove(dir, out var watcher))
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
            }

            // Add new watchers
            foreach (var dir in requiredDirs)
            {
                if (dir != null && !_directoryWatchers.ContainsKey(dir))
                {
                    try
                    {
                        var watcher = new FileSystemWatcher(dir)
                        {
                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                            Filter = "*.*",
                            EnableRaisingEvents = true
                        };

                        watcher.Changed += OnFileSystemEvent;
                        watcher.Deleted += OnFileSystemEvent;
                        watcher.Renamed += OnFileSystemRenamed;

                        _directoryWatchers[dir] = watcher;
                    }
                    catch
                    {
                        // Safely ignore directories that cannot be watched (e.g. permission/network drive)
                    }
                }
            }
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        DebounceFileEvent(e.FullPath, e.ChangeType);
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        DebounceFileEvent(e.OldFullPath, WatcherChangeTypes.Deleted);
        DebounceFileEvent(e.FullPath, WatcherChangeTypes.Changed);
    }

    private void DebounceFileEvent(string fullPath, WatcherChangeTypes changeType)
    {
        if (string.IsNullOrEmpty(fullPath)) return;

        // Cancel pending debounce for this path
        if (_debounceTokens.TryRemove(fullPath, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _debounceTokens[fullPath] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, cts.Token);
                ProcessFileChange(fullPath, changeType);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _debounceTokens.TryRemove(fullPath, out _);
                cts.Dispose();
            }
        });
    }

    public void ProcessFileChange(string fullPath, WatcherChangeTypes changeType)
    {
        var doc = _getOpenDocuments().FirstOrDefault(d =>
            !string.IsNullOrEmpty(d.FilePath) &&
            string.Equals(Path.GetFullPath(d.FilePath), Path.GetFullPath(fullPath), StringComparison.OrdinalIgnoreCase));

        if (doc == null) return;

        if (!File.Exists(fullPath) || changeType == WatcherChangeTypes.Deleted)
        {
            FileDeletedOnDisk?.Invoke(doc);
            return;
        }

        // Read updated file from disk with safe retry
        string? newContent = ReadFileWithRetry(fullPath);
        if (newContent == null) return;

        // Check if content actually changed
        if (string.Equals(doc.Document.Text, newContent, StringComparison.Ordinal))
        {
            return;
        }

        if (!doc.IsDirty)
        {
            // Auto-reload clean document
            doc.Document.Text = newContent;
            doc.IsDirty = false;
            FileReloaded?.Invoke(doc);
        }
        else
        {
            // Raise conflict for dirty document
            FileConflictDetected?.Invoke(doc, newContent);
        }
    }

    private static string? ReadFileWithRetry(string path, int maxAttempts = 3)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (IOException)
            {
                if (i == maxAttempts - 1) return null;
                Thread.Sleep(100);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var watcher in _directoryWatchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _directoryWatchers.Clear();

            foreach (var cts in _debounceTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _debounceTokens.Clear();
        }
    }
}

