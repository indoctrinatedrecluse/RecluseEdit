using System.Diagnostics;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Python.Toolchains;

/// <summary>
/// Verifies the presence and version of the Python runtime interpreter on the system PATH.
/// </summary>
public class PythonToolchainCheck : IToolchainCheck
{
    public string ToolName => "Python";
    public string Command => "python";
    public string? RequiredVersion => ">= 3.10.0";
    public string? InstallHelp => "Download Python from https://www.python.org or install via winget: 'winget install Python.Python.3.12'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        // Check "python -V", fallback to "python3 -V" if python is not found
        var (success, output, path) = await ExecuteCommandAsync("python", "-V", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            (success, output, path) = await ExecuteCommandAsync("python3", "-V", cancellationToken);
        }

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Python was not detected on system PATH. Install Python to execute scripts, run Flask/Django apps, and use Streamlit/Gradio.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "Python 3.12.2"
        var versionMatch = Regex.Match(output, @"Python\s+(\d+\.\d+(\.\d+)?)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Python runtime detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }

    internal static async Task<(bool success, string output, string? path)> ExecuteCommandAsync(string cmd, string args, CancellationToken ct)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var fileName = isWindows ? "cmd.exe" : cmd;
            var arguments = isWindows ? $"/c {cmd} {args}" : args;

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) return (false, "", null);

            var stdout = await p.StandardOutput.ReadToEndAsync(ct);
            var stderr = await p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var outText = !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : stderr.Trim();
            return (p.ExitCode == 0, outText, cmd);
        }
        catch
        {
            return (false, "", null);
        }
    }
}

/// <summary>
/// Verifies the presence and version of the Pip package manager on the system PATH.
/// </summary>
public class PipToolchainCheck : IToolchainCheck
{
    public string ToolName => "Pip";
    public string Command => "pip";
    public string? RequiredVersion => ">= 23.0.0";
    public string? InstallHelp => "Pip is bundled with Python. You can also run: 'python -m ensurepip --upgrade'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await PythonToolchainCheck.ExecuteCommandAsync("pip", "-V", cancellationToken);
        if (!success || string.IsNullOrEmpty(output))
        {
            (success, output, path) = await PythonToolchainCheck.ExecuteCommandAsync("pip3", "-V", cancellationToken);
        }

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Pip package manager was not detected on system PATH. Required for installing Python frameworks and packages.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "pip 24.0 from C:\Python312\Lib\site-packages\pip (python 3.12)"
        var versionMatch = Regex.Match(output, @"pip\s+(\d+\.\d+(\.\d+)?)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Split('\n')[0].Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Pip detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}

/// <summary>
/// Verifies the presence and version of the Django CLI management utility on the system PATH.
/// </summary>
public class DjangoToolchainCheck : IToolchainCheck
{
    public string ToolName => "Django CLI";
    public string Command => "django-admin";
    public string? RequiredVersion => ">= 4.2.0";
    public string? InstallHelp => "Install Django via pip: 'pip install django' or 'python -m pip install django'";

    public async Task<ToolchainReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var (success, output, path) = await PythonToolchainCheck.ExecuteCommandAsync("django-admin", "--version", cancellationToken);

        if (!success || string.IsNullOrEmpty(output))
        {
            return new ToolchainReport
            {
                ToolName = ToolName,
                Command = Command,
                Status = ToolchainStatus.Warning,
                RequiredVersion = RequiredVersion,
                Description = "Django CLI (django-admin) was not detected on system PATH. Install Django via pip to start and manage projects.",
                InstallHelp = InstallHelp
            };
        }

        // Example output: "5.0.3" or "4.2.11"
        var versionMatch = Regex.Match(output, @"(\d+\.\d+(\.\d+)?)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : output.Trim();

        return new ToolchainReport
        {
            ToolName = ToolName,
            Command = Command,
            Status = ToolchainStatus.Available,
            DetectedVersion = version,
            RequiredVersion = RequiredVersion,
            Description = $"Django CLI detected ({version})",
            InstallHelp = InstallHelp,
            Path = path
        };
    }
}
