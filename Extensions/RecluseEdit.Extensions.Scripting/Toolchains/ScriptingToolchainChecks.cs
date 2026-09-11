using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Toolchains;

internal static class ToolchainProcessHelper
{
    public static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
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
/// Verifies the presence and version of the Rust compiler (rustc).
/// </summary>
public class RustToolchainCheck : IToolchainCheck
{
    public string ToolName => "Rust Compiler";
    public string Command => "rustc";
    public string? RequiredVersion => ">= 1.70.0";
    public string? InstallHelp => "Install Rust toolchain via rustup from https://rustup.rs/ or via winget: 'winget install Rustlang.Rustup'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("rustc", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Rust compiler (rustc) was not detected on system PATH. Install rustup to compile Rust applications.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "rustc 1.82.0 (f6e511eec 2024-10-15)"
        var match = Regex.Match(output, @"rustc\s+([0-9a-zA-Z\.\-]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Rust compiler detected ({output.Split('\n')[0].Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

/// <summary>
/// Verifies the presence and version of the Lua runtime.
/// </summary>
public class LuaToolchainCheck : IToolchainCheck
{
    public string ToolName => "Lua Runtime";
    public string Command => "lua";
    public string? RequiredVersion => ">= 5.1";
    public string? InstallHelp => "Download Lua from https://www.lua.org/ or via scoop ('scoop install lua') / winget ('winget install Lua.Lua').";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("lua", "-v", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            // Try luajit fallback
            var (jitSuccess, jitOutput, jitPath) = await ToolchainProcessHelper.ExecuteCommandAsync("luajit", "-v", cancellationToken);
            if (jitSuccess && !string.IsNullOrEmpty(jitOutput))
            {
                return new ToolchainReport
                {
                    ToolName = "LuaJIT Runtime",
                    Command = "luajit",
                    Status = ToolchainStatus.Available,
                    DetectedVersion = jitOutput.Split('\n')[0].Trim(),
                    RequiredVersion = RequiredVersion,
                    Description = $"LuaJIT runtime detected ({jitOutput.Split('\n')[0].Trim()})",
                    InstallHelp = InstallHelp,
                    Path = jitPath
                };
            }

            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Lua runtime was not detected on system PATH. Install Lua to execute .lua scripts.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Lua 5.4.6  Copyright (C) 1994-2023 Lua.org, PUC-Rio"
        var match = Regex.Match(output, @"Lua\s+([0-9a-zA-Z\.\-]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Lua runtime detected ({output.Split('\n')[0].Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

/// <summary>
/// Verifies the presence of PowerShell 7+ (pwsh) or Windows PowerShell (powershell).
/// </summary>
public class PowerShellToolchainCheck : IToolchainCheck
{
    public string ToolName => "PowerShell Engine";
    public string Command => "pwsh";
    public string? RequiredVersion => ">= 5.1";
    public string? InstallHelp => "Install modern PowerShell 7+ from https://github.com/PowerShell/PowerShell or via winget: 'winget install Microsoft.PowerShell'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Check pwsh first
        var (pwshSuccess, pwshOutput, pwshPath) = await ToolchainProcessHelper.ExecuteCommandAsync("pwsh", "--version", cancellationToken);
        if (pwshSuccess && !string.IsNullOrEmpty(pwshOutput))
        {
            var match = Regex.Match(pwshOutput, @"PowerShell\s+([0-9a-zA-Z\.\-]+)");
            var version = match.Success ? match.Groups[1].Value : pwshOutput.Trim();

            return new ToolchainReport
            {
                ToolName = "PowerShell 7+",
                Command = "pwsh",
                Status = ToolchainStatus.Available,
                DetectedVersion = version,
                RequiredVersion = RequiredVersion,
                Description = $"Modern PowerShell 7+ detected ({version})",
                InstallHelp = InstallHelp,
                Path = pwshPath
            };
        }

        // Fallback to built-in Windows PowerShell
        var (psSuccess, psOutput, psPath) = await ToolchainProcessHelper.ExecuteCommandAsync("powershell", "-NoProfile -Command \"$PSVersionTable.PSVersion.ToString()\"", cancellationToken);
        if (psSuccess && !string.IsNullOrEmpty(psOutput))
        {
            var version = psOutput.Trim();
            return new ToolchainReport
            {
                ToolName = "Windows PowerShell",
                Command = "powershell",
                Status = ToolchainStatus.Available,
                DetectedVersion = version,
                RequiredVersion = RequiredVersion,
                Description = $"Windows PowerShell detected ({version})",
                InstallHelp = InstallHelp,
                Path = psPath
            };
        }

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Warning,
            RequiredVersion = RequiredVersion,
            Description = "PowerShell executable was not detected on system PATH.",
            InstallHelp = InstallHelp
        };
    }
}

/// <summary>
/// Verifies the presence of Bash (Git Bash, WSL, MSYS2, or POSIX bash).
/// </summary>
public class BashToolchainCheck : IToolchainCheck
{
    public string ToolName => "Bash Shell";
    public string Command => "bash";
    public string? RequiredVersion => ">= 4.0";
    public string? InstallHelp => "Install Git for Windows (includes Git Bash) from https://gitforwindows.org/ or WSL (wsl --install).";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await ToolchainProcessHelper.ExecuteCommandAsync("bash", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Bash shell was not detected on system PATH. Install Git for Windows or WSL to run shell scripts.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "GNU bash, version 5.2.32(1)-release (x86_64-pc-msys)"
        var match = Regex.Match(output, @"version\s+([0-9a-zA-Z\.\-\(\)]+)");
        var version = match.Success ? match.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Bash shell detected ({output.Split('\n')[0].Trim()})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

