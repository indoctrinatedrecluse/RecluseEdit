using System.Diagnostics;
using System.IO;
using System.Windows;
using RecluseEdit.Sdk;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Provides concrete workspace file operations, active editor state, and security-governed command execution.
/// </summary>
public class WorkspaceContext : IWorkspaceContext
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly DocumentManager _documentManager;

    public WorkspaceContext(WorkspaceManager workspaceManager, DocumentManager documentManager)
    {
        _workspaceManager = workspaceManager;
        _documentManager = documentManager;
    }

    public string? WorkspaceRoot => _workspaceManager.RootPath ?? AppDomain.CurrentDomain.BaseDirectory;

    public string? ActiveFilePath => _documentManager.ActiveDocument?.FilePath;

    public string? ActiveDocumentContent => _documentManager.ActiveDocument?.Document.Text;

    public string? SelectedText => null;

    public void OpenFile(string path)
    {
        var resolved = ResolvePath(path);
        if (File.Exists(resolved) && Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _documentManager.OpenDocument(resolved);
            });
        }
    }

    public async Task<string> ReadFileAsync(string path, CancellationToken ct = default)
    {
        var resolved = ResolvePath(path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"File '{path}' does not exist in workspace.");
        }
        return await File.ReadAllTextAsync(resolved, ct);
    }

    public async Task WriteFileAsync(string path, string content, CancellationToken ct = default)
    {
        var resolved = ResolvePath(path);
        var dir = Path.GetDirectoryName(resolved);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(resolved, content, ct);

        // Sync with open document tabs if active
        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var openDoc = _documentManager.Documents.FirstOrDefault(d =>
                    !string.IsNullOrEmpty(d.FilePath) &&
                    string.Equals(Path.GetFullPath(d.FilePath), resolved, StringComparison.OrdinalIgnoreCase));

                if (openDoc != null)
                {
                    openDoc.Document.Text = content;
                    openDoc.IsDirty = false;
                }
                else
                {
                    _documentManager.OpenDocument(resolved);
                }
            });
        }
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string path = "", CancellationToken ct = default)
    {
        var resolved = string.IsNullOrWhiteSpace(path) ? WorkspaceRoot : ResolvePath(path);
        if (string.IsNullOrEmpty(resolved) || !Directory.Exists(resolved))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var results = new List<string>();
        var entries = Directory.GetFileSystemEntries(resolved, "*", SearchOption.TopDirectoryOnly);
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (name.StartsWith(".") || name == "bin" || name == "obj" || name == "node_modules")
                continue;

            var isDir = Directory.Exists(entry);
            results.Add(isDir ? name + "/" : name);
        }

        return Task.FromResult<IReadOnlyList<string>>(results);
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(string command, string? workingDirectory = null, CancellationToken ct = default)
    {
        var cwd = workingDirectory ?? WorkspaceRoot ?? AppDomain.CurrentDomain.BaseDirectory;

        // 1. Mandatory User Confirmation Prompt
        var prompt = $"The AI Assistant requests permission to execute the following shell command:\n\nFolder: {cwd}\n\nCommand:\n> {command}\n\nDo you approve executing this command on your machine?";
        var approved = await RequestUserConfirmationAsync("Security Permission Request - Execute Command", prompt);
        if (!approved)
        {
            return new CommandExecutionResult
            {
                ExitCode = -1,
                StandardOutput = "",
                StandardError = "Execution cancelled: The user explicitly denied command execution.",
                UserApproved = false
            };
        }

        // 2. Safe process execution
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var fileName = isWindows ? "cmd.exe" : "sh";
            var arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"";

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = cwd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new CommandExecutionResult
                {
                    ExitCode = -1,
                    StandardOutput = "",
                    StandardError = "Failed to spawn process.",
                    UserApproved = true
                };
            }

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return new CommandExecutionResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                UserApproved = true
            };
        }
        catch (Exception ex)
        {
            return new CommandExecutionResult
            {
                ExitCode = -1,
                StandardOutput = "",
                StandardError = ex.Message,
                UserApproved = true
            };
        }
    }

    public Task<bool> RequestUserConfirmationAsync(string title, string prompt)
    {
        if (Application.Current == null)
        {
            return Task.FromResult(true);
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = Application.Current.MainWindow;
            var res = window != null
                ? MessageBox.Show(window, prompt, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                : MessageBox.Show(prompt, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return res == MessageBoxResult.Yes;
        }).Task;
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }
        var root = WorkspaceRoot ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(root, path));
    }
}
