using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Ruby.Toolchains;

/// <summary>
/// Verifies the presence and version of the Ruby interpreter executable on the system PATH.
/// </summary>
public class RubyToolchainCheck : IToolchainCheck
{
    public string ToolName => "Ruby";
    public string Command => "ruby";
    public string? RequiredVersion => ">= 3.0.0";
    public string? InstallHelp => "Download Ruby from https://rubyinstaller.org or install via winget: 'winget install RubyInstallerTeam.RubyWithDevKit'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("ruby", "-v", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Ruby was not detected on system PATH. Install Ruby with DevKit to execute Ruby scripts and run Rails servers.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "ruby 3.3.0 (2023-12-25 revision 5124f9ac75) [x64-mingw-ucrt]"
        var versionMatch = Regex.Match(output, @"ruby\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Ruby runtime detected ({version})",
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
/// Verifies the presence and version of Bundler (Ruby gem dependency manager) on the system PATH.
/// </summary>
public class BundlerToolchainCheck : IToolchainCheck
{
    public string ToolName => "Bundler";
    public string Command => "bundle";
    public string? RequiredVersion => ">= 2.4.0";
    public string? InstallHelp => "Bundler is included with modern Ruby. You can also install it via gem: 'gem install bundler'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await RubyToolchainCheck.ExecuteCommandAsync("bundle", "-v", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Bundler was not detected on system PATH. Required for managing Gemfile dependencies in Rails projects.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Bundler version 2.5.6"
        var versionMatch = Regex.Match(output, @"Bundler\s+version\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Bundler detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

/// <summary>
/// Verifies the presence and version of the Ruby on Rails CLI on the system PATH.
/// </summary>
public class RailsToolchainCheck : IToolchainCheck
{
    public string ToolName => "Ruby on Rails";
    public string Command => "rails";
    public string? RequiredVersion => ">= 7.0.0";
    public string? InstallHelp => "Install Ruby on Rails via gem: 'gem install rails'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await RubyToolchainCheck.ExecuteCommandAsync("rails", "-v", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Ruby on Rails CLI was not detected on system PATH. Install Rails globally or invoke via 'bundle exec rails'.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Rails 7.1.3.2" or "Rails 8.0.0"
        var versionMatch = Regex.Match(output, @"Rails\s+(\d+\.\d+\.\d+)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output;

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Ruby on Rails detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}
