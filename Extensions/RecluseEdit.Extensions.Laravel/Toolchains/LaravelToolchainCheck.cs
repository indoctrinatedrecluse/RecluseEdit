using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Laravel.Toolchains;

/// <summary>
/// Verifies the presence and version of the official Laravel installer CLI on the system PATH.
/// </summary>
public class LaravelCliToolchainCheck : IToolchainCheck
{
    public string ToolName => "Laravel Installer";
    public string Command => "laravel";
    public string? RequiredVersion => ">= 5.0.0";
    public string? InstallHelp => "Install the official Laravel installer via Composer: 'composer global require laravel/installer'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("laravel", "-V", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Laravel installer CLI was not detected on system PATH. Install it to scaffold new Laravel projects via 'laravel new'.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Laravel Installer 5.8.0"
        var versionMatch = Regex.Match(output, @"(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Laravel Installer detected ({version})",
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

/// <summary>
/// Verifies the presence and version of the Laravel Artisan CLI.
/// </summary>
public class ArtisanToolchainCheck : IToolchainCheck
{
    public string ToolName => "Artisan CLI";
    public string Command => "php artisan";
    public string? RequiredVersion => ">= 10.0.0";
    public string? InstallHelp => "Artisan is the command-line interface included with Laravel. Run 'php artisan' from inside a Laravel project directory.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await LaravelCliToolchainCheck.ExecuteCommandAsync("php", "artisan --version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output) || !output.Contains("Laravel Framework", StringComparison.OrdinalIgnoreCase))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Artisan CLI was not detected in current directory. Run commands inside an active Laravel project root.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Laravel Framework 11.0.8" or "Laravel Framework 10.48.4"
        var versionMatch = Regex.Match(output, @"Laravel\s+Framework\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Laravel Artisan detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}
