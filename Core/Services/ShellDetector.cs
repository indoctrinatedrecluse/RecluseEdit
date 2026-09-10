using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Detects installed and configured shells on the host operating system,
/// retaining unavailable shells with an IsAvailable = false flag.
/// </summary>
public class ShellDetector
{
    private readonly ShellSettingsService _settingsService;

    public ShellDetector(ShellSettingsService? settingsService = null)
    {
        _settingsService = settingsService ?? new ShellSettingsService();
    }

    /// <summary>
    /// Scans the operating system, user settings, and PATH to return the full catalog of shells.
    /// </summary>
    public List<ShellInfo> DetectShells()
    {
        var discovered = new List<ShellInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            DetectWindowsShells(discovered, seenPaths);
        }
        else
        {
            DetectUnixShells(discovered, seenPaths);
        }

        // Scan system PATH for any additional shell binaries not in canonical catalog
        ScanPathForAdditionalShells(discovered, seenPaths);

        // Pick default shell among available shells
        var defaultShell = discovered.FirstOrDefault(s => s.IsAvailable && s.Id == "pwsh")
                           ?? discovered.FirstOrDefault(s => s.IsAvailable && s.Id == "powershell")
                           ?? discovered.FirstOrDefault(s => s.IsAvailable && s.Id == "git-bash")
                           ?? discovered.FirstOrDefault(s => s.IsAvailable && s.Id == "cmd")
                           ?? discovered.FirstOrDefault(s => s.IsAvailable)
                           ?? discovered.FirstOrDefault();

        if (defaultShell != null)
        {
            defaultShell.IsDefault = true;
        }

