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
    public void TestShellDetector_FindsAvailableShells()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();

        Assert.IsNotNull(shells);
        Assert.IsNotEmpty(shells);

        // Verify default shell is assigned
        var defaultShell = shells.FirstOrDefault(s => s.IsDefault);
        Assert.IsNotNull(defaultShell, "Expected one shell to be designated as default.");

        // Verify all detected shells exist on disk
        foreach (var shell in shells)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.Id));
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.DisplayName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(shell.ExecutablePath));
            Assert.IsTrue(File.Exists(shell.ExecutablePath), $"Shell executable must exist on disk: {shell.ExecutablePath}");
        }
    }

    [TestMethod]
    public void TestShellInfo_Properties()
    {
        var shell = new ShellInfo
        {
            Id = "powershell",
            DisplayName = "Windows PowerShell",
            ExecutablePath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            Arguments = "-NoLogo -NoExit",
            Icon = "⚡",
            IsDefault = true
        };

        Assert.AreEqual("powershell", shell.Id);
        Assert.AreEqual("Windows PowerShell", shell.DisplayName);
        Assert.AreEqual("-NoLogo -NoExit", shell.Arguments);
        Assert.AreEqual("⚡", shell.Icon);
        Assert.IsTrue(shell.IsDefault);
        Assert.AreEqual("⚡ Windows PowerShell", shell.ToString());
    }

    [TestMethod]
    public async Task TestTerminalSession_LaunchesAndExecutesCommand()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.FirstOrDefault(s => s.Id == "cmd") ?? shells.First();

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
    public async Task TestTerminalSession_ExitCommandTriggersProcessExited()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.FirstOrDefault(s => s.Id == "cmd") ?? shells.First();

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
    public void TestTerminalSession_CleanClose()
    {
        var detector = new ShellDetector();
        var shells = detector.DetectShells();
        var shell = shells.First();

        var session = new TerminalSession(shell, "Close Test", AppDomain.CurrentDomain.BaseDirectory);
        session.Start();
        Assert.IsTrue(session.IsRunning);

        session.Close();
        Assert.IsFalse(session.IsRunning);
    }
}
