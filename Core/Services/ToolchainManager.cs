using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Orchestrates and monitors developer toolchains (compilers, runtimes, CLI tools) registered by extensions.
/// </summary>
public class ToolchainManager
{
    private readonly List<IToolchainCheck> _checks = [];
    private readonly List<ToolchainReport> _reports = [];

    public IReadOnlyList<ToolchainReport> Reports => _reports.AsReadOnly();
    public IReadOnlyList<IToolchainCheck> RegisteredChecks => _checks.AsReadOnly();
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
        var tasks = _checks.Select(c => RunCheckAsync(c, cancellationToken)).ToList();
        await Task.WhenAll(tasks);
        ToolchainStatusChanged?.Invoke();
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
    /// </summary>
    public static async Task<(bool success, string output, string? path)> ExecuteToolAsync(
        string command,
        string args = "--version",
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var completed = await Task.WhenAny(process.WaitForExitAsync(cancellationToken), Task.Delay(timeoutMs, cancellationToken));
            if (completed != process.WaitForExitAsync(cancellationToken))
            {
                try { process.Kill(); } catch { }
                return (false, "Execution timed out", null);
            }

            var output = (await stdoutTask).Trim();
            if (string.IsNullOrEmpty(output))
            {
                output = (await stderrTask).Trim();
            }

            string? resolvedPath = null;
            try
            {
                resolvedPath = process.MainModule?.FileName;
            }
            catch { }

            return (process.ExitCode == 0, output, resolvedPath);
        }
        catch
        {
            // Executable not found on PATH or access denied
            return (false, "Not found on PATH", null);
        }
    }
}

