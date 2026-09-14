using System.IO;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Tests;

[TestClass]
public class SdkPathResolverTests
{
    [TestMethod]
    public void SdkPathResolver_ResolvesExecutableFromPath_ForKnownTool()
    {
        var path = SdkPathResolver.ResolveExecutable("dotnet");

        Assert.IsNotNull(path);
        Assert.IsTrue(File.Exists(path), $"Resolved path does not exist: {path}");
        if (OperatingSystem.IsWindows())
        {
            Assert.IsTrue(path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase), $"Expected .exe path but got: {path}");
        }
    }

    [TestMethod]
    public void SdkPathResolver_ResolvesGit_FromStandardOrPath()
    {
        var path = SdkPathResolver.ResolveExecutable("git");

        // Git should be available in standard developer environments
        if (path != null)
        {
            Assert.IsTrue(File.Exists(path));
        }
    }

    [TestMethod]
    public void SdkPathResolver_ResolvesWorkspaceLocalBinary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RecluseEdit_Test_LocalBin_" + Guid.NewGuid().ToString("N"));
        var nodeBin = Path.Combine(tempDir, "node_modules", ".bin");
        Directory.CreateDirectory(nodeBin);

        try
        {
            var fakeTool = Path.Combine(nodeBin, OperatingSystem.IsWindows() ? "local-vite.cmd" : "local-vite");
            File.WriteAllText(fakeTool, "@echo off\necho 5.0.0");

            var resolved = SdkPathResolver.ResolveExecutable("local-vite", tempDir);

            Assert.IsNotNull(resolved);
            Assert.IsTrue(File.Exists(resolved));
            Assert.AreEqual(Path.GetFullPath(fakeTool), Path.GetFullPath(resolved));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void SdkPathResolver_ResolvesVenvBinary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RecluseEdit_Test_VenvBin_" + Guid.NewGuid().ToString("N"));
        var scriptsDir = OperatingSystem.IsWindows()
            ? Path.Combine(tempDir, ".venv", "Scripts")
            : Path.Combine(tempDir, ".venv", "bin");
        Directory.CreateDirectory(scriptsDir);

        try
        {
            var fakePy = Path.Combine(scriptsDir, OperatingSystem.IsWindows() ? "test-python.exe" : "test-python");
            File.WriteAllText(fakePy, "binary placeholder");

            var resolved = SdkPathResolver.ResolveExecutable("test-python", tempDir);

            Assert.IsNotNull(resolved);
            Assert.IsTrue(File.Exists(resolved));
            Assert.AreEqual(Path.GetFullPath(fakePy), Path.GetFullPath(resolved));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void SdkPathResolver_ReturnsNull_ForNonExistentTool()
    {
        var resolved = SdkPathResolver.ResolveExecutable("non_existent_compiler_xyz_404");
        Assert.IsNull(resolved);
    }

    [TestMethod]
    public async Task ToolchainExecutor_ExecutesCommand_AndExtractsVersion()
    {
        var result = await ToolchainExecutor.ExecuteAsync("dotnet", "--version");

        Assert.IsTrue(result.Success, $"Expected success but got: {result.Output}");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Output));
        Assert.IsNotNull(result.DetectedVersion);
        Assert.IsNotNull(result.ResolvedPath);
        Assert.IsTrue(File.Exists(result.ResolvedPath));
    }

    [TestMethod]
    public async Task ToolchainExecutor_HandlesInvalidCommand_Gracefully()
    {
        var result = await ToolchainExecutor.ExecuteAsync("non_existent_tool_123456", "--version");

        Assert.IsFalse(result.Success);
        Assert.AreNotEqual(0, result.ExitCode);
    }

    [TestMethod]
    public void ToolchainExecutor_ExtractVersion_HandlesVariousFormats()
    {
        Assert.AreEqual("10.0.100", ToolchainExecutor.ExtractVersion("dotnet 10.0.100"));
        Assert.AreEqual("1.82.0", ToolchainExecutor.ExtractVersion("rustc 1.82.0 (f6e511eec 2024-10-15)"));
        Assert.AreEqual("20.11.1", ToolchainExecutor.ExtractVersion("v20.11.1"));
        Assert.AreEqual("3.12.2", ToolchainExecutor.ExtractVersion("Python 3.12.2"));
    }

    private class TestDotNetToolCheck : BaseToolchainCheck
    {
        public override string ToolName => "Test .NET";
        public override string Command => "dotnet";
        public override string? SdkId => KnownSdk.DotNet;
    }

    [TestMethod]
    public async Task BaseToolchainCheck_GeneratesReport_WithResolvedPath()
    {
        var check = new TestDotNetToolCheck();
        var report = await check.CheckAsync();

        Assert.AreEqual("Test .NET", report.ToolName);
        Assert.AreEqual(ToolchainStatus.Available, report.Status);
        Assert.IsNotNull(report.DetectedVersion);
        Assert.IsNotNull(report.Path);
        Assert.IsTrue(File.Exists(report.Path));
        Assert.AreEqual(KnownSdk.DotNet, report.SdkId);
    }

    [TestMethod]
    public async Task SdkAutoDetector_DetectsInstalledSdks()
    {
        var sdks = await SdkAutoDetector.DetectAllSdksAsync();

        Assert.IsNotNull(sdks);
        Assert.IsNotEmpty(sdks, "Expected at least one installed SDK to be detected on the system.");

        var dotnetSdk = sdks.FirstOrDefault(s => s.SdkId == KnownSdk.DotNet);
        Assert.IsNotNull(dotnetSdk, "Expected .NET SDK to be discovered.");
        Assert.IsNotNull(dotnetSdk.Version);
        Assert.IsNotNull(dotnetSdk.RootDirectory);
        Assert.IsTrue(Directory.Exists(dotnetSdk.RootDirectory), $"RootDirectory {dotnetSdk.RootDirectory} does not exist");
        Assert.IsTrue(File.Exists(dotnetSdk.MainExecutablePath), $"MainExecutablePath {dotnetSdk.MainExecutablePath} does not exist");
    }

    [TestMethod]
    public async Task ToolchainManager_ScanSdksAsync_PopulatesDetectedSdks()
    {
        var manager = new ToolchainManager();
        var sdks = await manager.ScanSdksAsync();

        Assert.IsNotNull(sdks);
        Assert.HasCount(sdks.Count, manager.DetectedSdks);
        Assert.IsTrue(manager.DetectedSdks.Any(s => s.SdkId == KnownSdk.DotNet));
    }
}