        return discovered;
    }

    private void DetectWindowsShells(List<ShellInfo> shells, HashSet<string> seenPaths)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

        // 1. PowerShell 7+
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "pwsh",
            DisplayName = "PowerShell 7",
            Icon = "⚡",
            Arguments = "-NoLogo",
            ExpectedBinaryNames = ["pwsh.exe", "powershell.exe"],
            Description = "Cross-platform modern PowerShell 7+"
        },
        candidates:
        [
            Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe"),
            Path.Combine(localAppData, "Microsoft", "PowerShell", "pwsh.exe")
        ],
        pathBinaries: ["pwsh.exe"]);

        // 2. Windows PowerShell
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "powershell",
            DisplayName = "Windows PowerShell",
            Icon = "⚡",
            Arguments = "-NoLogo",
            ExpectedBinaryNames = ["powershell.exe", "pwsh.exe"],
            Description = "Built-in Windows PowerShell 5.1"
        },
        candidates:
        [
            Path.Combine(system32, "WindowsPowerShell", "v1.0", "powershell.exe")
        ],
        pathBinaries: ["powershell.exe"]);

        // 3. Command Prompt
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "cmd",
            DisplayName = "Command Prompt",
            Icon = ">_",
            Arguments = "",
            ExpectedBinaryNames = ["cmd.exe"],
            Description = "Standard Windows Command Processor"
        },
        candidates:
        [
            Path.Combine(system32, "cmd.exe")
        ],
        pathBinaries: ["cmd.exe"]);

        // 4. Git Bash
        var gitBashCandidates = new List<string>
        {
            Path.Combine(programFiles, "Git", "bin", "bash.exe"),
            Path.Combine(programFiles, "Git", "usr", "bin", "bash.exe"),
            Path.Combine(programFilesX86, "Git", "bin", "bash.exe"),
            Path.Combine(localAppData, "Programs", "Git", "bin", "bash.exe")
        };

        var gitFromPath = FindInPath("git.exe");
        if (!string.IsNullOrEmpty(gitFromPath))
        {
            var gitDir = Path.GetDirectoryName(gitFromPath);
            if (!string.IsNullOrEmpty(gitDir))
            {
                var parent = Directory.GetParent(gitDir)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    gitBashCandidates.Add(Path.Combine(parent, "bin", "bash.exe"));
                    gitBashCandidates.Add(Path.Combine(parent, "usr", "bin", "bash.exe"));
                }
            }
        }

        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "git-bash",
            DisplayName = "Git Bash",
            Icon = "🐚",
            Arguments = "--login -i",
            ExpectedBinaryNames = ["bash.exe", "sh.exe", "git-bash.exe"],
            Description = "Git for Windows Bash terminal"
        },
        candidates: gitBashCandidates.ToArray(),
        pathBinaries: ["bash.exe"]);

        // 5. WSL (Linux)
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "wsl",
            DisplayName = "WSL (Linux)",
            Icon = "🐧",
            Arguments = "",
            ExpectedBinaryNames = ["wsl.exe"],
            Description = "Windows Subsystem for Linux"
        },
        candidates:
        [
            Path.Combine(system32, "wsl.exe")
        ],
        pathBinaries: ["wsl.exe"]);

        // 6. Cygwin Bash
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "cygwin",
            DisplayName = "Cygwin Bash",
            Icon = "🐚",
            Arguments = "--login -i",
            ExpectedBinaryNames = ["bash.exe", "sh.exe"],
            Description = "Cygwin POSIX environment shell"
        },
        candidates:
        [
            @"C:\cygwin64\bin\bash.exe",
            @"C:\cygwin\bin\bash.exe"
        ],
        pathBinaries: []);

        // 7. MSYS2 Bash
        RegisterShell(shells, seenPaths, new ShellInfo
        {
            Id = "msys2",
            DisplayName = "MSYS2 Bash",
            Icon = "🐚",
            Arguments = "--login -i",
            ExpectedBinaryNames = ["bash.exe", "sh.exe"],
            Description = "MSYS2 software development shell"
        },
        candidates:
        [
            @"C:\msys64\usr\bin\bash.exe",
            @"C:\msys\usr\bin\bash.exe"
        ],
        pathBinaries: []);
    }

    private void DetectUnixShells(List<ShellInfo> shells, HashSet<string> seenPaths)
    {
        var unixDefinitions = new[]
        {
            ("zsh", "Zsh", "🐚", "-l -i", new[] { "/bin/zsh", "/usr/bin/zsh" }, new[] { "zsh" }),
            ("bash", "Bash", "🐚", "-l -i", new[] { "/bin/bash", "/usr/bin/bash" }, new[] { "bash" }),
            ("sh", "Sh", ">_", "-i", new[] { "/bin/sh", "/usr/bin/sh" }, new[] { "sh" })
        };

        foreach (var (id, name, icon, args, candidates, pathBinaries) in unixDefinitions)
        {
            RegisterShell(shells, seenPaths, new ShellInfo
            {
                Id = id,
                DisplayName = name,
                Icon = icon,
                Arguments = args,
                ExpectedBinaryNames = pathBinaries
            },
            candidates,
            pathBinaries);
        }
    }

    private void RegisterShell(List<ShellInfo> shells, HashSet<string> seenPaths, ShellInfo shell, string[] candidates, string[] pathBinaries)
    {
        // 1. Check custom path from settings
        var customPath = _settingsService.GetCustomPath(shell.Id);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
        {
            shell.ExecutablePath = Path.GetFullPath(customPath);
            shell.IsAvailable = true;
            shell.IsCustomConfigured = true;
            seenPaths.Add(shell.ExecutablePath);
            shells.Add(shell);
            return;
        }

        // 2. Check candidate standard paths
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                var full = Path.GetFullPath(candidate);
                shell.ExecutablePath = full;
                shell.IsAvailable = true;
                seenPaths.Add(full);
                shells.Add(shell);
                return;
            }
        }

        // 3. Check PATH
        foreach (var bin in pathBinaries)
        {
            var fromPath = FindInPath(bin);
            if (!string.IsNullOrEmpty(fromPath) && File.Exists(fromPath))
            {
                var full = Path.GetFullPath(fromPath);
                shell.ExecutablePath = full;
                shell.IsAvailable = true;
                seenPaths.Add(full);
                shells.Add(shell);
                return;
            }
        }

        // 4. Not found on disk: keep in catalog but mark as unavailable
        shell.ExecutablePath = candidates.FirstOrDefault() ?? (pathBinaries.FirstOrDefault() ?? "");
        shell.IsAvailable = false;
        shells.Add(shell);
    }

    private void ScanPathForAdditionalShells(List<ShellInfo> shells, HashSet<string> seenPaths)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv)) return;

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var dirs = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        var targetExes = OperatingSystem.IsWindows()
            ? new[] { "nu.exe", "fish.exe" }
            : new[] { "fish", "nu" };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var exe in targetExes)
            {
                var fullPath = Path.Combine(dir, exe);
                if (File.Exists(fullPath) && seenPaths.Add(Path.GetFullPath(fullPath)))
                {
                    var id = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    shells.Add(new ShellInfo
                    {
                        Id = id,
                        DisplayName = char.ToUpperInvariant(id[0]) + id[1..],
                        ExecutablePath = Path.GetFullPath(fullPath),
                        Arguments = "-i",
                        Icon = "🐚",
                        IsAvailable = true,
                        ExpectedBinaryNames = [exe]
                    });
                }
            }
        }
    }

    private string? FindInPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv)) return null;

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var dirs = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in dirs)
        {
            try
            {
                var full = Path.Combine(dir, fileName);
                if (File.Exists(full)) return full;
            }
            catch
            {
                // ignore path access errors
            }
        }
        return null;
    }
}
