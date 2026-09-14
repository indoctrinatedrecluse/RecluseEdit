using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RecluseEdit.Sdk.Toolchains;

/// <summary>
/// High-performance, cross-platform path resolver for compilers, runtimes, and developer SDK utilities.
/// Supports multi-tier resolution across workspace-local binaries, system PATH, environment variable roots,
/// package managers (Scoop, Chocolatey, WinGet), and well-known installation locations.
/// </summary>
public static class SdkPathResolver
{
    private static readonly ConcurrentDictionary<string, string?> PathCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] WindowsExecutableExtensions = [".exe", ".cmd", ".bat", ".ps1", ""];
    private static readonly string[] UnixExecutableExtensions = [""];

    private static string[] ExecutableExtensions =>
        OperatingSystem.IsWindows() ? WindowsExecutableExtensions : UnixExecutableExtensions;

    /// <summary>
    /// Clears the internal executable resolution cache.
    /// </summary>
    public static void ClearCache() => PathCache.Clear();

    /// <summary>
    /// Resolves the absolute path to an executable binary using multi-tier heuristics.
    /// </summary>
    /// <param name="toolCommand">The binary or command name (e.g. "dotnet", "rustc", "go", "python", "vite").</param>
    /// <param name="workspaceRoot">Optional active workspace directory to probe for local package/venv binaries.</param>
    /// <param name="additionalSearchDirs">Optional additional directories to prioritize.</param>
    /// <returns>The resolved absolute path on disk, or null if not found.</returns>
    public static string? ResolveExecutable(
        string toolCommand,
        string? workspaceRoot = null,
        IEnumerable<string>? additionalSearchDirs = null)
    {
        if (string.IsNullOrWhiteSpace(toolCommand))
            return null;

        var trimmed = toolCommand.Trim('"', '\'', ' ');

        // 1. Direct file path check
        if (Path.IsPathRooted(trimmed) || trimmed.Contains(Path.DirectorySeparatorChar) || trimmed.Contains(Path.AltDirectorySeparatorChar))
        {
            if (File.Exists(trimmed))
                return Path.GetFullPath(trimmed);

            // Try appending executable extensions if none provided
            if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(Path.GetExtension(trimmed)))
            {
                foreach (var ext in WindowsExecutableExtensions)
                {
                    if (string.IsNullOrEmpty(ext)) continue;
                    var withExt = trimmed + ext;
                    if (File.Exists(withExt))
                        return Path.GetFullPath(withExt);
                }
            }
            return null;
        }

        // Cache key includes workspace root if provided
        var cacheKey = string.IsNullOrEmpty(workspaceRoot) ? trimmed : $"{trimmed}@{workspaceRoot}";
        if (additionalSearchDirs == null && PathCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var resolved = ResolveExecutableCore(trimmed, workspaceRoot, additionalSearchDirs);

        if (additionalSearchDirs == null)
            PathCache[cacheKey] = resolved;

        return resolved;
    }

    private static string? ResolveExecutableCore(
        string tool,
        string? workspaceRoot,
        IEnumerable<string>? additionalSearchDirs)
    {
        // 2. Additional search directories
        if (additionalSearchDirs != null)
        {
            foreach (var dir in additionalSearchDirs)
            {
                var found = ProbeDirectoryForBinary(dir, tool);
                if (found != null) return found;
            }
        }

        // 3. Workspace-local binaries
        if (!string.IsNullOrEmpty(workspaceRoot) && Directory.Exists(workspaceRoot))
        {
            var localBin = ProbeWorkspaceLocalBinaries(workspaceRoot, tool);
            if (localBin != null) return localBin;
        }

        // 4. System PATH resolution
        var inPath = ProbeSystemPath(tool);
        if (inPath != null) return inPath;

        // 5. Tool-specific Environment Variables
        var inEnv = ProbeEnvironmentVariables(tool);
        if (inEnv != null) return inEnv;

        // 6. Well-known standard installation paths
        var inStandard = ProbeStandardLocations(tool);
        if (inStandard != null) return inStandard;

        return null;
    }

    /// <summary>
    /// Probes workspace directories for local node_modules, python venv, and cargo build binaries.
    /// </summary>
    public static string? ProbeWorkspaceLocalBinaries(string workspaceRoot, string tool)
    {
        try
        {
            // Node / Frontend: ./node_modules/.bin/<tool>
            var nodeBin = Path.Combine(workspaceRoot, "node_modules", ".bin");
            if (Directory.Exists(nodeBin))
            {
                var match = ProbeDirectoryForBinary(nodeBin, tool);
                if (match != null) return match;
            }

            // PHP Composer: ./vendor/bin/<tool>
            var composerBin = Path.Combine(workspaceRoot, "vendor", "bin");
            if (Directory.Exists(composerBin))
            {
                var match = ProbeDirectoryForBinary(composerBin, tool);
                if (match != null) return match;
            }

            // Python Virtual Environments: .venv or venv
            string[] venvNames = [".venv", "venv", "env", ".env"];
            foreach (var venv in venvNames)
            {
                var venvPath = Path.Combine(workspaceRoot, venv);
                if (!Directory.Exists(venvPath)) continue;

                var scriptsDir = OperatingSystem.IsWindows()
                    ? Path.Combine(venvPath, "Scripts")
                    : Path.Combine(venvPath, "bin");

                if (Directory.Exists(scriptsDir))
                {
                    var match = ProbeDirectoryForBinary(scriptsDir, tool);
                    if (match != null) return match;
                }
            }

            // Rust: ./target/debug or ./target/release
            var targetDebug = Path.Combine(workspaceRoot, "target", "debug");
            var targetRelease = Path.Combine(workspaceRoot, "target", "release");
            if (Directory.Exists(targetRelease))
            {
                var match = ProbeDirectoryForBinary(targetRelease, tool);
                if (match != null) return match;
            }
            if (Directory.Exists(targetDebug))
            {
                var match = ProbeDirectoryForBinary(targetDebug, tool);
                if (match != null) return match;
            }
        }
        catch
        {
            // Ignore workspace access errors
        }

        return null;
    }

    /// <summary>
    /// Searches directories listed in the PATH environment variable.
    /// </summary>
    public static string? ProbeSystemPath(string tool)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var dirs = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var dir in dirs)
        {
            var cleanDir = dir.Trim('"', ' ');
            if (string.IsNullOrEmpty(cleanDir) || !Directory.Exists(cleanDir))
                continue;

            var match = ProbeDirectoryForBinary(cleanDir, tool);
            if (match != null) return match;
        }

        return null;
    }

    /// <summary>
    /// Probes tool-specific environment variables for installation roots.
    /// </summary>
    public static string? ProbeEnvironmentVariables(string tool)
    {
        var lower = tool.ToLowerInvariant();

        var envVarCandidates = new List<string>();

        switch (lower)
        {
            case "dotnet":
                envVarCandidates.AddRange(["DOTNET_ROOT", "DOTNET_ROOT(x86)", "DOTNET_HOME"]);
                break;
            case "java" or "javac" or "jar":
                envVarCandidates.AddRange(["JAVA_HOME", "JDK_HOME", "JRE_HOME"]);
                break;
            case "mvn":
                envVarCandidates.AddRange(["MAVEN_HOME", "M2_HOME"]);
                break;
            case "gradle":
                envVarCandidates.AddRange(["GRADLE_HOME"]);
                break;
            case "go" or "golangci-lint":
                envVarCandidates.AddRange(["GOROOT", "GOPATH"]);
                break;
            case "rustc" or "cargo" or "trunk" or "wasm-pack":
                envVarCandidates.AddRange(["CARGO_HOME", "RUSTUP_HOME"]);
                break;
            case "flutter" or "dart":
                envVarCandidates.AddRange(["FLUTTER_ROOT", "DART_SDK"]);
                break;
            case "python" or "python3" or "py" or "pip" or "django-admin":
                envVarCandidates.AddRange(["PYTHONHOME", "VIRTUAL_ENV"]);
                break;
            case "node" or "npm" or "npx":
                envVarCandidates.AddRange(["NODE_PATH", "NVM_BIN"]);
                break;
            case "pnpm":
                envVarCandidates.AddRange(["PNPM_HOME"]);
                break;
            case "bun":
                envVarCandidates.AddRange(["BUN_INSTALL"]);
                break;
            case "elixir" or "mix" or "iex":
                envVarCandidates.AddRange(["ELIXIR_HOME"]);
                break;
            case "clang" or "clang++":
                envVarCandidates.AddRange(["LLVM_HOME"]);
                break;
        }

        foreach (var envVar in envVarCandidates)
        {
            var val = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrWhiteSpace(val)) continue;

            var clean = val.Trim('"', ' ');
            if (!Directory.Exists(clean)) continue;

            // Direct directory
            var match = ProbeDirectoryForBinary(clean, tool);
            if (match != null) return match;

            // bin subdirectory
            var binSubdir = Path.Combine(clean, "bin");
            if (Directory.Exists(binSubdir))
            {
                match = ProbeDirectoryForBinary(binSubdir, tool);
                if (match != null) return match;
            }

            // GOPATH bin
            if (envVar.Equals("GOPATH", StringComparison.OrdinalIgnoreCase))
            {
                var gopathBin = Path.Combine(clean, "bin");
                if (Directory.Exists(gopathBin))
                {
                    match = ProbeDirectoryForBinary(gopathBin, tool);
                    if (match != null) return match;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Probes well-known standard OS installation folders for the specified tool.
    /// </summary>
    public static string? ProbeStandardLocations(string tool)
    {
        if (!OperatingSystem.IsWindows())
            return ProbeUnixStandardLocations(tool);

        return ProbeWindowsStandardLocations(tool);
    }

    private static string? ProbeWindowsStandardLocations(string tool)
    {
        var lower = tool.ToLowerInvariant();
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programData = Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData";
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";

        var probeDirs = new List<string>();

        switch (lower)
        {
            case "dotnet":
                probeDirs.Add(Path.Combine(progFiles, "dotnet"));
                probeDirs.Add(Path.Combine(progFilesX86, "dotnet"));
                probeDirs.Add(Path.Combine(userProfile, ".dotnet", "tools"));
                break;

            case "node" or "npm" or "npx":
                probeDirs.Add(Path.Combine(progFiles, "nodejs"));
                probeDirs.Add(Path.Combine(appData, "npm"));
                probeDirs.Add(Path.Combine(localAppData, "pnpm"));
                probeDirs.Add(Path.Combine(userProfile, ".bun", "bin"));
                break;

            case "pnpm":
                probeDirs.Add(Path.Combine(localAppData, "pnpm"));
                probeDirs.Add(Path.Combine(appData, "npm"));
                break;

            case "bun":
                probeDirs.Add(Path.Combine(userProfile, ".bun", "bin"));
                break;

            case "rustc" or "cargo" or "trunk" or "wasm-pack":
                probeDirs.Add(Path.Combine(userProfile, ".cargo", "bin"));
                break;

            case "go" or "golangci-lint":
                probeDirs.Add(Path.Combine(progFiles, "Go", "bin"));
                probeDirs.Add(Path.Combine(userProfile, "go", "bin"));
                break;

            case "python" or "python3" or "pip":
                probeDirs.Add(Path.Combine(localAppData, "Microsoft", "WindowsApps"));
                probeDirs.Add(systemRoot);
                // Scan LocalAppData/Programs/Python
                var pyRoot = Path.Combine(localAppData, "Programs", "Python");
                if (Directory.Exists(pyRoot))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(pyRoot, "Python*"))
                        {
                            probeDirs.Add(dir);
                            probeDirs.Add(Path.Combine(dir, "Scripts"));
                        }
                    }
                    catch { }
                }
                // Root C:\Python*
                try
                {
                    var drive = Path.GetPathRoot(progFiles) ?? "C:\\";
                    foreach (var dir in Directory.GetDirectories(drive, "Python*"))
                    {
                        probeDirs.Add(dir);
                        probeDirs.Add(Path.Combine(dir, "Scripts"));
                    }
                }
                catch { }
                break;

            case "flutter" or "dart":
                probeDirs.Add(@"C:\flutter\bin");
                probeDirs.Add(@"C:\src\flutter\bin");
                probeDirs.Add(Path.Combine(userProfile, "fvm", "default", "bin"));
                probeDirs.Add(Path.Combine(localAppData, "Pub", "Cache", "bin"));
                break;

            case "java" or "javac" or "jar":
                // Standard JDK locations
                probeDirs.Add(@"C:\Program Files\Common Files\Oracle\Java\javapath");
                ProbeJdkDirectories(progFiles, probeDirs);
                break;

            case "mvn":
                probeDirs.Add(Path.Combine(progFiles, "Apache", "maven", "bin"));
                probeDirs.Add(Path.Combine(programData, "chocolatey", "lib", "maven", "bin"));
                break;

            case "gradle":
                probeDirs.Add(Path.Combine(progFiles, "Gradle", "bin"));
                probeDirs.Add(Path.Combine(userProfile, ".gradle", "wrapper", "dists"));
                break;

            case "php" or "composer":
                probeDirs.Add(@"C:\php");
                probeDirs.Add(@"C:\tools\php");
                probeDirs.Add(Path.Combine(programData, "ComposerSetup", "bin"));
                probeDirs.Add(Path.Combine(appData, "Composer", "vendor", "bin"));
                break;

            case "ruby" or "gem" or "bundle" or "rails":
                try
                {
                    var drive = Path.GetPathRoot(progFiles) ?? "C:\\";
                    foreach (var dir in Directory.GetDirectories(drive, "Ruby*"))
                    {
                        probeDirs.Add(Path.Combine(dir, "bin"));
                    }
                    foreach (var dir in Directory.GetDirectories(Path.Combine(drive, "tools"), "ruby*"))
                    {
                        probeDirs.Add(Path.Combine(dir, "bin"));
                    }
                }
                catch { }
                break;

            case "elixir" or "mix" or "iex":
                probeDirs.Add(Path.Combine(progFiles, "Elixir", "bin"));
                probeDirs.Add(Path.Combine(userProfile, ".mix", "escripts"));
                break;

            case "git" or "bash" or "curl":
                probeDirs.Add(Path.Combine(progFiles, "Git", "cmd"));
                probeDirs.Add(Path.Combine(progFiles, "Git", "bin"));
                probeDirs.Add(Path.Combine(progFiles, "Git", "usr", "bin"));
                break;

            case "clang" or "clang++":
                probeDirs.Add(Path.Combine(progFiles, "LLVM", "bin"));
                probeDirs.Add(@"C:\msys64\mingw64\bin");
                probeDirs.Add(@"C:\msys64\ucrt64\bin");
                break;

            case "cl": // MSVC compiler
                var clPath = ProbeMsvcCompiler(progFilesX86, progFiles);
                if (clPath != null) return clPath;
                break;
        }

        // Package managers global shims
        probeDirs.Add(Path.Combine(userProfile, "scoop", "shims"));
        probeDirs.Add(Path.Combine(programData, "chocolatey", "bin"));

        foreach (var dir in probeDirs)
        {
            if (!Directory.Exists(dir)) continue;
            var match = ProbeDirectoryForBinary(dir, tool);
            if (match != null) return match;
        }

        return null;
    }

    private static void ProbeJdkDirectories(string progFiles, List<string> probeDirs)
    {
        string[] jdkContainers =
        [
            Path.Combine(progFiles, "Java"),
            Path.Combine(progFiles, "Eclipse Adoptium"),
            Path.Combine(progFiles, "Microsoft"),
            Path.Combine(progFiles, "Amazon Corretto"),
            Path.Combine(progFiles, "Zulu")
        ];

        foreach (var container in jdkContainers)
        {
            if (!Directory.Exists(container)) continue;
            try
            {
                foreach (var dir in Directory.GetDirectories(container, "*jdk*"))
                {
                    probeDirs.Add(Path.Combine(dir, "bin"));
                }
            }
            catch { }
        }
    }

    private static string? ProbeMsvcCompiler(string progFilesX86, string progFiles)
    {
        // 1. Try vswhere.exe
        var vswhere = Path.Combine(progFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (File.Exists(vswhere))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = vswhere,
                    Arguments = "-latest -products * -property installationPath",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    var vsPath = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();
                    if (!string.IsNullOrEmpty(vsPath) && Directory.Exists(vsPath))
                    {
                        var msvcRoot = Path.Combine(vsPath, "VC", "Tools", "MSVC");
                        if (Directory.Exists(msvcRoot))
                        {
                            var latestVersion = Directory.GetDirectories(msvcRoot).OrderByDescending(d => d).FirstOrDefault();
                            if (latestVersion != null)
                            {
                                var clCandidate = Path.Combine(latestVersion, "bin", "Hostx64", "x64", "cl.exe");
                                if (File.Exists(clCandidate)) return clCandidate;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    private static string? ProbeUnixStandardLocations(string tool)
    {
        string[] unixDirs =
        [
            "/usr/local/bin",
            "/usr/bin",
            "/bin",
            "/opt/homebrew/bin",
            "/usr/local/go/bin",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools")
        ];

        foreach (var dir in unixDirs)
        {
            if (!Directory.Exists(dir)) continue;
            var match = ProbeDirectoryForBinary(dir, tool);
            if (match != null) return match;
        }

        return null;
    }

    private static string? ProbeDirectoryForBinary(string dir, string tool)
    {
        foreach (var ext in ExecutableExtensions)
        {
            var fileName = tool.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                ? tool
                : tool + ext;

            try
            {
                var full = Path.Combine(dir, fileName);
                if (File.Exists(full))
                    return Path.GetFullPath(full);
            }
            catch
            {
                // Ignore invalid path character exceptions
            }
        }

        return null;
    }
}
