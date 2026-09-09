using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Manages a running interactive shell terminal process, handling standard I/O streams and process termination.
/// </summary>
public class TerminalSession : IDisposable
{
    private static readonly Regex AnsiEscapeRegex = new(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled);

    private Process? _process;
    private bool _isDisposed;

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; }
    public ShellInfo Shell { get; }
    public string WorkingDirectory { get; }

    public bool IsRunning => _process != null && !_process.HasExited;
    public int? ExitCode => _process is { HasExited: true } ? _process.ExitCode : null;

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
    /// Launches the shell process and connects standard streams.
    /// </summary>
    public void Start()
    {
        if (_process != null) return;

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
            if (e.Data != null)
            {
                var clean = CleanAnsi(e.Data);
                OutputReceived?.Invoke(clean + "\n");
            }
        };

        _process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                var clean = CleanAnsi(e.Data);
                OutputReceived?.Invoke(clean + "\n");
            }
        };

        _process.Exited += (s, e) =>
        {
            var code = _process?.ExitCode ?? 0;
            ProcessExited?.Invoke(code);
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Sends a command or text line to the shell's standard input.
    /// </summary>
    public void SendInput(string input)
    {
        if (_process == null || _process.HasExited || _isDisposed) return;

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
    /// Sends a break signal / Ctrl+C character to standard input.
    /// </summary>
    public void SendCtrlC()
    {
        if (_process == null || _process.HasExited || _isDisposed) return;

        try
        {
            _process.StandardInput.Write("\x03");
            _process.StandardInput.Flush();
        }
        catch
        {
            // Ignore
        }
    }

    private static string CleanAnsi(string text)
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

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Process already exited
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        GC.SuppressFinalize(this);
    }
}
