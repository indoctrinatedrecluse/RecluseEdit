using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Angular.Toolchains;

/// <summary>
/// Verifies the presence of the Angular CLI (ng) on the system PATH.
/// </summary>
public class AngularCliToolchainCheck : IToolchainCheck
{
    public string ToolName => "Angular CLI (ng)";
    public string Command => "ng";
    public string? RequiredVersion => ">= 17.0.0";
    public string? InstallHelp => "Install globally via: 'npm install -g @angular/cli' or run via npx: 'npx @angular/cli'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("ng", "version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Angular CLI (ng) was not detected globally on system PATH. Workspace npx ng will be used when available in Angular projects.",
                InstallHelp = InstallHelp
            };
        }

        // Match Angular CLI version from output, e.g. "Angular CLI: 18.2.0"
        var versionMatch = Regex.Match(output, @"Angular CLI:\s*(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Angular CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    private static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
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
            await p.WaitForExitAsync(ct);

            return (p.ExitCode == 0, stdout.Trim(), cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}
