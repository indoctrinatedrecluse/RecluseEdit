using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Orchestrates and monitors developer toolchains (compilers, runtimes, CLI tools) registered by extensions,
/// and automatically detects system development kits and compiler suites.
/// Supports high-speed persistent caching and lazy background execution for instant application startup.
/// </summary>
public class ToolchainManager
{
    private readonly List<IToolchainCheck> _checks = [];
    private readonly List<ToolchainReport> _reports = [];
    private readonly List<DetectedSdk> _detectedSdks = [];
    private readonly ToolchainCacheService _cacheService;
    private int _statusChangeScheduled = 0;

    public IReadOnlyList<ToolchainReport> Reports => _reports.AsReadOnly();
    public IReadOnlyList<IToolchainCheck> RegisteredChecks => _checks.AsReadOnly();
    public IReadOnlyList<DetectedSdk> DetectedSdks => _detectedSdks.AsReadOnly();
    public bool HasIssues => _reports.Any(r => r.Status is ToolchainStatus.Missing or ToolchainStatus.Warning);
    public ToolchainCacheService CacheService => _cacheService;

    public event Action? ToolchainStatusChanged;

    public ToolchainManager(ToolchainCacheService? cacheService = null)
    {
        _cacheService = cacheService ?? new ToolchainCacheService();
    }

    /// <summary>
    /// Registers a toolchain check. By default, check execution is deferred to background/on-demand
    /// so that application startup remains instant.
    /// </summary>
    public void RegisterCheck(IToolchainCheck check, bool runEagerly = false)
    {
        lock (_checks)
        {
            if (!_checks.Any(c => c.ToolName == check.ToolName))
            {
                _checks.Add(check);
            }
        }

        if (runEagerly)
        {
            _ = RunCheckAsync(check);
        }
    }

    /// <summary>
    /// Initializes toolchains from the persistent disk cache (<2ms), and optionally triggers a background refresh
    /// if the cache is missing, empty, or older than 24 hours.
    /// </summary>
    public async Task InitializeWithCacheAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var cached = _cacheService.LoadCache();
        if (cached != null)
        {
            lock (_reports)
            {
                _reports.Clear();
                _reports.AddRange(cached.Reports);
            }
            lock (_detectedSdks)
            {
                _detectedSdks.Clear();
                _detectedSdks.AddRange(cached.DetectedSdks);
            }
            ToolchainStatusChanged?.Invoke();
        }

        if (forceRefresh || !_cacheService.IsCacheValid(cached))
        {
            await RunAllChecksAsync(cancellationToken);
        }
    }

    public async Task RunAllChecksAsync(CancellationToken cancellationToken = default)
    {
        List<IToolchainCheck> checksCopy;
        lock (_checks)
        {
            checksCopy = _checks.ToList();
        }

        var checkTasks = checksCopy.Select(c => (Task)RunCheckAsync(c, cancellationToken));
        var sdkTask = (Task)ScanSdksAsync(null, cancellationToken);

        await Task.WhenAll(checkTasks.Append(sdkTask));

        // Persist reports and SDKs to disk cache
        await _cacheService.SaveCacheAsync(Reports, DetectedSdks, cancellationToken);

        ToolchainStatusChanged?.Invoke();
    }

    public async Task<IReadOnlyList<DetectedSdk>> ScanSdksAsync(string? workspaceRoot = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var sdks = await SdkAutoDetector.DetectAllSdksAsync(workspaceRoot, cancellationToken);
            lock (_detectedSdks)
            {
                _detectedSdks.Clear();
                _detectedSdks.AddRange(sdks);
            }
            DebounceStatusChanged();
            return _detectedSdks.AsReadOnly();
        }
        catch
        {
            return _detectedSdks.AsReadOnly();
        }
    }

    public async Task<ToolchainReport> RunCheckAsync(IToolchainCheck check, CancellationToken cancellationToken = default)
    {
        ToolchainReport report;
        try
        {
            report = await check.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            report = new ToolchainReport
            {
                ToolName = check.ToolName,
                Command = check.Command,
                Status = ToolchainStatus.Missing,
                Description = $"Failed to execute check: {ex.Message}"
            };
        }

        lock (_reports)
        {
            _reports.RemoveAll(r => r.ToolName == report.ToolName);
            _reports.Add(report);
        }

        DebounceStatusChanged();
        return report;
    }

    private void DebounceStatusChanged()
    {
        if (Interlocked.Exchange(ref _statusChangeScheduled, 1) == 0)
        {
            Task.Delay(100).ContinueWith(_ =>
            {
                Interlocked.Exchange(ref _statusChangeScheduled, 0);
                ToolchainStatusChanged?.Invoke();
            }, TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Helper method to execute a command-line tool with arguments and retrieve its stdout/exit code safely.
    /// Delegates to the centralized Sdk.Toolchains.ToolchainExecutor.
    /// </summary>
    public static async Task<(bool success, string output, string? path)> ExecuteToolAsync(
        string command,
        string args = "--version",
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        var result = await ToolchainExecutor.ExecuteAsync(command, args, null, null, timeoutMs, cancellationToken);
        return (result.Success, result.Output, result.ResolvedPath);
    }
}
