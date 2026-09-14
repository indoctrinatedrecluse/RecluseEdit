using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Orchestrates and monitors developer toolchains (compilers, runtimes, CLI tools) registered by extensions,
/// and automatically detects system development kits and compiler suites.
/// </summary>
public class ToolchainManager
{
    private readonly List<IToolchainCheck> _checks = [];
    private readonly List<ToolchainReport> _reports = [];
    private readonly List<DetectedSdk> _detectedSdks = [];

    public IReadOnlyList<ToolchainReport> Reports => _reports.AsReadOnly();
    public IReadOnlyList<IToolchainCheck> RegisteredChecks => _checks.AsReadOnly();
    public IReadOnlyList<DetectedSdk> DetectedSdks => _detectedSdks.AsReadOnly();
    public bool HasIssues => _reports.Any(r => r.Status is ToolchainStatus.Missing or ToolchainStatus.Warning);

    public event Action? ToolchainStatusChanged;

    public void RegisterCheck(IToolchainCheck check)
    {
        if (!_checks.Any(c => c.ToolName == check.ToolName))
        {
            _checks.Add(check);
            _ = RunCheckAsync(check);
        }
    }

    public async Task RunAllChecksAsync(CancellationToken cancellationToken = default)
    {
        var checkTasks = _checks.Select(c => (Task)RunCheckAsync(c, cancellationToken));
        var sdkTask = (Task)ScanSdksAsync(null, cancellationToken);

        await Task.WhenAll(checkTasks.Append(sdkTask));
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
            ToolchainStatusChanged?.Invoke();
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

        ToolchainStatusChanged?.Invoke();
        return report;
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

