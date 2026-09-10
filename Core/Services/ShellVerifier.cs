using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Result of verifying a shell executable path.
/// </summary>
public class ShellVerificationResult
{
    public bool IsValid { get; }
    public string Message { get; }
    public string? ProbeOutput { get; }

    public ShellVerificationResult(bool isValid, string message, string? probeOutput = null)
    {
        IsValid = isValid;
        Message = message;
        ProbeOutput = probeOutput;
    }

    public static ShellVerificationResult Success(string message, string? probeOutput = null) => new(true, message, probeOutput);
    public static ShellVerificationResult Failure(string message) => new(false, message);
}

/// <summary>
/// Validates and verifies candidate shell executables before accepting them into the editor.
/// </summary>
public static class ShellVerifier
{
    /// <summary>
    /// Synchronously validates and probes a candidate shell executable.
    /// </summary>
    public static ShellVerificationResult Verify(ShellInfo shell, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return ShellVerificationResult.Failure("Executable path cannot be empty.");
        }

        var expanded = Environment.ExpandEnvironmentVariables(executablePath.Trim('\"', ' '));

        if (!File.Exists(expanded))
        {
            return ShellVerificationResult.Failure($"File does not exist at specified path:\n{expanded}");
        }

        var ext = Path.GetExtension(expanded).ToLowerInvariant();
        if (OperatingSystem.IsWindows() && ext is not (".exe" or ".cmd" or ".bat"))
        {
            return ShellVerificationResult.Failure($"Selected file must be an executable binary (.exe, .cmd, .bat), but got '{ext}'.");
        }

        var fileName = Path.GetFileName(expanded).ToLowerInvariant();
        if (shell.ExpectedBinaryNames is { Length: > 0 } &&
            !shell.ExpectedBinaryNames.Any(e => fileName.Equals(e, StringComparison.OrdinalIgnoreCase)))
        {
            var expectedList = string.Join(", ", shell.ExpectedBinaryNames);
            return ShellVerificationResult.Failure($"The selected file '{fileName}' does not match expected executable names for {shell.DisplayName} (expected: {expectedList}).");
        }

        // Determine probe arguments based on shell type
        var probeArgs = GetProbeArguments(shell.Id);

        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = expanded,
                    Arguments = probeArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (!proc.Start())
            {
                return ShellVerificationResult.Failure("Failed to launch executable process for verification.");
            }

            if (!proc.WaitForExit(2500))
            {
                try { proc.Kill(); } catch { }
                return ShellVerificationResult.Failure("Executable probe execution timed out (exceeded 2.5s). The program did not exit.");
            }

            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            if (proc.ExitCode == 0 || !string.IsNullOrWhiteSpace(stdout))
            {
                var summary = !string.IsNullOrWhiteSpace(stdout)
                    ? stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                    : "OK";

                return ShellVerificationResult.Success($"Verification succeeded! Detected: {summary}", summary);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                var errLine = stderr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                return ShellVerificationResult.Failure($"Executable returned exit code {proc.ExitCode}: {errLine}");
            }

            return ShellVerificationResult.Failure($"Executable probe returned exit code {proc.ExitCode}.");
        }
        catch (Exception ex)
        {
            return ShellVerificationResult.Failure($"Failed to execute binary: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously validates and probes a candidate shell executable.
    /// </summary>
    public static Task<ShellVerificationResult> VerifyAsync(ShellInfo shell, string executablePath)
    {
        return Task.Run(() => Verify(shell, executablePath));
    }

    private static string GetProbeArguments(string shellId)
    {
        var id = shellId.ToLowerInvariant();
        if (id.Contains("pwsh") || id.Contains("power"))
        {
            return "-NoProfile -NonInteractive -Command \"$PSVersionTable.PSVersion.ToString()\"";
        }
        if (id == "cmd")
        {
            return "/c echo RecluseEdit_OK";
        }
        if (id.Contains("bash") || id.Contains("zsh") || id.Contains("cygwin") || id.Contains("msys"))
        {
            return "--version";
        }
        if (id == "wsl")
        {
            return "--version";
        }

        return "--version";
    }
}

