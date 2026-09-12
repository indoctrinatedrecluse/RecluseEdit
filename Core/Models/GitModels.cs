namespace RecluseEdit.Core.Models;

public enum GitChangeType
{
    Modified,
    Added,
    Deleted,
    Untracked,
    Renamed
}

public enum DiffHunkType
{
    Added,
    Modified,
    Deleted
}

public class GitFileItem
{
    public required string FilePath { get; set; }
    public GitChangeType ChangeType { get; set; }
    public bool IsStaged { get; set; }

    public string FileName => System.IO.Path.GetFileName(FilePath);
    public string DirectoryPath => System.IO.Path.GetDirectoryName(FilePath) ?? "";

    public string StatusBadge => ChangeType switch
    {
        GitChangeType.Added => "A",
        GitChangeType.Modified => "M",
        GitChangeType.Deleted => "D",
        GitChangeType.Untracked => "U",
        GitChangeType.Renamed => "R",
        _ => "?"
    };

    public string BadgeColor => ChangeType switch
    {
        GitChangeType.Added => "#4EC9B0",
        GitChangeType.Modified => "#E5C07B",
        GitChangeType.Deleted => "#E06C75",
        GitChangeType.Untracked => "#73C991",
        _ => "#888888"
    };
}

public class GitRepoStatus
{
    public bool IsGitRepository { get; set; }
    public string Branch { get; set; } = "HEAD";
    public int Ahead { get; set; }
    public int Behind { get; set; }
    public List<GitFileItem> Files { get; set; } = [];

    public IEnumerable<GitFileItem> StagedFiles => Files.Where(f => f.IsStaged);
    public IEnumerable<GitFileItem> UnstagedFiles => Files.Where(f => !f.IsStaged);
}

public class DiffHunk
{
    public int OldStartLine { get; set; }
    public int OldLineCount { get; set; }
    public int NewStartLine { get; set; }
    public int NewLineCount { get; set; }
    public DiffHunkType Type { get; set; }
}

