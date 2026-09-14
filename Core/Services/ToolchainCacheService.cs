using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Data payload persisted to disk for toolchain and SDK diagnostics caching.
/// </summary>
public class ToolchainCacheData
{
    public string Version { get; set; } = string.Empty;
    public DateTime LastScannedUtc { get; set; }
    public List<ToolchainReport> Reports { get; set; } = [];
    public List<DetectedSdk> DetectedSdks { get; set; } = [];
}

/// <summary>
/// High-speed persistent caching service for toolchain diagnostics and detected SDKs.
/// Allows RecluseEdit to display previous toolchain states instantaneously on startup (<2ms)
/// without executing external processes or performing heavy filesystem traversal.
/// </summary>
public class ToolchainCacheService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _cacheFilePath;
    private readonly string _currentVersion;

    public ToolchainCacheService(string? customCachePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customCachePath))
        {
            _cacheFilePath = customCachePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RecluseEdit");
            _cacheFilePath = Path.Combine(dir, "toolchain_cache.json");
        }

        _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "5.5.0";
    }

    /// <summary>
    /// Synchronously loads the cached toolchain state from disk. Returns null if missing or invalid.
    /// </summary>
    public ToolchainCacheData? LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var json = File.ReadAllText(_cacheFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<ToolchainCacheData>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks whether the cache is valid (matches current application version and is within TTL).
    /// </summary>
    public bool IsCacheValid(ToolchainCacheData? cache, TimeSpan? maxAge = null)
    {
        if (cache == null) return false;
        if (!string.Equals(cache.Version, _currentVersion, StringComparison.OrdinalIgnoreCase))
            return false;

        var ttl = maxAge ?? TimeSpan.FromHours(24);
        return (DateTime.UtcNow - cache.LastScannedUtc) <= ttl;
    }

    /// <summary>
    /// Asynchronously persists toolchain reports and detected SDKs to disk.
    /// </summary>
    public async Task SaveCacheAsync(
        IReadOnlyList<ToolchainReport> reports,
        IReadOnlyList<DetectedSdk> detectedSdks,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new ToolchainCacheData
            {
                Version = _currentVersion,
                LastScannedUtc = DateTime.UtcNow,
                Reports = reports.ToList(),
                DetectedSdks = detectedSdks.ToList()
            };

            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(data, JsonOpts);
            await File.WriteAllTextAsync(_cacheFilePath, json, cancellationToken);
        }
        catch
        {
            // Ignore cache write failures silently
        }
    }

    /// <summary>
    /// Clears the cached toolchain data from disk.
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }
        }
        catch { }
    }
}

