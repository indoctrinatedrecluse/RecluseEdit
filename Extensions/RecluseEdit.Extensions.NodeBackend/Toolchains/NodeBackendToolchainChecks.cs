using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.NodeBackend.Toolchains;

internal static class NodeBackendToolchainRunner
{
    public static async Task<(bool success, string output, string? path)> ExecuteAsync(string cmd, string args, CancellationToken ct)
    {
        var res = await RecluseEdit.Sdk.Toolchains.ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}

public class NestCliToolchainCheck : IToolchainCheck
{
    public string ToolName => "NestJS CLI";
    public string Command => "nest";
    public string? RequiredVersion => ">= 10.0.0";
    public string? InstallHelp => "Install NestJS CLI via 'npm install -g @nestjs/cli'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await NodeBackendToolchainRunner.ExecuteAsync("nest", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "NestJS CLI not found on system PATH.",
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
            Description = $"NestJS CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class Pm2ToolchainCheck : IToolchainCheck
{
    public string ToolName => "PM2 Process Manager";
    public string Command => "pm2";
    public string? RequiredVersion => ">= 5.0.0";
    public string? InstallHelp => "Install PM2 globally via 'npm install -g pm2'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await NodeBackendToolchainRunner.ExecuteAsync("pm2", "-v", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "PM2 process manager not found on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = output.Split('\n')[0].Trim(),
            RequiredVersion = RequiredVersion,
            Description = $"PM2 detected ({output.Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class FastifyCliToolchainCheck : IToolchainCheck
{
    public string ToolName => "Fastify CLI";
    public string Command => "fastify";
    public string? RequiredVersion => ">= 5.0.0";
    public string? InstallHelp => "Install Fastify CLI globally via 'npm install -g fastify-cli'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await NodeBackendToolchainRunner.ExecuteAsync("fastify", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Fastify CLI not found on system PATH.",
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
            Description = $"Fastify CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

