using System.IO;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages the currently opened workspace folder and tree representation.
/// </summary>
public class WorkspaceManager
{
    private string? _rootPath;
    private FileSystemItem? _rootItem;

    public string? RootPath => _rootPath;
    public FileSystemItem? RootItem => _rootItem;
    public bool HasWorkspace => !string.IsNullOrEmpty(_rootPath) && Directory.Exists(_rootPath);

    public event Action? WorkspaceChanged;

    public void OpenWorkspace(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        _rootPath = Path.GetFullPath(folderPath);
        _rootItem = new FileSystemItem(_rootPath, true);
        _rootItem.LoadChildren();
        _rootItem.IsExpanded = true;

        WorkspaceChanged?.Invoke();
    }

    public void RefreshWorkspace()
    {
        if (!string.IsNullOrEmpty(_rootPath))
        {
            OpenWorkspace(_rootPath);
        }
    }

    public void CloseWorkspace()
    {
        _rootPath = null;
        _rootItem = null;
        WorkspaceChanged?.Invoke();
    }
}
