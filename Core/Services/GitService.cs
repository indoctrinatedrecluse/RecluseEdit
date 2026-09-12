using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

public partial class GitService
{
    [GeneratedRegex(@"^##\s+(?:No commits yet on\s+|Initial commit on\s+)?([^\s\.]+)(?:\.\.\.([^\s]+)(?:\s+\[(?:ahead\s+(\d+))?(?:,\s*)?(?:behind\s+(\d+))?\])?)?", RegexOptions.Compiled)]
    private static partial Regex BranchRegex();

    [GeneratedRegex(@"^@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@", RegexOptions.Compiled)]
    private static partial Regex HunkHeaderRegex();

    public async Task<GitRepoStatus> GetStatusAsync(string? workspacePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return new GitRepoStatus { IsGitRepository = false };
        }

        string safePath = workspacePath;
        var (exitCode, stdout, _) = await RunGitCommandAsync(safePath, "status --porcelain=v1 -b", ct);
        if (exitCode != 0)
        {
            return new GitRepoStatus { IsGitRepository = false };
        }

        return ParseStatusOutput(stdout);
    }

    public static GitRepoStatus ParseStatusOutput(string stdout)
    {
        var status = new GitRepoStatus { IsGitRepository = true };
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length > 0 && lines[0].StartsWith("##"))
        {
            var match = BranchRegex().Match(lines[0]);
            if (match.Success)
            {
                status.Branch = match.Groups[1].Value;
                if (int.TryParse(match.Groups[3].Value, out int ahead)) status.Ahead = ahead;
                if (int.TryParse(match.Groups[4].Value, out int behind)) status.Behind = behind;
            }
            else
            {
                status.Branch = lines[0][2..].Trim();
            }
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length < 3) continue;

            char stagedCode = line[0];
            char unstagedCode = line[1];
            string rawPath = line[3..].Trim();

            // Handle quoted paths (e.g. "path/with space")
            if (rawPath.StartsWith('"') && rawPath.EndsWith('"') && rawPath.Length >= 2)
            {
                rawPath = rawPath[1..^1];
            }

            // Untracked
            if (stagedCode == '?' && unstagedCode == '?')
            {
                status.Files.Add(new GitFileItem
                {
                    FilePath = rawPath,
                    ChangeType = GitChangeType.Untracked,
                    IsStaged = false
                });
                continue;
            }

            // Staged file
            if (stagedCode is not ' ' and not '?')
            {
                var changeType = stagedCode switch
                {
                    'A' => GitChangeType.Added,
                    'D' => GitChangeType.Deleted,
                    'R' => GitChangeType.Renamed,
                    _ => GitChangeType.Modified
                };

                string path = rawPath;
                if (changeType == GitChangeType.Renamed && rawPath.Contains(" -> "))
                {
                    var parts = rawPath.Split([" -> "], StringSplitOptions.None);
                    path = parts.Length > 1 ? parts[1] : rawPath;
                }

                status.Files.Add(new GitFileItem
                {
                    FilePath = path,
                    ChangeType = changeType,
                    IsStaged = true
                });
            }

            // Unstaged changes in working tree
            if (unstagedCode is not ' ' and not '?')
            {
                var changeType = unstagedCode switch
                {
                    'D' => GitChangeType.Deleted,
                    _ => GitChangeType.Modified
                };

                status.Files.Add(new GitFileItem
                {
                    FilePath = rawPath,
                    ChangeType = changeType,
                    IsStaged = false
                });
            }
        }

        return status;
    }

    public async Task<List<DiffHunk>> GetFileDiffHunksAsync(string workspacePath, string relativeFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return [];
        }

        // Run git diff HEAD -U0 -- <path>
        var (exitCode, stdout, _) = await RunGitCommandAsync(workspacePath, $"diff HEAD -U0 -- \"{relativeFilePath}\"", ct);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            // If file is newly added/untracked, check git diff --staged or status
            var (stagedExit, stagedOut, _) = await RunGitCommandAsync(workspacePath, $"diff --staged -U0 -- \"{relativeFilePath}\"", ct);
            if (stagedExit == 0 && !string.IsNullOrWhiteSpace(stagedOut))
            {
                stdout = stagedOut;
            }
            else
            {
                return [];
            }
        }

        return ParseDiffOutput(stdout);
    }

    public static List<DiffHunk> ParseDiffOutput(string stdout)
    {
        var hunks = new List<DiffHunk>();
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.StartsWith("@@")) continue;

            var match = HunkHeaderRegex().Match(line);
            if (!match.Success) continue;

            int oldStart = int.Parse(match.Groups[1].Value);
            int oldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
            int newStart = int.Parse(match.Groups[3].Value);
            int newCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;

            var hunkType = DiffHunkType.Modified;
            if (oldCount == 0)
            {
                hunkType = DiffHunkType.Added;
            }
            else if (newCount == 0)
            {
                hunkType = DiffHunkType.Deleted;
            }

            hunks.Add(new DiffHunk
            {
                OldStartLine = oldStart,
                OldLineCount = oldCount,
                NewStartLine = newStart,
                NewLineCount = newCount,
                Type = hunkType
            });
        }

        return hunks;
    }

    public async Task<(bool success, string output)> StageFileAsync(string workspacePath, string relativeFilePath, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, $"add \"{relativeFilePath}\"", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    public async Task<(bool success, string output)> UnstageFileAsync(string workspacePath, string relativeFilePath, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, $"restore --staged \"{relativeFilePath}\"", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    public async Task<(bool success, string output)> DiscardChangesAsync(string workspacePath, string relativeFilePath, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, $"restore \"{relativeFilePath}\"", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    public async Task<(bool success, string output)> CommitAsync(string workspacePath, string message, CancellationToken ct = default)
    {
        // Escape quotes in commit message
        string safeMsg = message.Replace("\"", "\\\"");
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, $"commit -m \"{safeMsg}\"", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    public async Task<(bool success, string output)> PushAsync(string workspacePath, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, "push", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    public async Task<(bool success, string output)> PullAsync(string workspacePath, CancellationToken ct = default)
    {
        var (exitCode, stdout, stderr) = await RunGitCommandAsync(workspacePath, "pull", ct);
        return (exitCode == 0, exitCode == 0 ? stdout : stderr);
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunGitCommandAsync(string workingDirectory, string arguments, CancellationToken ct)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);

            await proc.WaitForExitAsync(ct);

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            return (proc.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }
}
