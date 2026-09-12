using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class GitServiceTests
{
    [TestMethod]
    public void TestParseStatusBranchAndCounts()
    {
        var stdout = "## main...origin/main [ahead 3, behind 1]\n M src/Program.cs\n?? newfile.txt\n";
        var status = GitService.ParseStatusOutput(stdout);

        Assert.IsTrue(status.IsGitRepository);
        Assert.AreEqual("main", status.Branch);
        Assert.AreEqual(3, status.Ahead);
        Assert.AreEqual(1, status.Behind);
        Assert.HasCount(2, status.Files);

        var modified = status.Files.FirstOrDefault(f => f.FilePath == "src/Program.cs");
        Assert.IsNotNull(modified);
        Assert.IsFalse(modified.IsStaged);
        Assert.AreEqual(GitChangeType.Modified, modified.ChangeType);

        var untracked = status.Files.FirstOrDefault(f => f.FilePath == "newfile.txt");
        Assert.IsNotNull(untracked);
        Assert.IsFalse(untracked.IsStaged);
        Assert.AreEqual(GitChangeType.Untracked, untracked.ChangeType);
    }

    [TestMethod]
    public void TestParseStatusStagedAndRenamed()
    {
        var stdout = "## feature/auth\nA  README.md\nM  appsettings.json\nR  old.txt -> new.txt\n D deleted.cs\n";
        var status = GitService.ParseStatusOutput(stdout);

        Assert.AreEqual("feature/auth", status.Branch);
        Assert.AreEqual(0, status.Ahead);
        Assert.AreEqual(0, status.Behind);

        var stagedList = status.StagedFiles.ToList();
        Assert.HasCount(3, stagedList);

        var added = stagedList.FirstOrDefault(f => f.FilePath == "README.md");
        Assert.IsNotNull(added);
        Assert.AreEqual(GitChangeType.Added, added.ChangeType);

        var modified = stagedList.FirstOrDefault(f => f.FilePath == "appsettings.json");
        Assert.IsNotNull(modified);
        Assert.AreEqual(GitChangeType.Modified, modified.ChangeType);

        var renamed = stagedList.FirstOrDefault(f => f.FilePath == "new.txt");
        Assert.IsNotNull(renamed);
        Assert.AreEqual(GitChangeType.Renamed, renamed.ChangeType);

        var unstagedList = status.UnstagedFiles.ToList();
        Assert.HasCount(1, unstagedList);
        Assert.AreEqual("deleted.cs", unstagedList[0].FilePath);
        Assert.AreEqual(GitChangeType.Deleted, unstagedList[0].ChangeType);
    }

    [TestMethod]
    public void TestParseDiffHunks()
    {
        var diffText = """
            diff --git a/file.txt b/file.txt
            index 83db48f..bf269f4 100644
            --- a/file.txt
            +++ b/file.txt
            @@ -0,0 +1,10 @@
            +new line 1
            +new line 2
            @@ -25,4 +35,6 @@
             context
            -old line
            +modified line
            @@ -50,5 +62,0 @@
            -deleted 1
            -deleted 2
            """;

        var hunks = GitService.ParseDiffOutput(diffText);
        Assert.HasCount(3, hunks);

        // Hunk 1: Added
        Assert.AreEqual(0, hunks[0].OldStartLine);
        Assert.AreEqual(0, hunks[0].OldLineCount);
        Assert.AreEqual(1, hunks[0].NewStartLine);
        Assert.AreEqual(10, hunks[0].NewLineCount);
        Assert.AreEqual(DiffHunkType.Added, hunks[0].Type);

        // Hunk 2: Modified
        Assert.AreEqual(25, hunks[1].OldStartLine);
        Assert.AreEqual(4, hunks[1].OldLineCount);
        Assert.AreEqual(35, hunks[1].NewStartLine);
        Assert.AreEqual(6, hunks[1].NewLineCount);
        Assert.AreEqual(DiffHunkType.Modified, hunks[1].Type);

        // Hunk 3: Deleted
        Assert.AreEqual(50, hunks[2].OldStartLine);
        Assert.AreEqual(5, hunks[2].OldLineCount);
        Assert.AreEqual(62, hunks[2].NewStartLine);
        Assert.AreEqual(0, hunks[2].NewLineCount);
        Assert.AreEqual(DiffHunkType.Deleted, hunks[2].Type);
    }
}
