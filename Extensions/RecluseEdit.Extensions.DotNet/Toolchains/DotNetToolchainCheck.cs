using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.DotNet.Toolchains;

public class DotNetToolchainCheck : IToolchainCheck
{
    public string ToolName => ".NET SDK & CLI (dotnet)";
    public string Command => "dotnet";
    public string? RequiredVersion => ">= 8.0.0";
    public string? InstallHelp => "Install .NET SDK from https://dotnet.microsoft.com/download or via winget: 'winget install Microsoft.DotNet.SDK.10'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var psi = new ProcessStartInfo
            {
                FileName = isWindows ? "dotnet.exe" : "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null)
            {
                return new ToolchainReport
                {
                    ToolName = ToolName,
                    Command = Command,
                    Status = ToolchainStatus.Warning,
                    RequiredVersion = RequiredVersion,
                    Description = ".NET CLI could not be launched.",
                    InstallHelp = InstallHelp
                };
            }

            var stdout = await p.StandardOutput.ReadToEndAsync(cancellationToken);
            await p.WaitForExitAsync(cancellationToken);

            if (p.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                return new ToolchainReport
                {
                    ToolName = ToolName,
                    Command = Command,
                    Status = ToolchainStatus.Warning,
                    RequiredVersion = RequiredVersion,
                    Description = ".NET CLI returned non-zero exit code.",
                    InstallHelp = InstallHelp
                };
            }

            var version = stdout.Trim().Split('\n')[0].Trim();

            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Available,
                DetectedVersion = version,
                RequiredVersion = RequiredVersion,
                Description = ".NET SDK is available for compiling, running, and publishing applications.",
                InstallHelp = InstallHelp
            };
        }
        catch (Exception ex)
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = $"Failed to check .NET toolchain: {ex.Message}",
                InstallHelp = InstallHelp
            };
        }
    }
}
