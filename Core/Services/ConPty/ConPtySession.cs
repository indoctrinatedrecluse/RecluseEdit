using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RecluseEdit.Core.Services.ConPty;

/// <summary>
/// Encapsulates a running Windows PseudoConsole (ConPTY) session, connecting standard I/O pipes
/// to an interactive child shell process.
/// </summary>
public class ConPtySession : IDisposable
{
    private IntPtr _hPC = IntPtr.Zero;
    private SafeFileHandle? _pipeInRead;
    private SafeFileHandle? _pipeOutWrite;
    private ConPtyNative.PROCESS_INFORMATION _processInfo;
    private FileStream? _inWriter;
    private FileStream? _outReader;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private Task? _exitMonitorTask;
    private bool _isDisposed;

    public int ProcessId => _processInfo.dwProcessId;
    public bool IsRunning => _processInfo.hProcess != IntPtr.Zero && !HasExited();

    public event Action<string>? OutputReceived;
    public event Action<int>? ProcessExited;

    public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                                      Environment.OSVersion.Version.Major >= 10 &&
                                      Environment.OSVersion.Version.Build >= 17763;

    /// <summary>
    /// Starts a child process attached to a new Windows PseudoConsole with the specified dimensions.
    /// </summary>
    public static ConPtySession Start(
        string commandLine,
        string? workingDirectory = null,
        short cols = 80,
        short rows = 25,
        Action<string>? onOutput = null,
        Action<int>? onExited = null)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("Windows ConPTY requires Windows 10 (Build 17763) or newer.");
        }

        var session = new ConPtySession();
        if (onOutput != null) session.OutputReceived += onOutput;
        if (onExited != null) session.ProcessExited += onExited;

        session.Initialize(commandLine, workingDirectory, cols, rows);
        return session;
    }

    private void Initialize(string commandLine, string? workingDirectory, short cols, short rows)
    {
        // 1. Create Pipes
        // Pipe In: Parent writes to pipeInWrite -> ConPTY reads from pipeInRead
        if (!ConPtyNative.CreatePipe(out _pipeInRead, out var pipeInWrite, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException($"Failed to create ConPTY input pipe: {Marshal.GetLastWin32Error()}");
        }

        // Pipe Out: ConPTY writes to pipeOutWrite -> Parent reads from pipeOutRead
        if (!ConPtyNative.CreatePipe(out var pipeOutRead, out _pipeOutWrite, IntPtr.Zero, 0))
        {
            _pipeInRead?.Dispose();
            pipeInWrite.Dispose();
            throw new InvalidOperationException($"Failed to create ConPTY output pipe: {Marshal.GetLastWin32Error()}");
        }

        // 2. Create PseudoConsole
        var size = new ConPtyNative.COORD { X = cols > 0 ? cols : (short)80, Y = rows > 0 ? rows : (short)25 };
        var hr = ConPtyNative.CreatePseudoConsole(size, _pipeInRead, _pipeOutWrite, 0, out _hPC);

        if (hr != 0)
        {
            _pipeInRead?.Dispose();
            _pipeOutWrite?.Dispose();
            pipeInWrite.Dispose();
            pipeOutRead.Dispose();
            throw new InvalidOperationException($"CreatePseudoConsole failed with HRESULT 0x{hr:X8}");
        }

        // 3. Prepare STARTUPINFOEX with AttributeList
        var lpSize = IntPtr.Zero;
        ConPtyNative.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);

        var lpAttributeList = Marshal.AllocHGlobal(lpSize);
        if (!ConPtyNative.InitializeProcThreadAttributeList(lpAttributeList, 1, 0, ref lpSize))
        {
            Marshal.FreeHGlobal(lpAttributeList);
            pipeInWrite.Dispose();
            pipeOutRead.Dispose();
            ConPtyNative.ClosePseudoConsole(_hPC);
            throw new InvalidOperationException($"InitializeProcThreadAttributeList failed: {Marshal.GetLastWin32Error()}");
        }

        if (!ConPtyNative.UpdateProcThreadAttribute(
            lpAttributeList,
            0,
            ConPtyNative.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
            _hPC,
            (IntPtr)IntPtr.Size,
            IntPtr.Zero,
            IntPtr.Zero))
        {
            ConPtyNative.DeleteProcThreadAttributeList(lpAttributeList);
            Marshal.FreeHGlobal(lpAttributeList);
            pipeInWrite.Dispose();
            pipeOutRead.Dispose();
            ConPtyNative.ClosePseudoConsole(_hPC);
            throw new InvalidOperationException($"UpdateProcThreadAttribute failed: {Marshal.GetLastWin32Error()}");
        }

        var siex = new ConPtyNative.STARTUPINFOEX();
        siex.StartupInfo.cb = Marshal.SizeOf<ConPtyNative.STARTUPINFOEX>();
        siex.lpAttributeList = lpAttributeList;

        var workDir = !string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory)
            ? workingDirectory
            : null;

        var created = ConPtyNative.CreateProcess(
            null,
            commandLine,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            ConPtyNative.EXTENDED_STARTUPINFO_PRESENT,
            IntPtr.Zero,
            workDir,
            ref siex,
            out _processInfo);

        var createError = Marshal.GetLastWin32Error();

        // Attribute list can be cleaned up after CreateProcess
        ConPtyNative.DeleteProcThreadAttributeList(lpAttributeList);
        Marshal.FreeHGlobal(lpAttributeList);

        if (!created)
        {
            pipeInWrite.Dispose();
            pipeOutRead.Dispose();
            ConPtyNative.ClosePseudoConsole(_hPC);
            throw new InvalidOperationException($"Failed to create child process for ConPTY: {createError}");
        }

        // Close thread handle; we only need process handle
        if (_processInfo.hThread != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_processInfo.hThread);
            _processInfo.hThread = IntPtr.Zero;
        }

        _inWriter = new FileStream(pipeInWrite, FileAccess.Write, 4096, false);
        _outReader = new FileStream(pipeOutRead, FileAccess.Read, 4096, false);
        _readCts = new CancellationTokenSource();

        // 4. Start background output pump
        _readTask = Task.Run(() => ReadOutputLoop(_readCts.Token));

        // 5. Start background process exit monitor
        _exitMonitorTask = Task.Run(MonitorExitLoop);
    }

    private void ReadOutputLoop(CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && _outReader != null)
            {
                var bytesRead = _outReader.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break;

                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OutputReceived?.Invoke(text);
            }
        }
        catch
        {
            // Stream closed or canceled during shutdown
        }
    }

    private void MonitorExitLoop()
    {
        if (_processInfo.hProcess == IntPtr.Zero) return;

        try
        {
            ConPtyNative.WaitForSingleObject(_processInfo.hProcess, ConPtyNative.INFINITE);

            var code = 0;
            if (ConPtyNative.GetExitCodeProcess(_processInfo.hProcess, out var exitCode))
            {
                code = (int)exitCode;
            }

            if (!_isDisposed)
            {
                ProcessExited?.Invoke(code);
            }
        }
        catch
        {
            // Ignore on dispose
        }
    }

    public void SendRawInput(string data)
    {
        if (string.IsNullOrEmpty(data) || _inWriter == null || _isDisposed) return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            lock (_inWriter)
            {
                _inWriter.Write(bytes, 0, bytes.Length);
                _inWriter.Flush();
            }
        }
        catch
        {
            // Stream closed
        }
    }

    public void Resize(short cols, short rows)
    {
        if (_hPC == IntPtr.Zero || _isDisposed) return;

        if (cols > 0 && rows > 0)
        {
            var size = new ConPtyNative.COORD { X = cols, Y = rows };
            ConPtyNative.ResizePseudoConsole(_hPC, size);
        }
    }

    private bool HasExited()
    {
        if (_processInfo.hProcess == IntPtr.Zero) return true;
        if (ConPtyNative.GetExitCodeProcess(_processInfo.hProcess, out var exitCode))
        {
            return exitCode != 259; // STILL_ACTIVE = 259
        }
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        OutputReceived = null;
        ProcessExited = null;

        _readCts?.Cancel();

        try
        {
            _inWriter?.Dispose();
        }
        catch { }

        try
        {
            _outReader?.Dispose();
        }
        catch { }

        if (_hPC != IntPtr.Zero)
        {
            try
            {
                ConPtyNative.ClosePseudoConsole(_hPC);
            }
            catch { }
            _hPC = IntPtr.Zero;
        }

        try { _pipeInRead?.Dispose(); } catch { }
        try { _pipeOutWrite?.Dispose(); } catch { }
        _pipeInRead = null;
        _pipeOutWrite = null;

        if (_processInfo.hProcess != IntPtr.Zero)
        {
            try
            {
                if (!HasExited())
                {
                    ConPtyNative.TerminateProcess(_processInfo.hProcess, 1);
                }
                ConPtyNative.CloseHandle(_processInfo.hProcess);
            }
            catch { }
            _processInfo.hProcess = IntPtr.Zero;
        }

        _readCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

