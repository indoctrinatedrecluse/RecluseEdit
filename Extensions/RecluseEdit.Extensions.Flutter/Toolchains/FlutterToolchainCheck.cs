using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Flutter.Toolchains;

/// <summary>
/// Verifies the presence and version of the Flutter SDK CLI on the system PATH.
/// </summary>
public class FlutterToolchainCheck : IToolchainCheck
{
    public string ToolName => "Flutter SDK";
    public string Command => "flutter";
    public string? RequiredVersion => ">= 3.20.0";
    public string? InstallHelp => "Download Flutter SDK from https://docs.flutter.dev/get-started/install or install via: 'winget install Google.Flutter'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("flutter", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Missing,
                RequiredVersion = RequiredVersion,
                Description = "Flutter SDK was not detected on system PATH. Install Flutter to enable cross-platform app compilation.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Flutter 3.47.2 • channel master • ..." or "Flutter 3.24.0 • channel stable • ..."
        var versionMatch = Regex.Match(output, @"Flutter\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Flutter SDK detected ({version})",
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

/// <summary>
/// Verifies the presence and version of the Dart SDK on the system PATH.
/// </summary>
public class DartToolchainCheck : IToolchainCheck
{
    public string ToolName => "Dart SDK";
    public string Command => "dart";
    public string? RequiredVersion => ">= 3.0.0";
    public string? InstallHelp => "Dart is bundled with Flutter. Alternatively, install standalone from https://dart.dev/get-dart";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("dart", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Missing,
                RequiredVersion = RequiredVersion,
                Description = "Dart SDK was not detected on system PATH. Required for compiling Dart applications.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Dart SDK version: 3.13.2 (stable) ..."
        var versionMatch = Regex.Match(output, @"Dart SDK version:\s*(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Dart SDK detected ({version})",
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
