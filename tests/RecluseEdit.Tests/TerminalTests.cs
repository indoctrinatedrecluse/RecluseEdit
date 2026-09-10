using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class TerminalTests
{
    [TestMethod]
    public void TestShellDetector_FindsAvailableShellsAndRetainsCatalog()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();

        Assert.IsNotNull(shells);
        Assert.IsNotEmpty(shells);

        // Verify default shell is assigned and available
        var defaultShell = shells.FirstOrDefault(s => s.IsDefault);
        Assert.IsNotNull(defaultShell, "Expected one shell to be designated as default.");
        Assert.IsTrue(defaultShell.IsAvailable, "Default shell must be available.");

        // Verify all available shells exist on disk
        var availableShells = shells.Where(s => s.IsAvailable).ToList();
        Assert.IsNotEmpty(availableShells, "Expected at least one shell to be available on host.");

        foreach (var shell in availableShells)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.DisplayName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.ExecutablePath));
            Assert.IsTrue(File.Exists(shell.ExecutablePath), $"Shell executable must exist on disk: {shell.ExecutablePath}");
        }

        // Verify standard catalog IDs are represented
        var ids = shells.Select(s => s.Id).ToList();
        Assert.Contains("cmd", ids, "Catalog must include cmd.");
        Assert.Contains("powershell", ids, "Catalog must include powershell.");
    }

    [TestMethod]
    public void TestShellInfo_Properties()
    {
        var shell = new ShellInfo
        {
            Id = "powershell",
            DisplayName = "Windows PowerShell",
            ExecutablePath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            Arguments = "-NoLogo",
            Icon = "⚡",
            IsDefault = true,
            IsAvailable = true,
            ExpectedBinaryNames = ["powershell.exe", "pwsh.exe"]
        };

        Assert.AreEqual("powershell", shell.Id);
        Assert.AreEqual("Windows PowerShell", shell.DisplayName);
        Assert.AreEqual("-NoLogo", shell.Arguments);
        Assert.AreEqual("⚡", shell.Icon);
        Assert.IsTrue(shell.IsDefault);
        Assert.IsTrue(shell.IsAvailable);
        Assert.AreEqual("⚡ Windows PowerShell", shell.ToString());

        shell.IsAvailable = false;
        Assert.AreEqual("⚡ Windows PowerShell (not found)", shell.ToString());
    }

    [TestMethod]
    public void TestShellVerifier_RejectsNonExistentFile()
    {
        var shell = new ShellInfo
        {
            Id = "pwsh",
            DisplayName = "PowerShell 7",
            ExpectedBinaryNames = ["pwsh.exe"]
        };

        var result = ShellVerifier.Verify(shell, @"C:\NonExistentDirectory_12345\pwsh.exe");
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Message.Contains("File does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TestShellVerifier_RejectsMismatchedBinaryName()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (!File.Exists(cmdPath)) return;

        var shell = new ShellInfo
        {
            Id = "git-bash",
            DisplayName = "Git Bash",
            ExpectedBinaryNames = ["bash.exe", "sh.exe"]
        };

        var result = ShellVerifier.Verify(shell, cmdPath);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Message.Contains("does not match expected", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TestShellVerifier_AcceptsValidBinary()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (!File.Exists(cmdPath)) return;

        var shell = new ShellInfo
        {
            Id = "cmd",
            DisplayName = "Command Prompt",
            ExpectedBinaryNames = ["cmd.exe"]
        };

        var result = ShellVerifier.Verify(shell, cmdPath);
        Assert.IsTrue(result.IsValid, $"Expected cmd.exe to be verified, but got: {result.Message}");
    }

    [TestMethod]
    public void TestShellSettingsService_PersistsCustomPaths()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"recluse_test_settings_{Guid.NewGuid():N}.json");
        try
        {
            var service = new ShellSettingsService(tempFile);
            service.SetCustomPath("cygwin", @"C:\custom\bin\bash.exe");

            var loadedPath = service.GetCustomPath("cygwin");
            Assert.AreEqual(@"C:\custom\bin\bash.exe", loadedPath);

            // Reload from disk in a fresh instance
            var service2 = new ShellSettingsService(tempFile);
            Assert.AreEqual(@"C:\custom\bin\bash.exe", service2.GetCustomPath("cygwin"));

            service2.RemoveCustomPath("cygwin");
            Assert.IsNull(service2.GetCustomPath("cygwin"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
    public async Task TestTerminalSession_LaunchesAndExecutesCommand()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.FirstOrDefault(s => s.IsAvailable && s.Id == "cmd") ?? shells.First(s => s.IsAvailable);

        using var session = new TerminalSession(shell, "Test Session", AppDomain.CurrentDomain.BaseDirectory);
        var tcs = new TaskCompletionSource<string>();

        session.OutputReceived += text =>
        {
            if (text.Contains("TEST_TERMINAL_OUTPUT"))
            {
                tcs.TrySetResult(text);
            }
        };

        session.Start();
        Assert.IsTrue(session.IsRunning);

        session.SendInput("echo TEST_TERMINAL_OUTPUT");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var received = await tcs.Task;
        Assert.Contains("TEST_TERMINAL_OUTPUT", received);
    }

    [TestMethod]
    public async Task TestTerminalSession_CmdExitCommandTriggersProcessExited()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.FirstOrDefault(s => s.IsAvailable && s.Id == "cmd") ?? shells.First(s => s.IsAvailable);

        using var session = new TerminalSession(shell, "Exit Test Session", AppDomain.CurrentDomain.BaseDirectory);
        var tcs = new TaskCompletionSource<int>();

        session.ProcessExited += code =>
        {
            tcs.TrySetResult(code);
        };

        session.Start();
        Assert.IsTrue(session.IsRunning);

        session.SendInput("exit");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var exitCode = await tcs.Task;
        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(session.IsRunning);
    }

    [TestMethod]
    public async Task TestTerminalSession_PowerShellExitCommandTriggersCleanExitWithoutDeadlock()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var psShell = shells.FirstOrDefault(s => s.IsAvailable && (s.Id == "powershell" || s.Id == "pwsh"));
        if (psShell == null) return;

        using var session = new TerminalSession(psShell, "PS Exit Test", AppDomain.CurrentDomain.BaseDirectory);
        var tcs = new TaskCompletionSource<int>();

        session.ProcessExited += code =>
        {
            tcs.TrySetResult(code);
        };

        session.Start();
        Assert.IsTrue(session.IsRunning);

        // Send exit command
        session.SendInput("exit");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var exitCode = await tcs.Task;
        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(session.IsRunning);
    }

    [TestMethod]
    public void TestTerminalSession_CleanClose()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.First(s => s.IsAvailable);

        var session = new TerminalSession(shell, "Close Test", AppDomain.CurrentDomain.BaseDirectory);
        session.Start();
        Assert.IsTrue(session.IsRunning);

        session.Close();
        Assert.IsFalse(session.IsRunning);
    }
}
