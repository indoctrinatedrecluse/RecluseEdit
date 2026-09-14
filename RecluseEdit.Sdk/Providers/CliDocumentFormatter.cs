using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Base class for external command-line interface (CLI) document formatters.
/// Automatically handles process invocation, standard input/output streaming, timeout protection, and exit-code validation.
/// </summary>
public abstract class CliDocumentFormatter : IDocumentFormatter
{
    private static readonly Dictionary<string, bool> AvailabilityCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    public abstract string FormatterId { get; }
    public abstract string DisplayName { get; }
    public abstract IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>
    /// The binary or script name to execute (e.g., "prettier", "black", "ruff", "gofmt", "rustfmt", "dart").
    /// </summary>
    public abstract string ExecutableName { get; }

    /// <summary>
    /// Command line arguments passed to the formatter.
    /// Can contain placeholders such as <c>{filePath}</c> or <c>{language}</c>.
    /// </summary>
    public virtual string ArgumentsPattern => string.Empty;

    /// <summary>
    /// Process timeout before cancelling formatting. Defaults to 5000 milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Formatter priority over built-in formatters when available.
    /// </summary>
    public bool PreferCliOverBuiltIn { get; set; } = true;

    /// <summary>
    /// Checks whether the CLI tool executable is installed and reachable in the current environment PATH.
    /// </summary>
    public virtual bool IsAvailable()
    {
        lock (CacheLock)
        {
            if (AvailabilityCache.TryGetValue(ExecutableName, out var cached))
                return cached;

            bool found = FindExecutableInPath(ExecutableName) != null;
            AvailabilityCache[ExecutableName] = found;
            return found;
        }
    }

    /// <summary>
    /// Clears the availability cache (e.g. after installing new SDKs or tools).
    /// </summary>
    public static void InvalidateAvailabilityCache()
    {
        lock (CacheLock)
        {
            AvailabilityCache.Clear();
        }
    }

    public virtual bool CanFormat(string language, string filePath)
    {
        var lang = language?.ToLowerInvariant() ?? string.Empty;
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        bool langMatches = SupportedLanguages.Any(sl =>
            string.Equals(sl, lang, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sl, ext, StringComparison.OrdinalIgnoreCase));

        return langMatches && IsAvailable();
    }

    public virtual string Format(string sourceCode, string language, FormattingOptions options)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return sourceCode;

        var exePath = FindExecutableInPath(ExecutableName) ?? ExecutableName;
        var args = FormatArguments(language, options);

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();

            // Asynchronously read stdout and stderr
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // Stream source code to stdin
            using (var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
            {
                writer.Write(sourceCode);
                writer.Flush();
            }

            if (!process.WaitForExit(TimeoutMilliseconds))
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException($"CLI Formatter '{DisplayName}' timed out after {TimeoutMilliseconds}ms.");
            }

            outputTask.Wait(1000);
            errorTask.Wait(1000);

            string output = outputTask.Result;
            string error = errorTask.Result;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"CLI Formatter '{DisplayName}' failed with exit code {process.ExitCode}: {error}");
            }

            return !string.IsNullOrEmpty(output) ? output : sourceCode;
        }
        catch (Exception ex) when (ex is not TimeoutException && ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"CLI Formatter '{DisplayName}' execution error: {ex.Message}", ex);
        }
    }

    protected virtual string FormatArguments(string language, FormattingOptions options)
    {
        return ArgumentsPattern
            .Replace("{language}", language ?? string.Empty);
    }

    public static string? FindExecutableInPath(string executable)
    {
        if (File.Exists(executable))
            return Path.GetFullPath(executable);

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var extensions = OperatingSystem.IsWindows()
            ? [".exe", ".cmd", ".bat", ""]
            : new[] { "" };

        foreach (var path in paths)
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(path, executable + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
