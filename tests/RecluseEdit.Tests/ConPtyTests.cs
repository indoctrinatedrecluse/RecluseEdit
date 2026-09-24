using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Core.Services.ConPty;

namespace RecluseEdit.Tests;

[TestClass]
public class ConPtyTests
{
    [TestMethod]
    public void ConPty_IsSupported_OnModernWindows()
    {
        Assert.IsTrue(ConPtySession.IsSupported, "ConPTY must be supported on modern Windows.");
    }

    [TestMethod]
    public void ConPtySession_LaunchesAndAssignsProcessId()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (!File.Exists(cmdPath)) return;

        using var session = ConPtySession.Start($"\"{cmdPath}\"", AppDomain.CurrentDomain.BaseDirectory, 80, 25);
        Assert.IsTrue(session.IsRunning);
        Assert.IsGreaterThan(0, session.ProcessId);

        session.SendRawInput("echo 123\r\n");
        Assert.IsTrue(session.IsRunning);
    }

    [TestMethod]
    public void ConPtySession_Resize_ExecutesWithoutError()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (!File.Exists(cmdPath)) return;

        using var session = ConPtySession.Start($"\"{cmdPath}\"", AppDomain.CurrentDomain.BaseDirectory, 80, 25);
        session.Resize(120, 40);
        session.Resize(80, 25);
        Assert.IsTrue(session.IsRunning);
    }

    [TestMethod]
    public void ConPtySession_Dispose_TerminatesCleanly()
    {
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var cmdPath = Path.Combine(system32, "cmd.exe");
        if (!File.Exists(cmdPath)) return;

        var session = ConPtySession.Start($"\"{cmdPath}\"", AppDomain.CurrentDomain.BaseDirectory, 80, 25);
        Assert.IsTrue(session.IsRunning);

        session.Dispose();
        Assert.IsFalse(session.IsRunning);
    }
}

