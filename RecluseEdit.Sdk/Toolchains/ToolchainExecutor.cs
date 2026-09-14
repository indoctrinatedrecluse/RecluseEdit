using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RecluseEdit.Sdk.Toolchains;

/// <summary>
/// Result of executing an external toolchain binary or compiler command.
/// </summary>
public record ToolExecutionResult(
    bool Success,
    string Output,
    string? ResolvedPath,
    string? DetectedVersion,
    int ExitCode);

/// <summary>
/// Safe, asynchronous execution helper for compilers, runtimes, and CLI developer tools.
/// Automatically resolves executable paths, wraps script commands on Windows, and extracts version strings.
/// </summary>
public static class ToolchainExecutor
{
    private static readonly Regex DefaultVersionRegex = new(
        @"(?:version\s+|v)?(\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9.\-_]+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Executes a tool command asynchronously with timeout protection, automatic path resolution, and version extraction.
    /// </summary>
    public static async Task<ToolExecutionResult> ExecuteAsync(
        string command,
        string args = "--version",
        string? workspaceRoot = null,
        string? versionRegex = null,
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        var resolvedPath = SdkPathResolver.ResolveExecutable(command, workspaceRoot);
        var targetFile = resolvedPath ?? command;

        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var ext = Path.GetExtension(targetFile).ToLowerInvariant();
            var isScript = isWindows && (ext is ".cmd" or ".bat" or ".ps1" || string.IsNullOrEmpty(ext));

            ProcessStartInfo psi;
            if (isWindows && isScript && !targetFile.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{targetFile}\" {args}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = targetFile,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            if (!string.IsNullOrEmpty(workspaceRoot) && Directory.Exists(workspaceRoot))
            {
                psi.WorkingDirectory = workspaceRoot;
            }

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                return new ToolExecutionResult(false, "Failed to start process", resolvedPath, null, -1);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            var exitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

            var completed = await Task.WhenAny(exitTask, timeoutTask);
            if (completed == timeoutTask)
            {
                try { process.Kill(true); } catch { }
                return new ToolExecutionResult(false, "Execution timed out", resolvedPath, null, -1);
            }

            var stdout = (await stdoutTask).Trim();
            var stderr = (await stderrTask).Trim();
            var output = !string.IsNullOrEmpty(stdout) ? stdout : stderr;
            var success = process.ExitCode == 0;

            // In case binary path was bare command and succeeded, update resolvedPath from process main module if possible
            if (string.IsNullOrEmpty(resolvedPath))
            {
                try
                {
                    resolvedPath = process.MainModule?.FileName;
                }
                catch { }
            }

            var detectedVersion = ExtractVersion(output, versionRegex);

            return new ToolExecutionResult(success, output, resolvedPath ?? targetFile, detectedVersion, process.ExitCode);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, ex.Message, resolvedPath, null, -1);
        }
    }

    /// <summary>
    /// Extracts a version string from standard output using an optional custom regex pattern or a smart default.
    /// </summary>
    public static string? ExtractVersion(string output, string? regexPattern = null)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        if (!string.IsNullOrEmpty(regexPattern))
        {
            var match = Regex.Match(output, regexPattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups.Count > 1 && match.Groups[1].Success
                    ? match.Groups[1].Value
                    : match.Value;
            }
        }

        var defaultMatch = DefaultVersionRegex.Match(output);
        if (defaultMatch.Success)
        {
            return defaultMatch.Groups.Count > 1 && defaultMatch.Groups[1].Success
                ? defaultMatch.Groups[1].Value
                : defaultMatch.Value;
        }

        // Fallback: First non-empty line trimmed
        var firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();

        return firstLine;
    }
}
