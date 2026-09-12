using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.RestClient.Toolchains;

public class CurlToolchainCheck : IToolchainCheck
{
    public string ToolName => "cURL CLI";
    public string Command => "curl";
    public string? RequiredVersion => ">= 7.0";
    public string? InstallHelp => "curl is pre-installed on modern Windows 10/11, macOS, and Linux. Ensure curl is available in your PATH.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("curl", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "curl CLI not found on system PATH. The built-in HTTP client engine will still function.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"curl\s+([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"cURL CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    internal static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var fileName = isWindows ? "cmd.exe" : cmd;
            var arguments = isWindows ? $"/c {cmd} {args}" : args;

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) return (false, "", null);

            var stdout = await p.StandardOutput.ReadToEndAsync(ct);
            var stderr = await p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var outText = !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : stderr.Trim();
            return (p.ExitCode == 0, outText, cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}

