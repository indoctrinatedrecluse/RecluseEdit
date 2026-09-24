using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services.ConPty;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages a running interactive shell terminal process, handling standard I/O streams,
/// Windows PseudoConsole (ConPTY) when supported, and process termination.
/// </summary>
public class TerminalSession : IDisposable
{
    private static readonly Regex AnsiEscapeRegex = new(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled);

    private Process? _process;
    private ConPtySession? _conPtySession;
    private bool _isDisposed;

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; }
    public ShellInfo Shell { get; }
    public string WorkingDirectory { get; }

    /// <summary>
    /// Gets or sets whether to use Windows PseudoConsole (ConPTY) if available.
    /// </summary>
    public bool UseConPty { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to strip ANSI escape codes before firing OutputReceived.
    /// When using xterm.js or modern VT emulators, set this to false.
    /// </summary>
    public bool StripAnsi { get; set; } = true;

    public bool IsConPtyActive => _conPtySession != null && _conPtySession.IsRunning;

    public bool IsRunning => _conPtySession?.IsRunning ?? (_process != null && !_process.HasExited);
    public int? ExitCode
    {
        get
        {
            if (_conPtySession != null)
            {
                return _conPtySession.IsRunning ? null : 0;
            }
            return _process is { HasExited: true } ? _process.ExitCode : null;
        }
    }

    public event Action<string>? OutputReceived;
    public event Action<int>? ProcessExited;

    public TerminalSession(ShellInfo shell, string title, string? workingDirectory = null)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        Title = title;
        WorkingDirectory = !string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory)
            ? workingDirectory
            : Environment.CurrentDirectory;
    }

    /// <summary>
    /// Launches the shell process using ConPTY if enabled, or standard stream redirection as fallback.
    /// </summary>
    public void Start(short initialCols = 80, short initialRows = 25)
    {
        if (_conPtySession != null || _process != null) return;

        if (UseConPty && ConPtySession.IsSupported)
        {
            try
            {
                var commandLine = string.IsNullOrWhiteSpace(Shell.Arguments)
                    ? $"\"{Shell.ExecutablePath}\""
                    : $"\"{Shell.ExecutablePath}\" {Shell.Arguments}";

                _conPtySession = ConPtySession.Start(commandLine, WorkingDirectory, initialCols, initialRows);

                _conPtySession.OutputReceived += text =>
                {
                    if (_isDisposed) return;
                    var content = StripAnsi ? CleanAnsi(text) : text;
                    OutputReceived?.Invoke(content);
                };

                _conPtySession.ProcessExited += code =>
                {
                    if (_isDisposed) return;
                    ProcessExited?.Invoke(code);
                };

                return;
            }
            catch
            {
                // Fallback to redirected pipes if ConPTY fails to initialize
                _conPtySession = null;
            }
        }

        StartRedirectedProcess();
    }

    private void StartRedirectedProcess()
    {
        var psi = new ProcessStartInfo
        {
            FileName = Shell.ExecutablePath,
            Arguments = Shell.Arguments,
            WorkingDirectory = WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.Environment["TERM"] = "xterm-256color";
        psi.Environment["COLORTERM"] = "truecolor";

        _process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null && !_isDisposed)
            {
                var content = StripAnsi ? CleanAnsi(e.Data) : e.Data;
                OutputReceived?.Invoke(content + "\n");
            }
        };

        _process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null && !_isDisposed)
            {
                var content = StripAnsi ? CleanAnsi(e.Data) : e.Data;
                OutputReceived?.Invoke(content + "\n");
            }
        };

        _process.Exited += (s, e) =>
        {
            if (!_isDisposed)
            {
                var code = 0;
                try { code = _process?.ExitCode ?? 0; } catch { }
                ProcessExited?.Invoke(code);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Sends a line of text terminated with a newline to standard input.
    /// </summary>
    public void SendInput(string input)
    {
        if (_isDisposed) return;

        if (_conPtySession != null)
        {
            _conPtySession.SendRawInput(input + "\r\n");
            return;
        }

        if (_process == null || _process.HasExited) return;

        try
        {
            _process.StandardInput.WriteLine(input);
            _process.StandardInput.Flush();
        }
        catch
        {
            // Ignore if stream closed during exit
        }
    }

    /// <summary>
    /// Sends raw keystrokes or escape sequences directly to standard input without appending a newline.
    /// </summary>
    public void SendRawInput(string data)
    {
        if (_isDisposed) return;

        if (_conPtySession != null)
        {
            _conPtySession.SendRawInput(data);
            return;
        }

        if (_process == null || _process.HasExited) return;

        try
        {
            _process.StandardInput.Write(data);
            _process.StandardInput.Flush();
        }
        catch
        {
            // Ignore
        }
    }

    /// <summary>
    /// Resizes the pseudo console dimensions if running under ConPTY.
    /// </summary>
    public void Resize(short cols, short rows)
    {
        _conPtySession?.Resize(cols, rows);
    }

    /// <summary>
    /// Sends a break signal / Ctrl+C character to standard input.
    /// </summary>
    public void SendCtrlC()
    {
        SendRawInput("\x03");
    }

    public static string CleanAnsi(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return AnsiEscapeRegex.Replace(text, string.Empty);
    }

    public void Close()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        OutputReceived = null;
        ProcessExited = null;

        if (_conPtySession != null)
        {
            _conPtySession.Dispose();
            _conPtySession = null;
        }

        var proc = _process;
        _process = null;

        if (proc != null)
        {
            try { proc.CancelOutputRead(); } catch { }
            try { proc.CancelErrorRead(); } catch { }
            try { proc.StandardInput.Close(); } catch { }

            Task.Run(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(1000);
                    }
                }
                catch { }
                finally
                {
                    try { proc.Dispose(); } catch { }
                }
            });
        }

        GC.SuppressFinalize(this);
    }
}
