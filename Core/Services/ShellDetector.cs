using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Detects available shell executables installed on the system and in PATH.
/// </summary>
public class ShellDetector
{
    /// <summary>
    /// Scans the operating system and environment to return all available shells.
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

        // Scan system PATH for any additional shell binaries
        ScanPathForShells(discovered, seenPaths);

        // Pick default shell
        if (discovered.Count > 0)
        {
            var defaultShell = discovered.FirstOrDefault(s => s.Id == "pwsh")
                               ?? discovered.FirstOrDefault(s => s.Id == "powershell")
                               ?? discovered.FirstOrDefault(s => s.Id == "git-bash")
                               ?? discovered.FirstOrDefault(s => s.Id == "cmd")
                               ?? discovered.First();
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

        // 1. PowerShell 7+ (pwsh.exe)
        var pwshCandidates = new[]
        {
            Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe"),
            Path.Combine(localAppData, "Microsoft", "PowerShell", "pwsh.exe")
        };
        foreach (var p in pwshCandidates)
        {
            if (File.Exists(p) && seenPaths.Add(Path.GetFullPath(p)))
            {
                shells.Add(new ShellInfo
                {
                    Id = "pwsh",
                    DisplayName = "PowerShell 7",
                    ExecutablePath = Path.GetFullPath(p),
                    Arguments = "-NoLogo -NoExit",
                    Icon = "⚡"
                });
                break;
            }
        }

        // 2. Windows PowerShell (powershell.exe)
        var psPath = Path.Combine(system32, "WindowsPowerShell", "v1.0", "powershell.exe");
        if (File.Exists(psPath) && seenPaths.Add(Path.GetFullPath(psPath)))
        {
            shells.Add(new ShellInfo
            {
                Id = "powershell",
                DisplayName = "Windows PowerShell",
                ExecutablePath = Path.GetFullPath(psPath),
                Arguments = "-NoLogo -NoExit",
                Icon = "⚡"
            });
        }

        // 3. Command Prompt (cmd.exe)
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (File.Exists(cmdPath) && seenPaths.Add(Path.GetFullPath(cmdPath)))
        {
            shells.Add(new ShellInfo
            {
                Id = "cmd",
                DisplayName = "Command Prompt",
                ExecutablePath = Path.GetFullPath(cmdPath),
                Arguments = "/K",
                Icon = ">_"
            });
        }

        // 4. Git Bash
        var gitBashCandidates = new List<string>
        {
            Path.Combine(programFiles, "Git", "bin", "bash.exe"),
            Path.Combine(programFiles, "Git", "usr", "bin", "bash.exe"),
            Path.Combine(programFilesX86, "Git", "bin", "bash.exe"),
            Path.Combine(localAppData, "Programs", "Git", "bin", "bash.exe")
        };

        // Also resolve Git Bash relative to git.exe in PATH if available
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

        foreach (var p in gitBashCandidates)
        {
            if (File.Exists(p) && seenPaths.Add(Path.GetFullPath(p)))
            {
                shells.Add(new ShellInfo
                {
                    Id = "git-bash",
                    DisplayName = "Git Bash",
                    ExecutablePath = Path.GetFullPath(p),
                    Arguments = "--login -i",
                    Icon = "🐚"
                });
                break;
            }
        }

        // 5. WSL (wsl.exe)
        var wslPath = Path.Combine(system32, "wsl.exe");
        if (File.Exists(wslPath) && seenPaths.Add(Path.GetFullPath(wslPath)))
        {
            shells.Add(new ShellInfo
            {
                Id = "wsl",
                DisplayName = "WSL (Linux)",
                ExecutablePath = Path.GetFullPath(wslPath),
                Arguments = "",
                Icon = "🐧"
            });
        }

        // 6. Cygwin & MSYS2
        var cygwinCandidates = new[]
        {
            (@"C:\cygwin64\bin\bash.exe", "cygwin", "Cygwin Bash"),
            (@"C:\cygwin\bin\bash.exe", "cygwin", "Cygwin Bash"),
            (@"C:\msys64\usr\bin\bash.exe", "msys2", "MSYS2 Bash")
        };
        foreach (var (path, id, name) in cygwinCandidates)
        {
            if (File.Exists(path) && seenPaths.Add(Path.GetFullPath(path)))
            {
                shells.Add(new ShellInfo
                {
                    Id = id,
                    DisplayName = name,
                    ExecutablePath = Path.GetFullPath(path),
                    Arguments = "--login -i",
                    Icon = "🐚"
                });
            }
        }
    }

    private void DetectUnixShells(List<ShellInfo> shells, HashSet<string> seenPaths)
    {
        var unixCandidates = new[]
        {
            ("/bin/zsh", "zsh", "Zsh", "🐚"),
            ("/usr/bin/zsh", "zsh", "Zsh", "🐚"),
            ("/bin/bash", "bash", "Bash", "🐚"),
            ("/usr/bin/bash", "bash", "Bash", "🐚"),
            ("/bin/sh", "sh", "Sh", ">_")
        };

        foreach (var (path, id, name, icon) in unixCandidates)
        {
            if (File.Exists(path) && seenPaths.Add(Path.GetFullPath(path)))
            {
                shells.Add(new ShellInfo
                {
                    Id = id,
                    DisplayName = name,
                    ExecutablePath = path,
                    Arguments = "-l -i",
                    Icon = icon
                });
            }
        }
    }

    private void ScanPathForShells(List<ShellInfo> shells, HashSet<string> seenPaths)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv)) return;

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var dirs = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        var targetExes = OperatingSystem.IsWindows()
            ? new[] { "pwsh.exe", "powershell.exe", "bash.exe", "cmd.exe", "wsl.exe" }
            : new[] { "pwsh", "zsh", "bash", "sh" };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var exe in targetExes)
            {
                var fullPath = Path.Combine(dir, exe);
                if (File.Exists(fullPath) && seenPaths.Add(Path.GetFullPath(fullPath)))
                {
                    var id = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    var name = id switch
                    {
                        "pwsh" => "PowerShell 7",
                        "powershell" => "Windows PowerShell",
                        "bash" => "Bash",
                        "cmd" => "Command Prompt",
                        "wsl" => "WSL",
                        "zsh" => "Zsh",
                        _ => id
                    };
                    var icon = id.Contains("power") ? "⚡" : id.Contains("bash") || id.Contains("zsh") ? "🐚" : id == "wsl" ? "🐧" : ">_";

                    shells.Add(new ShellInfo
                    {
                        Id = id,
                        DisplayName = name,
                        ExecutablePath = Path.GetFullPath(fullPath),
                        Arguments = id.Contains("power") ? "-NoLogo -NoExit" : id.Contains("bash") ? "--login -i" : id == "cmd" ? "/K" : "",
                        Icon = icon
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
