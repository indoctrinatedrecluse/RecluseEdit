using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Service responsible for checking, downloading, and applying updates from GitHub Releases.
/// </summary>
public class UpdateService
{
    private const string DefaultRepoOwner = "indoctrinatedrecluse";
    private const string DefaultRepoName = "RecluseEdit";
    private static readonly TimeSpan BackgroundCheckThrottle = TimeSpan.FromHours(12);

    private readonly HttpClient _httpClient;
    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly string _cacheFilePath;
    private readonly string _currentVersion;

    public UpdateService(
        string repoOwner = DefaultRepoOwner,
        string repoName = DefaultRepoName,
        string? currentVersionOverride = null,
        HttpClient? httpClient = null)
    {
        _repoOwner = repoOwner;
        _repoName = repoName;
        _currentVersion = currentVersionOverride ??
                          Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ??
                          "6.0.0";

        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"RecluseEdit-Updater/{_currentVersion}");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "RecluseEdit");
        _cacheFilePath = Path.Combine(dir, "update_cache.json");
    }

    public string CurrentVersion => _currentVersion;

    /// <summary>
    /// Checks for updates against the GitHub Releases API.
    /// If isManual is false, checks the local cache to avoid exceeding GitHub API rate limits.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(bool isManual = false, CancellationToken ct = default)
    {
        try
        {
            // 1. Throttling check for background automated calls
            if (!isManual)
            {
                var cached = LoadCachedResult();
                if (cached != null && DateTime.UtcNow - cached.LastCheckedUtc < BackgroundCheckThrottle)
                {
                    return cached.Result;
                }
            }

            var apiUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            using var response = await _httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = $"GitHub API returned {(int)response.StatusCode} ({response.ReasonPhrase})";
                return UpdateCheckResult.Failed(_currentVersion, err);
            }

            var jsonString = await response.Content.ReadAsStringAsync(ct);
            var parsed = ParseGitHubReleaseJson(jsonString, _currentVersion);

            // Cache result
            SaveCachedResult(new CachedUpdateData
            {
                LastCheckedUtc = DateTime.UtcNow,
                Result = parsed
            });

            return parsed;
        }
        catch (OperationCanceledException)
        {
            return UpdateCheckResult.Failed(_currentVersion, "Update check was cancelled.");
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(_currentVersion, $"Failed to check for updates: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses the JSON response from the GitHub Releases API.
    /// </summary>
    public static UpdateCheckResult ParseGitHubReleaseJson(string json, string currentVersion)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var title = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? tagName : tagName;
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            var htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? "" : "";

            DateTime? publishedAt = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt))
            {
                publishedAt = dt;
            }

            var latestVersion = NormalizeVersionString(tagName);
            var hasUpdate = IsNewerVersion(currentVersion, latestVersion);

            // Parse assets
            var assetsList = new List<UpdateAssetInfo>();
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in assetsProp.EnumerateArray())
                {
                    var assetName = item.TryGetProperty("name", out var aName) ? aName.GetString() ?? "" : "";
                    var downloadUrl = item.TryGetProperty("browser_download_url", out var aUrl) ? aUrl.GetString() ?? "" : "";
                    var size = item.TryGetProperty("size", out var aSize) && aSize.TryGetInt64(out var s) ? s : 0;

                    var assetType = DetermineAssetType(assetName);
                    assetsList.Add(new UpdateAssetInfo
                    {
                        Name = assetName,
                        DownloadUrl = downloadUrl,
                        SizeBytes = size,
                        AssetType = assetType
                    });
                }
            }

            var matchingAsset = SelectMatchingAsset(assetsList);

            if (hasUpdate)
            {
                return UpdateCheckResult.SuccessUpdateAvailable(
                    currentVersion,
                    latestVersion,
                    title,
                    body,
                    publishedAt,
                    htmlUrl,
                    matchingAsset);
            }

            return UpdateCheckResult.SuccessUpToDate(currentVersion, latestVersion, htmlUrl);
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(currentVersion, $"Unable to parse release information: {ex.Message}");
        }
    }

    /// <summary>
    /// Selects the best matching asset for Windows distribution.
    /// Matches zip pattern: RecluseEdit.*windows.*\.zip or *.zip with RecluseEdit.
    /// </summary>
    public static UpdateAssetInfo? SelectMatchingAsset(IEnumerable<UpdateAssetInfo> assets)
    {
        var list = assets.ToList();
        if (list.Count == 0) return null;

        // 1. Prefer Windows portable zip archive
        var zipAsset = list.FirstOrDefault(a =>
            a.AssetType == UpdateAssetType.ZipArchive &&
            (Regex.IsMatch(a.Name, @"^RecluseEdit.*windows.*\.zip$", RegexOptions.IgnoreCase) ||
             a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase)));

        if (zipAsset != null) return zipAsset;

        // 2. Fallback to any RecluseEdit zip
        var anyZip = list.FirstOrDefault(a => a.AssetType == UpdateAssetType.ZipArchive);
        if (anyZip != null) return anyZip;

        // 3. Fallback to executable installer
        var exeAsset = list.FirstOrDefault(a => a.AssetType == UpdateAssetType.ExecutableInstaller);
        return exeAsset ?? list.FirstOrDefault();
    }

    /// <summary>
    /// Categorizes the asset type based on filename patterns.
    /// </summary>
    public static UpdateAssetType DetermineAssetType(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return UpdateAssetType.Unknown;

        if (filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateAssetType.ZipArchive;
        }

        if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateAssetType.ExecutableInstaller;
        }

        return UpdateAssetType.Unknown;
    }

    /// <summary>
    /// Normalizes a version string (e.g. "v5.5.0" -> "5.5.0").
    /// </summary>
    public static string NormalizeVersionString(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "0.0.0";
        var trimmed = version.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[1..];
        }
        return trimmed;
    }

    /// <summary>
    /// Compares two version strings (supports Semantic Versioning components).
    /// Returns true if latest > current.
    /// </summary>
    public static bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        var normCurrent = NormalizeVersionString(currentVersion);
        var normLatest = NormalizeVersionString(latestVersion);

        // Strip any prerelease tags for primary numeric comparison
        var cleanCurrent = normCurrent.Split('-')[0];
        var cleanLatest = normLatest.Split('-')[0];

        if (Version.TryParse(cleanCurrent, out var vCurrent) &&
            Version.TryParse(cleanLatest, out var vLatest))
        {
            if (vLatest > vCurrent) return true;
            if (vLatest < vCurrent) return false;

            // Same numeric version: if current has a prerelease (e.g. -beta) and latest does not, latest is newer
            if (normCurrent.Contains('-') && !normLatest.Contains('-'))
            {
                return true;
            }

            return false;
        }

        // Fallback to string comparison
        return string.Compare(normLatest, normCurrent, StringComparison.OrdinalIgnoreCase) > 0;
    }

    /// <summary>
    /// Downloads an update asset with progress reporting into a temporary directory.
    /// </summary>
    public async Task<string> DownloadUpdateAsync(
        UpdateAssetInfo asset,
        IProgress<UpdateProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RecluseEdit_Update");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var destinationFile = Path.Combine(tempDir, asset.Name);
        if (File.Exists(destinationFile))
        {
            File.Delete(destinationFile);
        }

        using var response = await _httpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.SizeBytes;
        long totalBytesRead = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            totalBytesRead += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percentage = (double)totalBytesRead / totalBytes * 100.0;
                progress.Report(new UpdateProgressReport
                {
                    BytesReceived = totalBytesRead,
                    TotalBytes = totalBytes,
                    Percentage = Math.Min(100.0, percentage),
                    StatusMessage = $"Downloading: {totalBytesRead / (1024 * 1024):0.0} MB of {totalBytes / (1024 * 1024):0.0} MB"
                });
            }
        }

        return destinationFile;
    }

    /// <summary>
    /// Applies the update and restarts the application.
    /// If zip: extracts files to staged folder and launches a detached PowerShell updater script that
    /// waits for the current process to exit, copies all files over, and relaunches the editor.
    /// If exe: launches the installer and exits.
    /// </summary>
    public void PrepareAndApplyUpdate(string downloadedFilePath, UpdateAssetInfo asset)
    {
        var currentProcess = Process.GetCurrentProcess();
        var currentPid = currentProcess.Id;
        var appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        var mainExe = currentProcess.MainModule?.FileName ?? Path.Combine(appDir, "RecluseEdit.exe");

        if (asset.AssetType == UpdateAssetType.ExecutableInstaller)
        {
            // Launch installer
            Process.Start(new ProcessStartInfo
            {
                FileName = downloadedFilePath,
                UseShellExecute = true
            });
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
            return;
        }

        // For zip archives: extract and force-replace files
        var updateRoot = Path.Combine(Path.GetTempPath(), "RecluseEdit_Update");
        var stagedDir = Path.Combine(updateRoot, "staged");

        if (Directory.Exists(stagedDir))
        {
            Directory.Delete(stagedDir, true);
        }
        Directory.CreateDirectory(stagedDir);

        // Extract zip
        ZipFile.ExtractToDirectory(downloadedFilePath, stagedDir, true);

        // Locate folder containing RecluseEdit.exe inside stagedDir (handles nested package root)
        var exeFiles = Directory.GetFiles(stagedDir, "RecluseEdit.exe", SearchOption.AllDirectories);
        var sourceDir = stagedDir;
        if (exeFiles.Length > 0)
        {
            sourceDir = Path.GetDirectoryName(exeFiles[0]) ?? stagedDir;
        }

        // Generate detached PowerShell updater script
        var scriptPath = Path.Combine(updateRoot, "apply_update.ps1");
        var scriptContent = GenerateUpdaterScriptContent(appDir, sourceDir, currentPid, mainExe);
        File.WriteAllText(scriptPath, scriptContent);

        // Launch detached updater process
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);

        // Terminate current process immediately to release file locks
        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    /// <summary>
    /// Generates the PowerShell script content used to wait for PID and replace files.
    /// </summary>
    public static string GenerateUpdaterScriptContent(string targetDir, string sourceDir, int parentPid, string exePath)
    {
        return $@"
# RecluseEdit Detached Auto-Updater Runner
$target = '{targetDir.Replace("'", "''")}'
$source = '{sourceDir.Replace("'", "''")}'
$pidToWait = {parentPid}
$exeToLaunch = '{exePath.Replace("'", "''")}'

# 1. Wait for parent process to exit
Write-Host 'Waiting for RecluseEdit (PID $pidToWait) to terminate...'
$timeoutSeconds = 30
$elapsed = 0
while ($elapsed -lt $timeoutSeconds) {{
    $proc = Get-Process -Id $pidToWait -ErrorAction SilentlyContinue
    if ($null -eq $proc) {{ break }}
    Start-Sleep -Milliseconds 250
    $elapsed += 0.25
}}

# Grace period to release OS file locks
Start-Sleep -Milliseconds 500

# 2. Force-replace all files in target directory
try {{
    Copy-Item -Path ""$source\*"" -Destination ""$target"" -Recurse -Force -ErrorAction Stop
}} catch {{
    # Fallback to robocopy
    robocopy ""$source"" ""$target"" /e /xo /r:3 /w:1 | Out-Null
}}

# 3. Relaunch updated application
if (Test-Path ""$exeToLaunch"") {{
    Start-Process -FilePath ""$exeToLaunch""
}}

# 4. Cleanup staging folder
Start-Sleep -Seconds 1
try {{
    Remove-Item -Path ""$source"" -Recurse -Force -ErrorAction SilentlyContinue
}} catch {{}}
";
    }

    private CachedUpdateData? LoadCachedResult()
    {
        try
        {
            if (!File.Exists(_cacheFilePath)) return null;
            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<CachedUpdateData>(json);
        }
        catch
        {
            return null;
        }
    }

    private void SaveCachedResult(CachedUpdateData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch
        {
            // Ignore cache write errors
        }
    }

    private class CachedUpdateData
    {
        public DateTime LastCheckedUtc { get; set; }
        public UpdateCheckResult Result { get; set; } = new();
    }
}

