using System.IO;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Toolchains;

namespace RecluseEdit.Tests;

[TestClass]
public class ToolchainOptimizationTests
{
    [TestMethod]
    public async Task ToolchainExecutor_DoesNotSpawnProcess_ForMissingTool()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await ToolchainExecutor.ExecuteAsync("completely_nonexistent_cli_tool_9999", "--version");
        sw.Stop();

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.ResolvedPath);
        Assert.Contains("not found", result.Output);
        // Must short-circuit immediately without launching cmd.exe or waiting for OS process creation
        Assert.IsLessThan(500L, sw.ElapsedMilliseconds, $"Expected instant short-circuit (<500ms), but took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public async Task ToolchainExecutor_ResolvesSpaceSeparatedCommand()
    {
        var result = await ToolchainExecutor.ExecuteAsync("dotnet --version", "");
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.ResolvedPath);
    }

    [TestMethod]
    public async Task ToolchainCacheService_SaveAndLoad_RoundtripsSuccessfully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"toolchain_cache_test_{Guid.NewGuid():N}.json");
        try
        {
            var cacheService = new ToolchainCacheService(tempFile);

            var sampleReports = new List<ToolchainReport>
            {
                new()
                {
                    ToolName = "Mock Compiler",
                    Command = "mockc",
                    Status = ToolchainStatus.Available,
                    DetectedVersion = "1.0.0",
                    Path = @"C:\bin\mockc.exe",
                    SdkId = KnownSdk.DotNet
                }
            };

            var sampleSdks = new List<DetectedSdk>
            {
                new()
                {
                    SdkId = KnownSdk.DotNet,
                    DisplayName = ".NET SDK",
                    Version = "10.0.100",
                    RootDirectory = @"C:\Program Files\dotnet",
                    MainExecutablePath = @"C:\Program Files\dotnet\dotnet.exe",
                    Category = "Compiler & Runtime"
                }
            };

            await cacheService.SaveCacheAsync(sampleReports, sampleSdks);

            var loaded = cacheService.LoadCache();
            Assert.IsNotNull(loaded);
            Assert.HasCount(1, loaded.Reports);
            Assert.AreEqual("Mock Compiler", loaded.Reports[0].ToolName);
            Assert.AreEqual(ToolchainStatus.Available, loaded.Reports[0].Status);
            Assert.AreEqual("1.0.0", loaded.Reports[0].DetectedVersion);
            Assert.AreEqual(@"C:\bin\mockc.exe", loaded.Reports[0].Path);

            Assert.HasCount(1, loaded.DetectedSdks);
            Assert.AreEqual(KnownSdk.DotNet, loaded.DetectedSdks[0].SdkId);
            Assert.AreEqual("10.0.100", loaded.DetectedSdks[0].Version);
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    [TestMethod]
    public void ToolchainCacheService_IsCacheValid_ReturnsFalseWhenExpiredOrMissing()
    {
        var cacheService = new ToolchainCacheService();

        Assert.IsFalse(cacheService.IsCacheValid(null));

        var expiredCache = new ToolchainCacheData
        {
            Version = "6.0.0",
            LastScannedUtc = DateTime.UtcNow.AddHours(-25),
            Reports = [],
            DetectedSdks = []
        };
        Assert.IsFalse(cacheService.IsCacheValid(expiredCache, TimeSpan.FromHours(24)));

        var validCache = new ToolchainCacheData
        {
            Version = "6.0.0",
            LastScannedUtc = DateTime.UtcNow.AddMinutes(-5),
            Reports = [],
            DetectedSdks = []
        };
        Assert.IsTrue(cacheService.IsCacheValid(validCache, TimeSpan.FromHours(24)));
    }

    [TestMethod]
    public async Task ToolchainManager_InitializeWithCacheAsync_PopulatesReportsWithoutBlocking()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"toolchain_manager_cache_{Guid.NewGuid():N}.json");
        try
        {
            var cacheService = new ToolchainCacheService(tempFile);

            var sampleReports = new List<ToolchainReport>
            {
                new()
                {
                    ToolName = "Cached Tool",
                    Command = "cached",
                    Status = ToolchainStatus.Available,
                    DetectedVersion = "2.5.0",
                    Path = @"C:\bin\cached.exe"
                }
            };
            await cacheService.SaveCacheAsync(sampleReports, []);

            var manager = new ToolchainManager(cacheService);
            Assert.HasCount(0, manager.Reports);

            // Initialize from cache
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await manager.InitializeWithCacheAsync(forceRefresh: false);
            sw.Stop();

            // Must complete virtually instantly from cache (<100ms)
            Assert.IsLessThan(200L, sw.ElapsedMilliseconds, $"Cache initialization took too long: {sw.ElapsedMilliseconds}ms");
            Assert.HasCount(1, manager.Reports);
            Assert.AreEqual("Cached Tool", manager.Reports[0].ToolName);
            Assert.AreEqual(ToolchainStatus.Available, manager.Reports[0].Status);
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    [TestMethod]
    public void SdkPathResolver_ClearCache_ClearsInternalStateSafely()
    {
        var path = SdkPathResolver.ResolveExecutable("dotnet");
        Assert.IsNotNull(path);

        SdkPathResolver.ClearCache();

        var pathAfterClear = SdkPathResolver.ResolveExecutable("dotnet");
        Assert.IsNotNull(pathAfterClear);
        Assert.AreEqual(path, pathAfterClear);
    }

    [TestMethod]
    public void MainWindow_AssemblyVersion_MatchesReleaseVersion()
    {
        var version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3);
        Assert.AreEqual("6.0.0", version);
    }
}
