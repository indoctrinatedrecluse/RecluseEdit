using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class UpdateServiceTests
{
    [TestMethod]
    public void NormalizeVersionString_HandlesVariousFormats()
    {
        Assert.AreEqual("5.5.0", UpdateService.NormalizeVersionString("v5.5.0"));
        Assert.AreEqual("5.5.0", UpdateService.NormalizeVersionString("V5.5.0"));
        Assert.AreEqual("5.5.0", UpdateService.NormalizeVersionString("5.5.0"));
        Assert.AreEqual("1.0.0-preview", UpdateService.NormalizeVersionString("v1.0.0-preview"));
        Assert.AreEqual("0.0.0", UpdateService.NormalizeVersionString(""));
    }

    [TestMethod]
    public void IsNewerVersion_CorrectlyIdentifiesUpdates()
    {
        // Newer versions
        Assert.IsTrue(UpdateService.IsNewerVersion("5.5.0", "5.6.0"));
        Assert.IsTrue(UpdateService.IsNewerVersion("5.5.0", "5.5.1"));
        Assert.IsTrue(UpdateService.IsNewerVersion("5.5.0", "6.0.0"));
        Assert.IsTrue(UpdateService.IsNewerVersion("v5.5.0", "v5.5.1"));
        Assert.IsTrue(UpdateService.IsNewerVersion("5.5.0-beta", "5.5.0"));

        // Same or older versions
        Assert.IsFalse(UpdateService.IsNewerVersion("5.5.0", "5.5.0"));
        Assert.IsFalse(UpdateService.IsNewerVersion("5.5.0", "5.4.0"));
        Assert.IsFalse(UpdateService.IsNewerVersion("5.5.1", "5.5.0"));
        Assert.IsFalse(UpdateService.IsNewerVersion("6.0.0", "5.5.0"));
    }

    [TestMethod]
    public void DetermineAssetType_IdentifiesZipAndExe()
    {
        Assert.AreEqual(UpdateAssetType.ZipArchive, UpdateService.DetermineAssetType("RecluseEdit-windows-v5.5.0.zip"));
        Assert.AreEqual(UpdateAssetType.ZipArchive, UpdateService.DetermineAssetType("package.ZIP"));
        Assert.AreEqual(UpdateAssetType.ExecutableInstaller, UpdateService.DetermineAssetType("RecluseEdit-Setup.exe"));
        Assert.AreEqual(UpdateAssetType.ExecutableInstaller, UpdateService.DetermineAssetType("installer.EXE"));
        Assert.AreEqual(UpdateAssetType.Unknown, UpdateService.DetermineAssetType("checksums.txt"));
        Assert.AreEqual(UpdateAssetType.Unknown, UpdateService.DetermineAssetType(""));
    }

    [TestMethod]
    public void SelectMatchingAsset_PrefersWindowsZipArchive()
    {
        var assets = new List<UpdateAssetInfo>
        {
            new() { Name = "checksums.txt", DownloadUrl = "https://example.com/sums", AssetType = UpdateAssetType.Unknown },
            new() { Name = "RecluseEdit-linux.tar.gz", DownloadUrl = "https://example.com/linux", AssetType = UpdateAssetType.Unknown },
            new() { Name = "RecluseEdit-windows-v5.6.0.zip", DownloadUrl = "https://example.com/winzip", AssetType = UpdateAssetType.ZipArchive, SizeBytes = 25000000 },
            new() { Name = "RecluseEdit-Setup.exe", DownloadUrl = "https://example.com/exe", AssetType = UpdateAssetType.ExecutableInstaller }
        };

        var selected = UpdateService.SelectMatchingAsset(assets);
        Assert.IsNotNull(selected);
        Assert.AreEqual("RecluseEdit-windows-v5.6.0.zip", selected.Name);
        Assert.AreEqual("https://example.com/winzip", selected.DownloadUrl);
        Assert.AreEqual(UpdateAssetType.ZipArchive, selected.AssetType);
        Assert.AreEqual("23.8 MB", selected.FormattedSize);
    }

    [TestMethod]
    public void ParseGitHubReleaseJson_ParsesUpdateAvailable()
    {
        var json = """
        {
          "tag_name": "v5.6.0",
          "name": "RecluseEdit v5.6.0 - Auto Updater & Enhancements",
          "body": "### New Features\n- Added auto-updater\n- Improved UI",
          "html_url": "https://github.com/indoctrinatedrecluse/RecluseEdit/releases/tag/v5.6.0",
          "published_at": "2026-09-18T20:00:00Z",
          "assets": [
            {
              "name": "RecluseEdit-windows-v5.6.0.zip",
              "browser_download_url": "https://github.com/indoctrinatedrecluse/RecluseEdit/releases/download/v5.6.0/RecluseEdit-windows-v5.6.0.zip",
              "size": 26214400
            }
          ]
        }
        """;

        var result = UpdateService.ParseGitHubReleaseJson(json, "5.5.0");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsUpdateAvailable);
        Assert.AreEqual("5.5.0", result.CurrentVersion);
        Assert.AreEqual("5.6.0", result.LatestVersion);
        Assert.AreEqual("RecluseEdit v5.6.0 - Auto Updater & Enhancements", result.ReleaseTitle);
        StringAssert.Contains(result.ReleaseNotes, "Added auto-updater");
        Assert.AreEqual("https://github.com/indoctrinatedrecluse/RecluseEdit/releases/tag/v5.6.0", result.HtmlUrl);
        Assert.IsNotNull(result.Asset);
        Assert.AreEqual("RecluseEdit-windows-v5.6.0.zip", result.Asset.Name);
        Assert.AreEqual(26214400, result.Asset.SizeBytes);
        Assert.AreEqual("25.0 MB", result.Asset.FormattedSize);
    }

    [TestMethod]
    public void ParseGitHubReleaseJson_ParsesUpToDate()
    {
        var json = """
        {
          "tag_name": "v5.5.0",
          "name": "RecluseEdit v5.5.0",
          "body": "Current release notes",
          "html_url": "https://github.com/indoctrinatedrecluse/RecluseEdit/releases/tag/v5.5.0",
          "published_at": "2026-09-14T15:38:18Z",
          "assets": []
        }
        """;

        var result = UpdateService.ParseGitHubReleaseJson(json, "5.5.0");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsUpdateAvailable);
        Assert.AreEqual("5.5.0", result.CurrentVersion);
        Assert.AreEqual("5.5.0", result.LatestVersion);
    }

    [TestMethod]
    public void GenerateUpdaterScriptContent_ProducesValidScript()
    {
        var script = UpdateService.GenerateUpdaterScriptContent(
            targetDir: @"C:\Program Files\RecluseEdit",
            sourceDir: @"C:\Users\AppData\Temp\staged",
            parentPid: 1234,
            exePath: @"C:\Program Files\RecluseEdit\RecluseEdit.exe");

        StringAssert.Contains(script, "1234", "Script must check parent process ID");
        StringAssert.Contains(script, "Get-Process -Id $pidToWait", "Script must wait for parent PID");
        StringAssert.Contains(script, "Copy-Item", "Script must copy items to target");
        StringAssert.Contains(script, "Start-Process", "Script must relaunch application");
        StringAssert.Contains(script, @"C:\Program Files\RecluseEdit", "Script must target specified installation path");
    }

    [TestMethod]
    public void MainWindow_HasCheckForUpdatesCommandRegistered()
    {
        Assert.IsNotNull(MainWindow.CheckForUpdatesCommand);
        Assert.AreEqual("Check for Updates", MainWindow.CheckForUpdatesCommand.Text);
    }
}

