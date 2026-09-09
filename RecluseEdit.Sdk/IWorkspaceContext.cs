namespace RecluseEdit.Sdk;

/// <summary>
/// Provides access to editor workspace state, document manipulation, file I/O, and secure command execution.
/// </summary>
public interface IWorkspaceContext
{
    /// <summary>
    /// Gets the absolute root path of the currently opened workspace directory, if any.
    /// </summary>
    string? WorkspaceRoot { get; }

    /// <summary>
    /// Gets the absolute path of the currently active document, if any.
    /// </summary>
    string? ActiveFilePath { get; }

    /// <summary>
    /// Gets the text content of the currently active document.
    /// </summary>
    string? ActiveDocumentContent { get; }

    /// <summary>
    /// Gets the currently selected text in the active editor.
    /// </summary>
    string? SelectedText { get; }

    /// <summary>
    /// Opens the specified file in the editor tabs.
    /// </summary>
    void OpenFile(string path);

    /// <summary>
    /// Reads text from a workspace file (relative to workspace root or absolute).
    /// </summary>
    Task<string> ReadFileAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Writes text to a workspace file (creates parent directories if needed).
    /// </summary>
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);

    /// <summary>
    /// Lists files and subdirectories within a workspace directory.
    /// </summary>
    Task<IReadOnlyList<string>> ListFilesAsync(string path = "", CancellationToken ct = default);

    /// <summary>
    /// Prompts the user for explicit permission to run a command, and if granted, executes the command.
    /// </summary>
    Task<CommandExecutionResult> ExecuteCommandAsync(string command, string? workingDirectory = null, CancellationToken ct = default);

    /// <summary>
    /// Displays a confirmation dialog to the user requesting approval for an action.
    /// </summary>
    Task<bool> RequestUserConfirmationAsync(string title, string prompt);
}

/// <summary>
/// Result of executing a shell command in the workspace.
/// </summary>
public class CommandExecutionResult
{
    public required int ExitCode { get; init; }
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
    public bool UserApproved { get; init; } = true;
    public bool Succeeded => UserApproved && ExitCode == 0;
}

