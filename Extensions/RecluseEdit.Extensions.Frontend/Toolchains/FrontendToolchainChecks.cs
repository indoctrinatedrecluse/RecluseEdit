using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Extensions.Frontend.Toolchains;

internal static class ToolchainRunner
{
    public static async Task<(bool success, string output, string? path)> ExecuteAsync(string cmd, string args, CancellationToken ct)
    {
        var res = await ToolchainExecutor.ExecuteAsync(cmd, args, null, null, 3000, ct);
        return (res.Success, res.Output, res.ResolvedPath);
    }
}

public class ViteToolchainCheck : IToolchainCheck
{
    public string ToolName => "Vite CLI";
    public string Command => "vite";
    public string? RequiredVersion => ">= 5.0.0";
    public string? InstallHelp => "Install Vite globally via 'npm install -g vite' or run within project using 'npx vite'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("vite", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Vite CLI not found on system PATH. Install for fast frontend dev server.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"vite[/ ]([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Vite CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class NextToolchainCheck : IToolchainCheck
{
    public string ToolName => "Next.js CLI";
    public string Command => "next";
    public string? RequiredVersion => ">= 14.0.0";
    public string? InstallHelp => "Install Next.js via 'npx create-next-app@latest' or 'npm install next'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("next", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Next.js CLI not found on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"Next\.js\s+v?([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Next.js CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class AstroToolchainCheck : IToolchainCheck
{
    public string ToolName => "Astro CLI";
    public string Command => "astro";
    public string? RequiredVersion => ">= 4.0.0";
    public string? InstallHelp => "Install Astro via 'npm install -g astro' or run 'npx astro'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("astro", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Astro CLI not found on system PATH.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"astro\s+v?([0-9\.]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Astro CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class TurboToolchainCheck : IToolchainCheck
{
    public string ToolName => "Turborepo CLI";
    public string Command => "turbo";
    public string? RequiredVersion => ">= 1.10.0";
    public string? InstallHelp => "Install Turbo globally via 'npm install -g turbo'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("turbo", "--version", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Turborepo CLI not found on system PATH.",
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
            Description = $"Turborepo CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class PnpmToolchainCheck : IToolchainCheck
{
    public string ToolName => "pnpm Package Manager";
    public string Command => "pnpm";
    public string? RequiredVersion => ">= 8.0.0";
    public string? InstallHelp => "Install pnpm via 'npm install -g pnpm' or 'corepack enable'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("pnpm", "-v", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "pnpm not found on system PATH.",
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
            Description = $"pnpm detected ({output.Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

public class BunToolchainCheck : IToolchainCheck
{
    public string ToolName => "Bun Runtime & Bundler";
    public string Command => "bun";
    public string? RequiredVersion => ">= 1.0.0";
    public string? InstallHelp => "Install Bun via 'powershell -c \"irm bun.sh/install.ps1 | iex\"'.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("bun", "-v", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Bun runtime not found on system PATH.",
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
            Description = $"Bun detected ({output.Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

