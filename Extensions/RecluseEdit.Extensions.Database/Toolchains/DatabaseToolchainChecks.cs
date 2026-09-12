using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Database.Toolchains;

public class SqliteToolchainCheck : IToolchainCheck
{
    public string ToolName => "SQLite CLI";
    public string Command => "sqlite3";
    public string? RequiredVersion => ">= 3.0.0";
    public string? InstallHelp => "Download SQLite from https://www.sqlite.org/download.html or install via 'winget install SQLite.SQLite'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ExecuteCommandAsync("sqlite3", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "SQLite CLI not found on system PATH. Install for external sqlite3 command line management.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"^([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"SQLite CLI detected ({version})",
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

public class PostgresToolchainCheck : IToolchainCheck
{
    public string ToolName => "PostgreSQL (psql)";
    public string Command => "psql";
    public string? RequiredVersion => ">= 14.0";
    public string? InstallHelp => "Download PostgreSQL client tools from https://www.postgresql.org/download/";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await SqliteToolchainCheck.ExecuteCommandAsync("psql", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "psql CLI not found on system PATH. Install PostgreSQL tools to connect via command line.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"psql\s+\(PostgreSQL\)\s+([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"PostgreSQL psql CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class MySqlToolchainCheck : IToolchainCheck
{
    public string ToolName => "MySQL CLI";
    public string Command => "mysql";
    public string? RequiredVersion => ">= 8.0";
    public string? InstallHelp => "Download MySQL client tools from https://dev.mysql.com/downloads/";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await SqliteToolchainCheck.ExecuteCommandAsync("mysql", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "mysql CLI not found on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"Distrib\s+([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"MySQL CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}
