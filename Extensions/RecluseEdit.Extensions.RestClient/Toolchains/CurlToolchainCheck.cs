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
        var res = await RecluseEdit.Sdk.Toolchains.ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}

