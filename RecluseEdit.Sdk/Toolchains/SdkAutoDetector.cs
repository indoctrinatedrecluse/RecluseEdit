using System.Collections.Concurrent;
using System.IO;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Sdk.Toolchains;

/// <summary>
/// Automatic discovery engine for development kits, compilers, and language runtimes.
/// Probes system environments and standard installation trees to produce a catalog of installed SDKs.
/// </summary>
public static class SdkAutoDetector
{
    /// <summary>
    /// Asynchronously detects and catalogs all development kits and compiler suites installed on the machine.
    /// </summary>
    public static async Task<IReadOnlyList<DetectedSdk>> DetectAllSdksAsync(
        string? workspaceRoot = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task<DetectedSdk?>>
        {
            DetectDotNetAsync(workspaceRoot, cancellationToken),
            DetectNodeJsAsync(workspaceRoot, cancellationToken),
            DetectRustAsync(workspaceRoot, cancellationToken),
            DetectGoAsync(workspaceRoot, cancellationToken),
            DetectPythonAsync(workspaceRoot, cancellationToken),
            DetectJavaAsync(workspaceRoot, cancellationToken),
            DetectFlutterAsync(workspaceRoot, cancellationToken),
            DetectMsvcAsync(workspaceRoot, cancellationToken),
            DetectLlvmAsync(workspaceRoot, cancellationToken),
            DetectPhpAsync(workspaceRoot, cancellationToken),
            DetectRubyAsync(workspaceRoot, cancellationToken),
            DetectElixirAsync(workspaceRoot, cancellationToken),
            DetectGitAsync(workspaceRoot, cancellationToken)
        };

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Select(r => r!).ToList();
    }

    private static async Task<DetectedSdk?> DetectDotNetAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("dotnet", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("dotnet", "--version", workspaceRoot, cancellationToken: ct);
        var rootDir = Path.GetDirectoryName(exe) ?? exe;

        return new DetectedSdk
        {
            SdkId = KnownSdk.DotNet,
            DisplayName = ".NET SDK",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Compiler & Runtime",
            EnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ROOT") != null ? "DOTNET_ROOT" : null
        };
    }

    private static async Task<DetectedSdk?> DetectNodeJsAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("node", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("node", "--version", workspaceRoot, cancellationToken: ct);
        var rootDir = Path.GetDirectoryName(exe) ?? exe;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "npm", "npx", "corepack" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.NodeJs,
            DisplayName = "Node.js JavaScript Runtime",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "JavaScript Runtime & Package Manager",
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectRustAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("rustc", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("rustc", "--version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "cargo", "rustup", "rustfmt", "trunk", "wasm-pack" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Rust,
            DisplayName = "Rust Toolchain",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Systems Compiler & Package Manager",
            EnvironmentVariable = Environment.GetEnvironmentVariable("CARGO_HOME") != null ? "CARGO_HOME" : null,
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectGoAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("go", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("go", "version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "gofmt", "golangci-lint" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Go,
            DisplayName = "Go SDK",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Systems Compiler & Package Manager",
            EnvironmentVariable = Environment.GetEnvironmentVariable("GOROOT") != null ? "GOROOT" : null,
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectPythonAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("python", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("python", "--version", workspaceRoot, cancellationToken: ct);
        var rootDir = Path.GetDirectoryName(exe) ?? exe;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "pip", "pythonw" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Python,
            DisplayName = "Python Environment",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Scripting & Data Science Runtime",
            EnvironmentVariable = Environment.GetEnvironmentVariable("PYTHONHOME") != null ? "PYTHONHOME" : null,
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectJavaAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("javac", workspaceRoot)
            ?? SdkPathResolver.ResolveExecutable("java", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync(exe, "-version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "java", "jar", "mvn", "gradle" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Java,
            DisplayName = "Java Development Kit (JDK)",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "JVM Compiler & Runtime",
            EnvironmentVariable = Environment.GetEnvironmentVariable("JAVA_HOME") != null ? "JAVA_HOME" : null,
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectFlutterAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("flutter", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("flutter", "--version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        var dart = SdkPathResolver.ResolveExecutable("dart", workspaceRoot);
        if (dart != null) secondaries.Add(dart);

        return new DetectedSdk
        {
            SdkId = KnownSdk.Flutter,
            DisplayName = "Flutter & Dart SDK",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Cross-Platform Framework SDK",
            EnvironmentVariable = Environment.GetEnvironmentVariable("FLUTTER_ROOT") != null ? "FLUTTER_ROOT" : null,
            SecondaryBinaries = secondaries
        };
    }

    private static Task<DetectedSdk?> DetectMsvcAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("cl", workspaceRoot);
        if (exe == null) return Task.FromResult<DetectedSdk?>(null);

        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        return Task.FromResult<DetectedSdk?>(new DetectedSdk
        {
            SdkId = KnownSdk.VisualStudio,
            DisplayName = "Microsoft Visual C++ (MSVC)",
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Native C/C++ Toolchain"
        });
    }

    private static async Task<DetectedSdk?> DetectLlvmAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("clang", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync(exe, "--version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        return new DetectedSdk
        {
            SdkId = KnownSdk.Llvm,
            DisplayName = "LLVM / Clang",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Native C/C++ Compiler Suite"
        };
    }

    private static async Task<DetectedSdk?> DetectPhpAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("php", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("php", "-v", workspaceRoot, cancellationToken: ct);
        var rootDir = Path.GetDirectoryName(exe) ?? exe;

        var secondaries = new List<string>();
        var composer = SdkPathResolver.ResolveExecutable("composer", workspaceRoot);
        if (composer != null) secondaries.Add(composer);

        return new DetectedSdk
        {
            SdkId = KnownSdk.Php,
            DisplayName = "PHP Runtime & Web Stack",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Web Scripting Runtime",
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectRubyAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("ruby", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("ruby", "-v", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "gem", "bundle", "rails" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Ruby,
            DisplayName = "Ruby Programming Environment",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Scripting & Web Framework Runtime",
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectElixirAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("elixir", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("elixir", "-v", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        var secondaries = new List<string>();
        foreach (var tool in new[] { "mix", "iex" })
        {
            var p = SdkPathResolver.ResolveExecutable(tool, workspaceRoot);
            if (p != null) secondaries.Add(p);
        }

        return new DetectedSdk
        {
            SdkId = KnownSdk.Elixir,
            DisplayName = "Elixir & Erlang BEAM",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Concurrent Distributed Platform",
            SecondaryBinaries = secondaries
        };
    }

    private static async Task<DetectedSdk?> DetectGitAsync(string? workspaceRoot, CancellationToken ct)
    {
        var exe = SdkPathResolver.ResolveExecutable("git", workspaceRoot);
        if (exe == null) return null;

        var versionRes = await ToolchainExecutor.ExecuteAsync("git", "--version", workspaceRoot, cancellationToken: ct);
        var binDir = Path.GetDirectoryName(exe) ?? exe;
        var rootDir = Directory.GetParent(binDir)?.FullName ?? binDir;

        return new DetectedSdk
        {
            SdkId = KnownSdk.Git,
            DisplayName = "Git Version Control",
            Version = versionRes.DetectedVersion ?? versionRes.Output.Split('\n')[0].Trim(),
            RootDirectory = rootDir,
            MainExecutablePath = exe,
            Category = "Version Control & Shell Tools"
        };
    }
}
