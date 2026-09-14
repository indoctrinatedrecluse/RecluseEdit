using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Toolchains;

public class TailwindToolchainCheck : IToolchainCheck
{
    public string ToolName => "Tailwind CSS CLI";
    public string Command => "tailwindcss";
    public string? RequiredVersion => ">= 3.0.0";
    public string? InstallHelp => "Install Tailwind CSS CLI via 'npm install -D tailwindcss' or use '@tailwindcss/cli' for Tailwind v4.";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainRunner.ExecuteAsync("npx", "tailwindcss --help", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            var standalone = await ToolchainRunner.ExecuteAsync("tailwindcss", "--help", cancellationToken);
            if (standalone.success && !string.IsNullOrEmpty(standalone.output))
            {
                success = true;
                output = standalone.output;
                path = standalone.path;
            }
        }

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Tailwind CSS CLI was not detected via npx or global path.",
                InstallHelp = InstallHelp
            };
        }

        var match = Regex.Match(output, @"tailwindcss\s+v?(\d+\.\d+(\.\d+)?)", RegexOptions.IgnoreCase);
        var version = match.Success ? match.Groups[1].Value : "Detected";

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = "Tailwind CSS CLI is available for building stylesheets.",
            Path = path,
            InstallHelp = InstallHelp
        };
    }
}

