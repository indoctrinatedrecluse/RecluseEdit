using System.Collections.ObjectModel;
using System.IO;

namespace RecluseEdit.Core.Models;

/// <summary>
/// Represents a file or folder node in the Workspace Explorer tree.
/// </summary>
public class FileSystemItem
{
    private bool _isExpanded;
    private bool _hasLoadedChildren;

    public string FullPath { get; }
    public string Name { get; }
    public bool IsDirectory { get; }
    public string Icon { get; }
    public ObservableCollection<FileSystemItem> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                if (_isExpanded && !_hasLoadedChildren && IsDirectory)
                {
                    LoadChildren();
                }
            }
        }
    }

    public FileSystemItem(string path, bool isDirectory)
    {
        FullPath = path;
        IsDirectory = isDirectory;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name) && isDirectory)
        {
            Name = path; // Root drive like C:\
        }

        Icon = GetIcon(path, isDirectory);

        if (isDirectory)
        {
            // Add dummy child so WPF TreeView renders the expand arrow
            Children.Add(new FileSystemItem("loading...", false));
        }
    }

    public void LoadChildren()
    {
        if (!IsDirectory || _hasLoadedChildren) return;

        Children.Clear();
        _hasLoadedChildren = true;

        try
        {
            var dirInfo = new DirectoryInfo(FullPath);

            // Directories first
            foreach (var subDir in dirInfo.GetDirectories().OrderBy(d => d.Name))
            {
                // Skip hidden/system folders like .git, bin, obj, .vs
                if ((subDir.Attributes & FileAttributes.Hidden) != 0 ||
                    subDir.Name is ".git" or ".vs" or "bin" or "obj")
                {
                    continue;
                }

                Children.Add(new FileSystemItem(subDir.FullName, true));
            }

            // Files
            foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
            {
                if ((file.Attributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }

                Children.Add(new FileSystemItem(file.FullName, false));
            }
        }
        catch
        {
            // Access denied or unreadable directory
        }
    }

    private static string GetIcon(string path, bool isDirectory)
    {
        if (isDirectory) return "📁";

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" or ".htm" => "🌐",
            ".css" or ".scss" or ".sass" or ".less" => "🎨",
            ".js" or ".mjs" or ".cjs" => "📜",
            ".ts" or ".tsx" => "🔷",
            ".jsx" => "⚛️",
            ".json" => "📋",
            ".xml" or ".svg" or ".xaml" => "📐",
            ".cs" => "🟣",
            ".md" => "📝",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".ico" => "🖼️",
            _ => "📄"
        };
    }
}

