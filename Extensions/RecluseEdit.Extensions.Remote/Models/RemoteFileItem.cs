using System;

namespace RecluseEdit.Extensions.Remote.Models;

/// <summary>
/// Represents a file or directory on a remote server accessible over SFTP.
/// </summary>
public class RemoteFileItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Length { get; set; }
    public string Permissions { get; set; } = string.Empty;
    public DateTime LastWriteTime { get; set; }

    public string Icon => IsDirectory ? "📁" : GetFileIcon(Name);

    public string HumanSize
    {
        get
        {
            if (IsDirectory) return "<DIR>";
            if (Length < 1024) return $"{Length} B";
            if (Length < 1024 * 1024) return $"{Length / 1024.0:F1} KB";
            if (Length < 1024 * 1024 * 1024) return $"{Length / (1024.0 * 1024.0):F1} MB";
            return $"{Length / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    private static string GetFileIcon(string filename)
    {
        var ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".sh" or ".bash" or ".zsh" => "📜",
            ".json" or ".yaml" or ".yml" or ".toml" => "⚙️",
            ".js" or ".ts" or ".jsx" or ".tsx" => "⚡",
            ".py" => "🐍",
            ".rs" => "🦀",
            ".go" => "🐹",
            ".cs" or ".csproj" => "🟣",
            ".html" or ".htm" => "🌐",
            ".css" or ".scss" => "🎨",
            ".log" or ".txt" => "📄",
            ".zip" or ".tar" or ".gz" or ".7z" => "📦",
            ".png" or ".jpg" or ".jpeg" or ".svg" => "🖼️",
            _ => "📄"
        };
    }
}

